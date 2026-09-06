using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RekeszAppBackend.Data;

namespace RekeszAppBackend.Controllers;

[ApiController]
[Route("api/egyenleg")]
[Authorize]
public class EgyenlegController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        var eladok = await db.FelvasarlasTetelek
            .Where(x => !x.SajatTermek && x.PartnerId != null && (!x.Fizetve || x.Mennyiseg > x.AdottRekeszDb))
            .Include(x => x.Partner).Include(x => x.Zoldseg).Include(x => x.RekeszTipus)
            .OrderBy(x => x.Partner!.Nev).ThenBy(x => x.RekeszTipus.Nev).ThenBy(x => x.Datum).ThenBy(x => x.Id)
            .ToListAsync();

        var vevok = await db.EladasTetelek
            .Where(x => !x.Fizetve || (x.Mennyiseg - x.VisszahozottDb - x.HianyFizettDb) > 0)
            .Include(x => x.Vevo).Include(x => x.Zoldseg).Include(x => x.RekeszTipus)
            .OrderBy(x => x.Vevo!.Nev).ThenBy(x => x.RekeszTipus.Nev).ThenBy(x => x.Datum).ThenBy(x => x.Id)
            .ToListAsync();

        var eladoCsoportok = eladok
            .GroupBy(x => new { x.PartnerId, PartnerNev = x.Partner!.Nev, x.RekeszTipusId, RekeszTipus = x.RekeszTipus.Nev })
            .Select(g => new
            {
                partnerId = g.Key.PartnerId,
                partnerNev = g.Key.PartnerNev,
                rekeszTipusId = g.Key.RekeszTipusId,
                rekeszTipus = g.Key.RekeszTipus,
                rekeszDb = g.Sum(x => Math.Max(0, x.Mennyiseg - x.AdottRekeszDb)),
                osszeg = g.Where(x => !x.Fizetve && x.Egysegar.HasValue).Sum(x => x.Mennyiseg * x.Egysegar!.Value),
                tetelek = g.Select(x => new
                {
                    x.Id, x.Datum, x.NapiSorszam, zoldsegNev = x.Zoldseg.Nev, x.Mennyiseg, x.Egysegar,
                    osszeg = !x.Fizetve && x.Egysegar.HasValue ? x.Mennyiseg * x.Egysegar.Value : 0m,
                    x.Fizetve, hozottDb = x.AdottRekeszDb,
                    nyitottRekeszDb = Math.Max(0, x.Mennyiseg - x.AdottRekeszDb),
                    teljesMennyiseg = x.Mennyiseg
                }).ToList()
            }).ToList();

        var vevoCsoportok = vevok
            .GroupBy(x => new { x.VevoId, VevoNev = x.Vevo != null ? x.Vevo.Nev : null, x.RekeszTipusId, RekeszTipus = x.RekeszTipus.Nev })
            .Select(g => new
            {
                vevoId = g.Key.VevoId,
                vevoNev = g.Key.VevoNev,
                rekeszTipusId = g.Key.RekeszTipusId,
                rekeszTipus = g.Key.RekeszTipus,
                rekeszDb = g.Sum(x => Math.Max(0, x.Mennyiseg - x.VisszahozottDb - x.HianyFizettDb)),
                osszeg = g.Where(x => !x.Fizetve && x.Egysegar.HasValue).Sum(x => x.Mennyiseg * x.Egysegar!.Value),
                tetelek = g.Select(x => new
                {
                    x.Id, x.Datum, x.NapiSorszam, zoldsegNev = x.Zoldseg.Nev, x.Mennyiseg, x.Egysegar,
                    osszeg = !x.Fizetve && x.Egysegar.HasValue ? x.Mennyiseg * x.Egysegar.Value : 0m,
                    x.Fizetve, hozottDb = x.VisszahozottDb,
                    nyitottRekeszDb = Math.Max(0, x.Mennyiseg - x.VisszahozottDb - x.HianyFizettDb),
                    teljesMennyiseg = x.Mennyiseg
                }).ToList()
            }).ToList();

        return Ok(new { eladok = eladoCsoportok, vevok = vevoCsoportok });
    }

    public record AllapotRequest(int HozottDb, bool Fizetve, bool TeljesMennyiseg);

    [HttpPatch("elado/{id:int}")]
    public async Task<IActionResult> EladoAllapot(int id, AllapotRequest request)
    {
        var entity = await db.FelvasarlasTetelek.FindAsync(id);
        if (entity is null || entity.SajatTermek || entity.PartnerId is null) return NotFound();
        var hozott = request.TeljesMennyiseg ? entity.Mennyiseg : request.HozottDb;
        if (hozott < 0 || hozott > entity.Mennyiseg)
            return BadRequest(new { message = "A hozott rekesz mennyisége 0 és a teljes mennyiség között kell legyen." });
        entity.AdottRekeszDb = hozott;
        entity.Fizetve = request.Fizetve;
        await db.SaveChangesAsync();
        return Ok(new { entity.Id, entity.AdottRekeszDb, entity.Fizetve });
    }

    [HttpPatch("vevo/{id:int}")]
    public async Task<IActionResult> VevoAllapot(int id, AllapotRequest request)
    {
        var entity = await db.EladasTetelek.FindAsync(id);
        if (entity is null) return NotFound();
        var hozott = request.TeljesMennyiseg ? entity.Mennyiseg : request.HozottDb;
        if (hozott < 0 || hozott > entity.Mennyiseg)
            return BadRequest(new { message = "A hozott rekesz mennyisége 0 és a teljes mennyiség között kell legyen." });
        entity.VisszahozottDb = hozott;
        if (entity.HianyFizettDb > entity.Mennyiseg - entity.VisszahozottDb)
            entity.HianyFizettDb = entity.Mennyiseg - entity.VisszahozottDb;
        entity.Fizetve = request.Fizetve;
        await db.SaveChangesAsync();
        return Ok(new { entity.Id, entity.VisszahozottDb, entity.Fizetve });
    }
}
