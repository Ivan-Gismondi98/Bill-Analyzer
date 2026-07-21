using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Infrastructure.Auth;
using BollettaAnalyzer.Infrastructure.Persistence;
using BollettaAnalyzer.Infrastructure.Services;
using BollettaAnalyzer.Infrastructure.Services.Ocr;
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

        // OCR bollette: provider selezionabile da configurazione (sezione "Ocr").
        //   - "Local" (default): PdfPig (PDF digitali) + Tesseract (immagini) + parser italiano
        //   - "Azure": Azure AI Document Intelligence (prebuilt-invoice) + parser italiano
        //   - "Mock": dati fittizi per sviluppo/demo
        var ocr = new OcrSettings();
        config.GetSection("Ocr").Bind(ocr);
        services.AddSingleton(ocr);

        // Componenti riusabili dalle pipeline OCR.
        services.AddSingleton<ItalianBillParser>();
        services.AddSingleton<PdfTextExtractor>();
        services.AddSingleton<TesseractOcrEngine>();

        switch (ocr.Provider.Trim().ToLowerInvariant())
        {
            case "azure":
                services.AddScoped<IBillOcrService, AzureDocumentIntelligenceOcrService>();
                break;
            case "mock":
                services.AddScoped<IBillOcrService, MockBillOcrService>();
                break;
            default: // "local"
                services.AddScoped<IBillOcrService, LocalBillOcrService>();
                break;
        }

        return services;
    }
}
