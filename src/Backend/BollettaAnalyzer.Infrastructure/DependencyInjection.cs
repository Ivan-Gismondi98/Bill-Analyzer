using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Infrastructure.Auth;
using BollettaAnalyzer.Infrastructure.Persistence;
using BollettaAnalyzer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BollettaAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Provider DB selezionabile da configurazione: "Sqlite" (default) o "Postgres".
        var provider = config["Database:Provider"] ?? "Sqlite";
        var connString = config.GetConnectionString("Default")
                         ?? "Data Source=bolletta.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(connString);
            else
                options.UseSqlite(connString);
        });

        // Impostazioni JWT
        var jwt = new JwtSettings();
        config.GetSection("Jwt").Bind(jwt);
        if (string.IsNullOrWhiteSpace(jwt.Key))
            jwt.Key = "CHANGE_ME_super_secret_dev_key_min_32_chars_length!!";
        services.AddSingleton(jwt);

        services.AddHttpContextAccessor();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Cifratura dei documenti sensibili (contratto PDF) a riposo.
        var encryption = new EncryptionSettings();
        config.GetSection("Encryption").Bind(encryption);
        services.AddSingleton(encryption);
        services.AddSingleton<IFileEncryptionService, AesFileEncryptionService>();

        // OCR: mock di default (sostituibile con integrazione reale).
        services.AddScoped<IBillOcrService, MockBillOcrService>();

        return services;
    }
}
