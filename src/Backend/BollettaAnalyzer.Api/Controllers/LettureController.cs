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
[Authorize]
public class LettureController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LettureController(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LetturaContatoreDto>>> Lista([FromQuery] Guid contrattoId)
    {
        var letture = await _db.Letture
            .Where(l => l.ContrattoId == contrattoId && l.Contratto!.UtenteId == _currentUser.UtenteId)
            .OrderByDescending(l => l.DataLettura)
            .ToListAsync();
        return Ok(letture.Select(l => l.ToDto()));
    }

    /// <summary>Restituisce l'ultima lettura registrata per il contratto (base per la previsione).</summary>
    [HttpGet("ultima")]
    public async Task<ActionResult<LetturaContatoreDto>> Ultima([FromQuery] Guid contrattoId)
    {
        var lettura = await _db.Letture
            .Where(l => l.ContrattoId == contrattoId && l.Contratto!.UtenteId == _currentUser.UtenteId)
            .OrderByDescending(l => l.DataLettura)
            .FirstOrDefaultAsync();
        return lettura is null ? NoContent() : Ok(lettura.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<LetturaContatoreDto>> Crea(CreateLetturaRequest req)
    {
        var contratto = await _db.Contratti
            .FirstOrDefaultAsync(c => c.Id == req.ContrattoId && c.UtenteId == _currentUser.UtenteId);
        if (contratto is null) return BadRequest(new { message = "Contratto non valido." });

        // Regole di dominio: una lettura non può essere futura e il contatore
        // è un totalizzatore che cresce, non può "tornare indietro".
        if (req.DataLettura.Date > DateTime.UtcNow.Date.AddDays(1))
            return BadRequest(new { message = "La data della lettura non può essere nel futuro." });

        var precedente = await _db.Letture
            .Where(l => l.ContrattoId == req.ContrattoId && l.DataLettura < req.DataLettura)
            .OrderByDescending(l => l.DataLettura)
            .FirstOrDefaultAsync();
        if (precedente is not null && req.ValoreTotale < precedente.ValoreTotale)
            return BadRequest(new
            {
                message = $"Il valore inserito ({req.ValoreTotale}) è inferiore alla lettura del " +
                          $"{precedente.DataLettura:dd/MM/yyyy} ({precedente.ValoreTotale}). " +
                          "Se il contatore è stato sostituito, elimina prima lo storico letture."
            });

        var lettura = new LetturaContatore
        {
            ContrattoId = req.ContrattoId,
            DataLettura = req.DataLettura,
            ValoreTotale = req.ValoreTotale,
            ValoreF1 = req.ValoreF1,
            ValoreF2 = req.ValoreF2,
            ValoreF3 = req.ValoreF3,
            Note = req.Note,
            DaBolletta = false
        };

        // Inserimento + potatura dello storico in un'unica transazione.
        await using var tx = await _db.Database.BeginTransactionAsync();
        _db.Letture.Add(lettura);
        await _db.SaveChangesAsync();
        await ManteniUltime3(req.ContrattoId);
        await tx.CommitAsync();

        return Ok(lettura.ToDto());
    }

    private const int MaxStorico = 3;

    /// <summary>
    /// Conserva solo le <see cref="MaxStorico"/> auto-letture più recenti del contratto.
    /// Le letture derivate da bolletta (baseline per le previsioni) non vengono mai eliminate.
    /// </summary>
    private async Task ManteniUltime3(Guid contrattoId)
    {
        var daRimuovere = await _db.Letture
            .Where(l => l.ContrattoId == contrattoId && !l.DaBolletta)
            .OrderByDescending(l => l.DataLettura)
            .ThenByDescending(l => l.CreatedAt)
            .Skip(MaxStorico)
            .ToListAsync();

        if (daRimuovere.Count > 0)
        {
            _db.Letture.RemoveRange(daRimuovere);
            await _db.SaveChangesAsync();
        }
    }
}
