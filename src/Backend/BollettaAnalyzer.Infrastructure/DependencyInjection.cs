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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config, bool isDevelopment = false)
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

        // Impostazioni JWT: nessun fallback in produzione (vedi anche Program.cs).
        var jwt = new JwtSettings();
        config.GetSection("Jwt").Bind(jwt);
        if (string.IsNullOrWhiteSpace(jwt.Key))
        {
            if (!isDevelopment)
                throw new InvalidOperationException(
                    "Jwt:Key non configurata. Impostarla via variabile d'ambiente o secret store.");
            jwt.Key = JwtSettings.DevOnlyKey;
        }
        services.AddSingleton(jwt);

        services.AddHttpContextAccessor();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Cifratura dei documenti sensibili (contratto PDF) a riposo.
        // Come per il JWT: in produzione la chiave è obbligatoria.
        var encryption = new EncryptionSettings();
        config.GetSection("Encryption").Bind(encryption);
        if (string.IsNullOrWhiteSpace(encryption.Key) && !isDevelopment)
            throw new InvalidOperationException(
                "Encryption:Key non configurata. Impostare una chiave AES-256 (Base64, 32 byte) via secret store.");
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
        services.AddSingleton<ItalianContractParser>();
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
