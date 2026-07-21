namespace BollettaAnalyzer.Application.Common.Interfaces;

/// <summary>Contenuto cifrato con AES-GCM: testo cifrato + nonce + tag di autenticazione.</summary>
public record EncryptedPayload(byte[] Ciphertext, byte[] Nonce, byte[] Tag);

/// <summary>Cifratura simmetrica dei documenti sensibili (es. contratto luce/gas).</summary>
public interface IFileEncryptionService
{
    EncryptedPayload Encrypt(byte[] plaintext);
    byte[] Decrypt(EncryptedPayload payload);
}
