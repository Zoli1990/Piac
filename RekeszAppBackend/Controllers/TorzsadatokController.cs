using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RekeszAppBackend.Contracts;
using RekeszAppBackend.Data;
using RekeszAppBackend.Domain;
using RekeszAppBackend.Infrastructure;

namespace RekeszAppBackend.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Admin")]
public class TorzsadatokController(AppDbContext db, IWebHostEnvironment env, IConfiguration config) : ControllerBase
{
    private static string? UresbolNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ---------- Partnerek (eladók) ----------

    [HttpGet("partnerek")]
    public async Task<IActionResult> Partnerek() =>
        Ok(await db.Partnerek.OrderBy(x => x.Nev).ToListAsync());

    [HttpPost("partnerek")]
    public async Task<IActionResult> UjPartner(PartnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nev))
            return BadRequest(new { message = "Az eladó neve kötelező." });
        var entity = new Partner { Nev = request.Nev.Trim(), Megjegyzes = UresbolNull(request.Megjegyzes) };
        db.Partnerek.Add(entity);
        await db.SaveChangesAsync();
        return Created($"api/partnerek/{entity.Id}", entity);
    }

    [HttpPut("partnerek/{id:int}")]
    public async Task<IActionResult> ModositPartner(int id, PartnerRequest request)
    {
        var entity = await db.Partnerek.FindAsync(id);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Nev))
            return BadRequest(new { message = "Az eladó neve kötelező." });
        entity.Nev = request.Nev.Trim();
        entity.Megjegyzes = UresbolNull(request.Megjegyzes);
        await db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("partnerek/{id:int}")]
    public async Task<IActionResult> TorolPartner(int id)
    {
        var entity = await db.Partnerek.FindAsync(id);
        if (entity is null) return NotFound();
        if (await db.FelvasarlasTetelek.AnyAsync(x => x.PartnerId == id))
            return Conflict(new { message = "A partner nem törölhető, mert van hozzá rögzített felvásárlás." });
        db.Partnerek.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Vevek ----------

    [HttpGet("vevek")]
    public async Task<IActionResult> Vevek() =>
        Ok(await db.Vevek.OrderBy(x => x.Nev).ToListAsync());

    [HttpPost("vevek")]
    public async Task<IActionResult> UjVevo(VevoRequest request)
    {
        var entity = new Vevo { Nev = UresbolNull(request.Nev), Megjegyzes = UresbolNull(request.Megjegyzes) };
        db.Vevek.Add(entity);
        await db.SaveChangesAsync();
        return Created($"api/vevek/{entity.Id}", entity);
    }

    [HttpPut("vevek/{id:int}")]
    public async Task<IActionResult> ModositVevo(int id, VevoRequest request)
    {
        var entity = await db.Vevek.FindAsync(id);
        if (entity is null) return NotFound();
        entity.Nev = UresbolNull(request.Nev);
        entity.Megjegyzes = UresbolNull(request.Megjegyzes);
        await db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("vevek/{id:int}")]
    public async Task<IActionResult> TorolVevo(int id)
    {
        var entity = await db.Vevek.FindAsync(id);
        if (entity is null) return NotFound();
        if (await db.EladasTetelek.AnyAsync(x => x.VevoId == id))
            return Conflict(new { message = "A vevő nem törölhető, mert van hozzá rögzített eladás." });
        db.Vevek.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Zoldsegek ----------

    [HttpGet("zoldsegek")]
    public async Task<IActionResult> Zoldsegek() =>
        Ok(await db.Zoldsegek
            .Select(x => new { x.Id, x.Nev, x.AlapertelmezettRekeszTipusId, alapertelmezettRekeszTipusNev = x.AlapertelmezettRekeszTipus != null ? x.AlapertelmezettRekeszTipus.Nev : null, x.KepUrl })
            .OrderBy(x => x.Nev).ToListAsync());

    [HttpPost("zoldsegek")]
    public async Task<IActionResult> UjZoldseg(ZoldsegRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nev))
            return BadRequest(new { message = "A zöldség neve kötelező." });
        if (await db.Zoldsegek.AnyAsync(x => x.Nev == request.Nev.Trim()))
            return Conflict(new { message = "Már létezik ilyen nevű zöldségfajta." });
        var entity = new Zoldseg { Nev = request.Nev.Trim(), AlapertelmezettRekeszTipusId = request.AlapertelmezettRekeszTipusId };
        db.Zoldsegek.Add(entity);
        await db.SaveChangesAsync();
        return Created($"api/zoldsegek/{entity.Id}", entity);
    }

    [HttpPut("zoldsegek/{id:int}")]
    public async Task<IActionResult> ModositZoldseg(int id, ZoldsegRequest request)
    {
        var entity = await db.Zoldsegek.FindAsync(id);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Nev))
            return BadRequest(new { message = "A zöldség neve kötelező." });
        entity.Nev = request.Nev.Trim();
        entity.AlapertelmezettRekeszTipusId = request.AlapertelmezettRekeszTipusId;
        await db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("zoldsegek/{id:int}")]
    public async Task<IActionResult> TorolZoldseg(int id)
    {
        var entity = await db.Zoldsegek.FindAsync(id);
        if (entity is null) return NotFound();
        var hasznalt = await db.FelvasarlasTetelek.AnyAsync(x => x.ZoldsegId == id)
            || await db.EladasTetelek.AnyAsync(x => x.ZoldsegId == id);
        if (hasznalt)
            return Conflict(new { message = "A zöldségfajta nem törölhető, mert van hozzá rögzített tétel." });
        db.Zoldsegek.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Zöldség-kategória képe: minden ehhez a zöldséghez tartozó felvásárlás/eladás ugyanazt a képet
    // mutatja, nem tételenkénti. Az új feltöltés felülírja a korábbi képet.
    [HttpPost("zoldsegek/{id:int}/kep")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ZoldsegKep(int id, IFormFile kep)
    {
        var entity = await db.Zoldsegek.FindAsync(id);
        if (entity is null) return NotFound();
        if (kep is null || kep.Length == 0) return BadRequest(new { message = "Nincs kiválasztott kép." });
        if (!kep.ContentType.StartsWith("image/")) return BadRequest(new { message = "Csak kép tölthető fel." });
        if (kep.Length > 5_000_000) return BadRequest(new { message = "A kép mérete legfeljebb 5 MB lehet." });

        var uploadsDir = UploadsPaths.Resolve(env, config);
        var fileName = Guid.NewGuid() + ".jpg";
        var filePath = Path.Combine(uploadsDir, fileName);
        try
        {
            await using var input = kep.OpenReadStream();
            await ImageProcessing.SaveResizedAsync(input, filePath);
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException)
        {
            return BadRequest(new { message = "A fájl nem értelmezhető képként." });
        }

        UploadsPaths.DeleteIfExists(env, config, entity.KepUrl);
        entity.KepUrl = "/uploads/" + fileName;
        await db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("zoldsegek/{id:int}/kep")]
    public async Task<IActionResult> ZoldsegKepTorlese(int id)
    {
        var entity = await db.Zoldsegek.FindAsync(id);
        if (entity is null) return NotFound();
        UploadsPaths.DeleteIfExists(env, config, entity.KepUrl);
        entity.KepUrl = null;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- RekeszTipusok ----------

    [HttpGet("rekesztipusok")]
    public async Task<IActionResult> RekeszTipusok() =>
        Ok(await db.RekeszTipusok.OrderBy(x => x.Nev).ToListAsync());

    [HttpPost("rekesztipusok")]
    public async Task<IActionResult> UjRekeszTipus(RekeszTipusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nev))
            return BadRequest(new { message = "A rekesztípus neve kötelező." });
        if (await db.RekeszTipusok.AnyAsync(x => x.Nev == request.Nev.Trim()))
            return Conflict(new { message = "Már létezik ilyen nevű rekesztípus." });
        var entity = new RekeszTipus { Nev = request.Nev.Trim() };
        db.RekeszTipusok.Add(entity);
        await db.SaveChangesAsync();
        return Created($"api/rekesztipusok/{entity.Id}", entity);
    }

    [HttpPut("rekesztipusok/{id:int}")]
    public async Task<IActionResult> ModositRekeszTipus(int id, RekeszTipusRequest request)
    {
        var entity = await db.RekeszTipusok.FindAsync(id);
        if (entity is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Nev))
            return BadRequest(new { message = "A rekesztípus neve kötelező." });
        entity.Nev = request.Nev.Trim();
        await db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("rekesztipusok/{id:int}")]
    public async Task<IActionResult> TorolRekeszTipus(int id)
    {
        var entity = await db.RekeszTipusok.FindAsync(id);
        if (entity is null) return NotFound();
        var hasznalt = await db.FelvasarlasTetelek.AnyAsync(x => x.RekeszTipusId == id)
            || await db.EladasTetelek.AnyAsync(x => x.RekeszTipusId == id);
        if (hasznalt)
            return Conflict(new { message = "A rekesztípus nem törölhető, mert van hozzá rögzített tétel." });
        db.RekeszTipusok.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
