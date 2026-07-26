using System.Text;
using System.Threading.RateLimiting;
using BollettaAnalyzer.Application;
using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Infrastructure;
using BollettaAnalyzer.Infrastructure.Auth;
using BollettaAnalyzer.Infrastructure.Persistence;
using BollettaAnalyzer.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

// --- Servizi applicativi ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, isDev);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

// --- Autenticazione JWT ---
// La chiave DEVE essere configurata: in produzione l'app rifiuta di partire senza,
// così un deploy dimenticato non gira mai con una chiave nota pubblicamente.
var jwt = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwt);
if (string.IsNullOrWhiteSpace(jwt.Key))
{
    if (!isDev)
        throw new InvalidOperationException(
            "Jwt:Key non configurata. Impostare una chiave segreta (>= 32 caratteri) via variabile d'ambiente o secret store.");
    jwt.Key = JwtSettings.DevOnlyKey;
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization();

// --- Rate limiting: protegge login/registrazione da brute force ed enumerazione ---
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// --- CORS ---
// In sviluppo il MAUI HybridWebView usa origin custom → si accetta tutto.
// In produzione solo la allowlist configurata (Cors:AllowedOrigins).
const string CorsPolicy = "ClientApp";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
{
    p.AllowAnyHeader().AllowAnyMethod();
    if (isDev)
        p.SetIsOriginAllowed(_ => true).AllowCredentials();
    else if (allowedOrigins.Length > 0)
        p.WithOrigins(allowedOrigins).AllowCredentials();
    // Nessun origin configurato in produzione → CORS resta chiuso (nessun header emesso).
}));

// --- Swagger con supporto Bearer ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bolletta Analyzer API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserisci il token JWT (senza prefisso 'Bearer ')."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// --- Schema DB ---
// Con migrazioni presenti si usa Migrate (evolvibile). Altrimenti le tabelle vengono
// create esplicitamente: EnsureCreated NON basta sui database "condivisi" come
// Supabase, dove il DB contiene già le tabelle di piattaforma (schemi auth/storage):
// EF le vedrebbe, concluderebbe che lo schema esiste e non creerebbe mai le tabelle
// dell'app ("relation Utenti does not exist"). Qui si controlla la presenza delle
// NOSTRE tabelle, non di tabelle qualsiasi.
// I dati demo vengono seminati SOLO in sviluppo: mai account noti in produzione.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.GetMigrations().Any())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync())
            await creator.CreateAsync();          // es. file SQLite nuovo
        if (!await TabelleAppEsistentiAsync(db))
            await creator.CreateTablesAsync();    // crea le tabelle dell'app
    }

    if (app.Environment.IsDevelopment())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, hasher);
    }
}

// --- Pipeline ---
if (app.Environment.IsDevelopment())
{
    // In sviluppo l'errore reale viene restituito nel campo "message", così il
    // client lo mostra direttamente (il banner rosso dell'app diventa il log).
    app.UseExceptionHandler(b => b.Run(async ctx =>
    {
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var messaggio = ex is null
            ? "Errore sconosciuto."
            : $"{ex.GetType().Name}: {ex.Message}" +
              (ex.InnerException is not null ? $" → {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : "");
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new { message = messaggio });
    }));
}
else
{
    app.UseExceptionHandler();   // produzione: ProblemDetails, mai dettagli interni al client
}
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Header di sicurezza di base.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { app = "Bolletta Analyzer API", status = "ok" }));

app.Run();

// Verifica se le tabelle dell'app esistono già, indipendentemente da altre tabelle
// presenti nel database (es. quelle interne di Supabase in altri schemi).
static async Task<bool> TabelleAppEsistentiAsync(AppDbContext db)
{
    try
    {
        await db.Utenti.AnyAsync();
        return true;
    }
    catch
    {
        return false;
    }
}
