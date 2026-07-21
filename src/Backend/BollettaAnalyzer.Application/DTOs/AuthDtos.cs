using System.ComponentModel.DataAnnotations;

namespace BollettaAnalyzer.Application.DTOs;

public record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(6)] string Password,
    [property: Required] string Nome,
    [property: Required] string Cognome);

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record AuthResponse(
    string Token,
    DateTime ScadenzaToken,
    UtenteDto Utente);

public record UtenteDto(
    Guid Id,
    string Email,
    string Nome,
    string Cognome,
    string? Telefono,
    string? Indirizzo,
    string? Citta,
    string? Cap);

public record UpdateProfiloRequest(
    string Nome,
    string Cognome,
    string? Telefono,
    string? Indirizzo,
    string? Citta,
    string? Cap);
