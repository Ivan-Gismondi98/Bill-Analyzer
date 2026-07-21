using System.Security.Cryptography;
using BollettaAnalyzer.Application.Common.Interfaces;

namespace BollettaAnalyzer.Infrastructure.Services;

/// <summary>Impostazioni di cifratura (bind da appsettings: "Encryption").</summary>
public class EncryptionSettings
{
    /// <summary>Chiave AES a 256 bit codificata in Base64 (32 byte).</summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Cifratura AES-256-GCM (confidenzialità + integrità autenticata).
/// Usata per conservare criptato il PDF del contratto luce/gas dell'utente.
/// </summary>
public class AesFileEncryptionService : IFileEncryptionService
{
    private const int NonceSize = 12;   // 96 bit, raccomandato per GCM
    private const int TagSize = 16;     // 128 bit
    private readonly byte[] _key;

    public AesFileEncryptionService(EncryptionSettings settings)
    {
        _key = DerivaChiave(settings.Key);
    }

    public EncryptedPayload Encrypt(byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return new EncryptedPayload(ciphertext, nonce, tag);
    }

    public byte[] Decrypt(EncryptedPayload payload)
    {
        var plaintext = new byte[payload.Ciphertext.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, plaintext);
        return plaintext;
    }

    /// <summary>
    /// Ricava una chiave a 256 bit dalla configurazione: usa direttamente i 32 byte
    /// Base64 se validi, altrimenti applica SHA-256 alla stringa fornita.
    /// </summary>
    private static byte[] DerivaChiave(string configKey)
    {
        if (!string.IsNullOrWhiteSpace(configKey))
        {
            try
            {
                var raw = Convert.FromBase64String(configKey);
                if (raw.Length == 32) return raw;
            }
            catch (FormatException) { /* non Base64: si passa all'hash */ }

            return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(configKey));
        }

        // Raggiungibile solo in Development: in produzione la DI rifiuta di partire
        // senza Encryption:Key configurata (vedi Infrastructure/DependencyInjection.cs).
        return SHA256.HashData("dev-only-encryption-key-change-me"u8.ToArray());
    }
}
