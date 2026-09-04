using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RekeszAppBackend.Contracts;
using RekeszAppBackend.Data;
using RekeszAppBackend.Domain;

namespace RekeszAppBackend.Controllers;

[ApiController]
[Route("api/eladas")]
[Authorize(Roles = "Admin")]
public class EladasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Lista([FromQuery] DateOnly? datum, [FromQuery] string? q)
    {
        var d = datum ?? DateOnly.FromDateTime(DateTime.Today);
        var query = db.EladasTetelek.Where(x => x.Datum == d);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            if (int.TryParse(term, out var sorszam))
                query = query.Where(x => x.NapiSorszam == sorszam || x.Zoldseg.Nev.Contains(term) || (x.Vevo != null && x.Vevo.Nev != null && x.Vevo.Nev.Contains(term)));
            else
                query = query.Where(x => x.Zoldseg.Nev.Contains(term) || (x.Vevo != null && ((x.Vevo.Nev != null && x.Vevo.Nev.Contains(term)) || (x.Vevo.Megjegyzes != null && x.Vevo.Megjegyzes.Contains(term)))));
        }
        var tetelek = await query.Include(x => x.Vevo).Include(x => x.Zoldseg).Include(x => x.RekeszTipus)
            .OrderBy(x => x.NapiSorszam)
            .Select(x => new { x.Id, x.NapiSorszam, x.Datum, x.Ido, x.VevoId, vevoNev = x.Vevo != null ? x.Vevo.Nev : null, x.ZoldsegId, zoldsegNev = x.Zoldseg.Nev, zoldsegKepUrl = x.Zoldseg.KepUrl, x.RekeszTipusId, rekeszTipus = x.RekeszTipus.Nev, x.Mennyiseg, x.Fizetve, x.Elvitte, x.VisszahozottDb, x.HianyFizettDb, x.Egysegar, x.Megjegyzes }).ToListAsync();
        return Ok(tetelek);
    }

    private static IActionResult? Validal(EladasRequest r)
    {
        if (r.Mennyiseg <= 0) return new BadRequestObjectResult(new { message = "A mennyiség legalább 1 kell legyen." });
        if (r.Egysegar is null || r.Egysegar <= 0) return new BadRequestObjectResult(new { message = "Az egységár megadása kötelező." });
        if (r.VisszahozottDb < 0 || r.VisszahozottDb > r.Mennyiseg) return new BadRequestObjectResult(new { message = "A visszahozott mennyiség 0 és a teljes mennyiség között kell legyen." });
        var hiany = r.Mennyiseg - r.VisszahozottDb;
        if (r.HianyFizettDb < 0 || r.HianyFizettDb > hiany) return new BadRequestObjectResult(new { message = "A kifizetett hiány nem lehet negatív, és nem lehet több, mint a vissza nem hozott rekeszek száma." });
        return null;
    }

    [HttpPost]
    public async Task<IActionResult> Uj(EladasRequest request)
    {
        var hiba = Validal(request);
        if (hiba is not null) return hiba;
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => { await using var tx = await db.Database.BeginTransactionAsync(); int? vevoId=request.VevoId; var vanUjVevoAdat=!string.IsNullOrWhiteSpace(request.UjVevoNev)||!string.IsNullOrWhiteSpace(request.UjVevoMegjegyzes); if(vevoId is null&&vanUjVevoAdat){var ujVevo=new Vevo{Nev=string.IsNullOrWhiteSpace(request.UjVevoNev)?null:request.UjVevoNev!.Trim(),Megjegyzes=string.IsNullOrWhiteSpace(request.UjVevoMegjegyzes)?null:request.UjVevoMegjegyzes!.Trim()};db.Vevek.Add(ujVevo);await db.SaveChangesAsync();vevoId=ujVevo.Id;} var datum=request.Datum??DateOnly.FromDateTime(DateTime.Today);var napiSorszam=(await db.EladasTetelek.Where(x=>x.Datum==datum).MaxAsync(x=>(int?)x.NapiSorszam)??0)+1;var entity=new EladasTetel{NapiSorszam=napiSorszam,Datum=datum,Ido=DateTime.Now,VevoId=vevoId,ZoldsegId=request.ZoldsegId,RekeszTipusId=request.RekeszTipusId,Mennyiseg=request.Mennyiseg,Fizetve=request.Fizetve,Elvitte=request.Elvitte,VisszahozottDb=request.VisszahozottDb,HianyFizettDb=request.HianyFizettDb,Egysegar=request.Egysegar,Megjegyzes=string.IsNullOrWhiteSpace(request.Megjegyzes)?null:request.Megjegyzes!.Trim()};db.EladasTetelek.Add(entity);await db.SaveChangesAsync();await tx.CommitAsync();return Created($"api/eladas/{entity.Id}",entity);});
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Modosit(int id, EladasRequest request)
    {
        var entity = await db.EladasTetelek.FindAsync(id);
        if (entity is null) return NotFound();
        var hiba = Validal(request);
        if (hiba is not null) return hiba;
        entity.VevoId=request.VevoId;entity.ZoldsegId=request.ZoldsegId;entity.RekeszTipusId=request.RekeszTipusId;entity.Mennyiseg=request.Mennyiseg;entity.Fizetve=request.Fizetve;entity.Elvitte=request.Elvitte;entity.VisszahozottDb=request.VisszahozottDb;entity.HianyFizettDb=request.HianyFizettDb;entity.Egysegar=request.Egysegar;entity.Megjegyzes=string.IsNullOrWhiteSpace(request.Megjegyzes)?null:request.Megjegyzes!.Trim();
        await db.SaveChangesAsync();
        return Ok(entity);
    }

    public record AllapotRequest(bool? Fizetve, bool? Elvitte, int? VisszahozottDb, int? HianyFizettDb);

    [HttpPatch("{id:int}/allapot")]
    public async Task<IActionResult> Allapot(int id, AllapotRequest request)
    {
        var entity = await db.EladasTetelek.FindAsync(id);
        if (entity is null) return NotFound();
        if (request.Fizetve.HasValue) entity.Fizetve = request.Fizetve.Value;
        if (request.Elvitte.HasValue) entity.Elvitte = request.Elvitte.Value;
        if (request.VisszahozottDb.HasValue)
        {
            if (request.VisszahozottDb.Value < 0 || request.VisszahozottDb.Value > entity.Mennyiseg)
                return BadRequest(new { message = "A visszahozott mennyiség 0 és a teljes mennyiség között kell legyen." });
            entity.VisszahozottDb = request.VisszahozottDb.Value;
            // ha a visszahozott mennyiség csökken, a korábban rögzített hiány-kifizetés nem lehet nagyobb az új hiánynál
            var ujHiany = entity.Mennyiseg - entity.VisszahozottDb;
            if (entity.HianyFizettDb > ujHiany) entity.HianyFizettDb = ujHiany;
        }
        if (request.HianyFizettDb.HasValue)
        {
            var hiany = entity.Mennyiseg - entity.VisszahozottDb;
            if (request.HianyFizettDb.Value < 0 || request.HianyFizettDb.Value > hiany)
                return BadRequest(new { message = "A kifizetett hiány nem lehet negatív, és nem lehet több, mint a vissza nem hozott rekeszek száma." });
            entity.HianyFizettDb = request.HianyFizettDb.Value;
        }
        await db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Torol(int id)
    {
        var entity = await db.EladasTetelek.FindAsync(id);
        if (entity is null) return NotFound();
        db.EladasTetelek.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
