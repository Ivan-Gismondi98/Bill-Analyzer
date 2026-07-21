using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Application.Mapping;
using BollettaAnalyzer.Domain.Entities;
using BollettaAnalyzer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BollettaAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
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
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var utente = await _db.Utenti.FirstOrDefaultAsync(u => u.Email == emailNorm);
        if (utente is null || !_hasher.Verifica(req.Password, utente.PasswordHash))
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
}
