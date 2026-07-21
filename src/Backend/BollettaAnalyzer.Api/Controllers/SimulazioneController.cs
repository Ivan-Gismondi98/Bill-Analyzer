using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.DTOs;
using BollettaAnalyzer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BollettaAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SimulazioneController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISimulazioneService _simulazione;

    public SimulazioneController(AppDbContext db, ICurrentUser currentUser, ISimulazioneService simulazione)
    {
        _db = db;
        _currentUser = currentUser;
        _simulazione = simulazione;
    }

    /// <summary>Calcola l'impatto aggregato dei dispositivi selezionati sul contratto luce.</summary>
    [HttpPost("dispositivi")]
    public async Task<ActionResult<SimulazioneDispositiviResponse>> Dispositivi(SimulazioneDispositiviRequest req)
    {
        var contratto = await _db.Contratti
            .FirstOrDefaultAsync(c => c.Id == req.ContrattoId && c.UtenteId == _currentUser.UtenteId);
        if (contratto is null) return BadRequest(new { message = "Contratto non valido." });

        var dispositivi = await _db.Dispositivi
            .Where(d => d.UtenteId == _currentUser.UtenteId &&
                        (req.DispositiviIds.Count == 0 || req.DispositiviIds.Contains(d.Id)))
            .ToListAsync();

        return Ok(_simulazione.SimulaDispositivi(contratto, dispositivi));
    }

    /// <summary>Previsione della prossima bolletta a partire da un'auto-lettura del contatore.</summary>
    [HttpPost("previsione")]
    public async Task<ActionResult<PrevisioneBollettaResponse>> Previsione(PrevisioneBollettaRequest req)
    {
        var contratto = await _db.Contratti
            .FirstOrDefaultAsync(c => c.Id == req.ContrattoId && c.UtenteId == _currentUser.UtenteId);
        if (contratto is null) return BadRequest(new { message = "Contratto non valido." });

        var ultimaLettura = await _db.Letture
            .Where(l => l.ContrattoId == req.ContrattoId)
            .OrderByDescending(l => l.DataLettura)
            .FirstOrDefaultAsync();

        return Ok(_simulazione.PrevediBolletta(contratto, ultimaLettura, req));
    }
}
