using System.ComponentModel.DataAnnotations;
using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Application.Mapping;
using BollettaAnalyzer.Domain.Entities;
using BollettaAnalyzer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BollettaAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Hash BCrypt di una password casuale: verificato quando l'utente non esiste,
    // così il tempo di risposta del login non rivela se un'email è registrata.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ICurrentUser _currentUser;

    public AuthController(AppDbContext db, IPasswordHasher hasher, IJwtTokenGenerator jwt, ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var emailNorm = req.Email.Trim().ToLowerInvariant();
        if (await _db.Utenti.AnyAsync(u => u.Email == emailNorm))
            return Conflict(new { message = "Email già registrata." });

        var utente = new Utente
        {
            Email = emailNorm,
            PasswordHash = _hasher.Hash(req.Password),
            Nome = req.Nome,
            Cognome = req.Cognome
        };
        _db.Utenti.Add(utente);
        await _db.SaveChangesAsync();

        var (token, scadenza) = _jwt.GeneraToken(utente);
        return Ok(new AuthResponse(token, scadenza, utente.ToDto()));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var utente = await _db.Utenti.FirstOrDefaultAsync(u => u.Email == emailNorm);

        // Si esegue SEMPRE una verifica BCrypt (vera o dummy) per uniformare i tempi.
        var passwordOk = _hasher.Verifica(req.Password, utente?.PasswordHash ?? DummyHash);
        if (utente is null || !passwordOk)
            return Unauthorized(new { message = "Credenziali non valide." });

        var (token, scadenza) = _jwt.GeneraToken(utente);
        return Ok(new AuthResponse(token, scadenza, utente.ToDto()));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UtenteDto>> Me()
    {
        var utente = await _db.Utenti.FindAsync(_currentUser.UtenteId);
        return utente is null ? NotFound() : Ok(utente.ToDto());
    }

    [HttpPut("profilo")]
    [Authorize]
    public async Task<ActionResult<UtenteDto>> AggiornaProfilo(UpdateProfiloRequest req)
    {
        var utente = await _db.Utenti.FindAsync(_currentUser.UtenteId);
        if (utente is null) return NotFound();

        utente.Nome = req.Nome;
        utente.Cognome = req.Cognome;
        utente.Telefono = req.Telefono;
        utente.Indirizzo = req.Indirizzo;
        utente.Citta = req.Citta;
        utente.Cap = req.Cap;
        await _db.SaveChangesAsync();

        return Ok(utente.ToDto());
    }

    /// <summary>
    /// Esporta tutti i dati dell'utente in JSON (portabilità GDPR):
    /// profilo, contratti, bollette con voci, letture e dispositivi.
    /// </summary>
    [HttpGet("export")]
    [Authorize]
    public async Task<IActionResult> EsportaDati()
    {
        var utente = await _db.Utenti.FindAsync(_currentUser.UtenteId);
        if (utente is null) return NotFound();

        var contratti = await _db.Contratti
            .Where(c => c.UtenteId == utente.Id)
            .ToListAsync();
        var contrattiIds = contratti.Select(c => c.Id).ToList();

        var bollette = await _db.Bollette
            .Include(b => b.VociDiCosto)
            .Where(b => contrattiIds.Contains(b.ContrattoId))
            .ToListAsync();
        var letture = await _db.Letture
            .Where(l => contrattiIds.Contains(l.ContrattoId))
            .ToListAsync();
        var dispositivi = await _db.Dispositivi
            .Where(d => d.UtenteId == utente.Id)
            .ToListAsync();

        var export = new
        {
            esportatoIl = DateTime.UtcNow,
            profilo = utente.ToDto(),
            contratti = contratti.Select(c => c.ToDto()),
            bollette = bollette.Select(b => b.ToDto()),
            letture = letture.Select(l => l.ToDto()),
            dispositivi = dispositivi.Select(d => d.ToDto()),
        };

        return File(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(export,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            "application/json",
            "bolletta-analyzer-export.json");
    }

    /// <summary>
    /// Elimina definitivamente l'account e tutti i dati collegati (diritto all'oblio GDPR).
    /// Richiede la password corrente come ri-autenticazione.
    /// </summary>
    [HttpDelete("account")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> EliminaAccount(DeleteAccountRequest req)
    {
        var utente = await _db.Utenti.FindAsync(_currentUser.UtenteId);
        if (utente is null) return NotFound();

        if (!_hasher.Verifica(req.Password, utente.PasswordHash))
            return Unauthorized(new { message = "Password non corretta." });

        // Il cascade configurato in EF elimina contratti, bollette, letture,
        // dispositivi e documenti cifrati collegati.
        _db.Utenti.Remove(utente);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>Ri-autenticazione richiesta per l'eliminazione dell'account.</summary>
public record DeleteAccountRequest([Required] string Password);
