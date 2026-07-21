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
public class ContrattiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IFileEncryptionService _encryption;

    public ContrattiController(AppDbContext db, ICurrentUser currentUser, IFileEncryptionService encryption)
    {
        _db = db;
        _currentUser = currentUser;
        _encryption = encryption;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContrattoDto>>> Lista()
    {
        var contratti = await _db.Contratti
            .Where(c => c.UtenteId == _currentUser.UtenteId)
            .OrderBy(c => c.TipoFornitura)
            .ToListAsync();
        return Ok(contratti.Select(c => c.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContrattoDto>> Dettaglio(Guid id)
    {
        var c = await _db.Contratti.FirstOrDefaultAsync(x => x.Id == id && x.UtenteId == _currentUser.UtenteId);
        return c is null ? NotFound() : Ok(c.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ContrattoDto>> Crea(UpsertContrattoRequest req)
    {
        var c = new Contratto { UtenteId = _currentUser.UtenteId!.Value };
        Applica(c, req);
        _db.Contratti.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Dettaglio), new { id = c.Id }, c.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContrattoDto>> Aggiorna(Guid id, UpsertContrattoRequest req)
    {
        var c = await _db.Contratti.FirstOrDefaultAsync(x => x.Id == id && x.UtenteId == _currentUser.UtenteId);
        if (c is null) return NotFound();
        Applica(c, req);
        await _db.SaveChangesAsync();
        return Ok(c.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Elimina(Guid id)
    {
        var c = await _db.Contratti.FirstOrDefaultAsync(x => x.Id == id && x.UtenteId == _currentUser.UtenteId);
        if (c is null) return NotFound();
        _db.Contratti.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Documento di contratto (PDF cifrato a riposo) ---

    /// <summary>Metadati del PDF di contratto cifrato (404 se non presente).</summary>
    [HttpGet("{id:guid}/documento")]
    public async Task<ActionResult<DocumentoContrattoInfoDto>> InfoDocumento(Guid id)
    {
        if (!await PossiedeContratto(id)) return NotFound();
        var doc = await _db.DocumentiContratto.AsNoTracking().FirstOrDefaultAsync(d => d.ContrattoId == id);
        return doc is null
            ? NotFound()
            : Ok(new DocumentoContrattoInfoDto(doc.ContrattoId, doc.NomeFile, doc.ContentType, doc.DimensioneByte, doc.CreatedAt));
    }

    /// <summary>
    /// Carica/sostituisce il PDF del contratto. Il file viene cifrato con AES-GCM
    /// prima di essere salvato: nel database non transita mai in chiaro.
    /// </summary>
    [HttpPost("{id:guid}/documento")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<DocumentoContrattoInfoDto>> CaricaDocumento(Guid id, IFormFile file, CancellationToken ct)
    {
        if (!await PossiedeContratto(id)) return NotFound();
        if (file is null || file.Length == 0) return BadRequest(new { message = "Nessun file caricato." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var payload = _encryption.Encrypt(ms.ToArray());

        var doc = await _db.DocumentiContratto.FirstOrDefaultAsync(d => d.ContrattoId == id, ct);
        if (doc is null)
        {
            doc = new DocumentoContratto { ContrattoId = id };
            _db.DocumentiContratto.Add(doc);
        }

        doc.NomeFile = file.FileName;
        doc.ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType;
        doc.DimensioneByte = file.Length;
        doc.Contenuto = payload.Ciphertext;
        doc.Nonce = payload.Nonce;
        doc.Tag = payload.Tag;

        await _db.SaveChangesAsync(ct);
        return Ok(new DocumentoContrattoInfoDto(doc.ContrattoId, doc.NomeFile, doc.ContentType, doc.DimensioneByte, doc.CreatedAt));
    }

    /// <summary>Scarica il PDF del contratto, decifrandolo al volo.</summary>
    [HttpGet("{id:guid}/documento/download")]
    public async Task<IActionResult> ScaricaDocumento(Guid id)
    {
        if (!await PossiedeContratto(id)) return NotFound();
        var doc = await _db.DocumentiContratto.AsNoTracking().FirstOrDefaultAsync(d => d.ContrattoId == id);
        if (doc is null) return NotFound();

        var plaintext = _encryption.Decrypt(new EncryptedPayload(doc.Contenuto, doc.Nonce, doc.Tag));
        return File(plaintext, doc.ContentType, doc.NomeFile);
    }

    [HttpDelete("{id:guid}/documento")]
    public async Task<IActionResult> EliminaDocumento(Guid id)
    {
        if (!await PossiedeContratto(id)) return NotFound();
        var doc = await _db.DocumentiContratto.FirstOrDefaultAsync(d => d.ContrattoId == id);
        if (doc is null) return NotFound();
        _db.DocumentiContratto.Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Task<bool> PossiedeContratto(Guid id) =>
        _db.Contratti.AnyAsync(c => c.Id == id && c.UtenteId == _currentUser.UtenteId);

    private static void Applica(Contratto c, UpsertContrattoRequest r)
    {
        c.TipoFornitura = r.TipoFornitura;
        c.Fornitore = r.Fornitore;
        c.CodicePod = r.CodicePod;
        c.CodicePdr = r.CodicePdr;
        c.NomeOfferta = r.NomeOfferta;
        c.TipoTariffa = r.TipoTariffa;
        c.PotenzaImpegnataKw = r.PotenzaImpegnataKw;
        c.PrezzoKwhMonorario = r.PrezzoKwhMonorario;
        c.PrezzoKwhF1 = r.PrezzoKwhF1;
        c.PrezzoKwhF2 = r.PrezzoKwhF2;
        c.PrezzoKwhF3 = r.PrezzoKwhF3;
        c.PrezzoSm3 = r.PrezzoSm3;
        c.QuotaFissaMensile = r.QuotaFissaMensile;
    }
}
