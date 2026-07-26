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
public class DispositiviController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DispositiviController(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DispositivoDto>>> Lista()
    {
        var dispositivi = await _db.Dispositivi
            .Where(d => d.UtenteId == _currentUser.UtenteId)
            .OrderBy(d => d.Nome)
            .ToListAsync();
        return Ok(dispositivi.Select(d => d.ToDto()));
    }

    [HttpPost]
    public async Task<ActionResult<DispositivoDto>> Crea(UpsertDispositivoRequest req)
    {
        if (!FasceValide(req)) return BadRequest(new { message = "Seleziona almeno una fascia oraria tra F1, F2 e F3." });
        var d = new DispositivoElettronico { UtenteId = _currentUser.UtenteId!.Value };
        Applica(d, req);
        _db.Dispositivi.Add(d);
        await _db.SaveChangesAsync();
        return Ok(d.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DispositivoDto>> Aggiorna(Guid id, UpsertDispositivoRequest req)
    {
        if (!FasceValide(req)) return BadRequest(new { message = "Seleziona almeno una fascia oraria tra F1, F2 e F3." });
        var d = await _db.Dispositivi.FirstOrDefaultAsync(x => x.Id == id && x.UtenteId == _currentUser.UtenteId);
        if (d is null) return NotFound();
        Applica(d, req);
        await _db.SaveChangesAsync();
        return Ok(d.ToDto());
    }

    private static bool FasceValide(UpsertDispositivoRequest r) =>
        r.Fasce.Count > 0 && r.Fasce.All(f => f is Domain.Enums.FasciaOraria.F1 or Domain.Enums.FasciaOraria.F2 or Domain.Enums.FasciaOraria.F3);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Elimina(Guid id)
    {
        var d = await _db.Dispositivi.FirstOrDefaultAsync(x => x.Id == id && x.UtenteId == _currentUser.UtenteId);
        if (d is null) return NotFound();
        _db.Dispositivi.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Applica(DispositivoElettronico d, UpsertDispositivoRequest r)
    {
        d.Nome = r.Nome;
        d.PotenzaWatt = r.PotenzaWatt;
        d.OreUtilizzoGiornaliere = r.OreUtilizzoGiornaliere;
        d.GiorniSettimana = r.GiorniSettimana;
        d.UsaF1 = r.Fasce.Contains(Domain.Enums.FasciaOraria.F1);
        d.UsaF2 = r.Fasce.Contains(Domain.Enums.FasciaOraria.F2);
        d.UsaF3 = r.Fasce.Contains(Domain.Enums.FasciaOraria.F3);
    }
}
