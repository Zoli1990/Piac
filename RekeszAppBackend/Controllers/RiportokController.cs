using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RekeszAppBackend.Data;
using RekeszAppBackend.Domain;

namespace RekeszAppBackend.Controllers;

[ApiController]
[Route("api/riportok")]
[Authorize]
public class RiportokController(AppDbContext db) : ControllerBase
{
    [HttpGet("mi-tartozunk")]
    public async Task<IActionResult> MiTartozunk()
    {
        var sorok = await db.FelvasarlasTetelek
            .Where(x => !x.SajatTermek && x.PartnerId != null)
            .Include(x => x.Partner).Include(x => x.RekeszTipus)
            .GroupBy(x => new { x.PartnerId, PartnerNev = x.Partner!.Nev, x.RekeszTipusId, RekeszTipusNev = x.RekeszTipus.Nev })
            .Select(g => new
            {
                partnerId = g.Key.PartnerId,
                partnerNev = g.Key.PartnerNev,
                rekeszTipusId = g.Key.RekeszTipusId,
                rekeszTipus = g.Key.RekeszTipusNev,
                mennyiseg = g.Sum(x => x.Mennyiseg - x.AdottRekeszDb)
            })
            .Where(x => x.mennyiseg != 0)
            .OrderBy(x => x.partnerNev).ThenBy(x => x.rekeszTipus)
            .ToListAsync();
        return Ok(sorok);
    }

    [HttpGet("nekunk-tartoznak")]
    public async Task<IActionResult> NekunkTartoznak()
    {
        var sorok = await db.EladasTetelek
            .Include(x => x.Vevo).Include(x => x.RekeszTipus)
            .GroupBy(x => new { x.VevoId, VevoNev = x.Vevo != null ? x.Vevo.Nev : null, x.RekeszTipusId, RekeszTipusNev = x.RekeszTipus.Nev })
            .Select(g => new
            {
                vevoId = g.Key.VevoId,
                vevoNev = g.Key.VevoNev,
                rekeszTipusId = g.Key.RekeszTipusId,
                rekeszTipus = g.Key.RekeszTipusNev,
                mennyiseg = g.Sum(x => (x.Mennyiseg - x.VisszahozottDb) - x.HianyFizettDb)
            })
            .Where(x => x.mennyiseg != 0)
            .OrderBy(x => x.vevoNev).ThenBy(x => x.rekeszTipus)
            .ToListAsync();
        return Ok(sorok);
    }

    [HttpGet("rekeszveszteseg")]
    public async Task<IActionResult> Rekeszveszteseg()
    {
        var sorok = await db.EladasTetelek
            .Where(x => x.HianyFizettDb > 0)
            .Include(x => x.Vevo).Include(x => x.Zoldseg).Include(x => x.RekeszTipus)
            .OrderByDescending(x => x.Datum).ThenByDescending(x => x.NapiSorszam)
            .Select(x => new
            {
                x.Id,
                x.NapiSorszam,
                x.Datum,
                vevoNev = x.Vevo != null ? x.Vevo.Nev : null,
                zoldsegNev = x.Zoldseg.Nev,
                rekeszTipus = x.RekeszTipus.Nev,
                hianyzoDb = x.HianyFizettDb,
                x.Megjegyzes
            })
            .ToListAsync();
        return Ok(sorok);
    }

    // Kocsi készlet zöldség + rekesztípus szerint csoportosítva.
    // Az eladás nem hivatkozik konkrét felvásárlási tételre, és erre nincs is szükség:
    // a készletet darabszám alapján követjük. Vételáras készletértékelést nem végzünk.
    [Authorize(Roles = "Admin")]
    [HttpGet("keszlet")]
    public async Task<IActionResult> Keszlet([FromQuery] DateOnly? datum)
    {
        var d = datum ?? DateOnly.FromDateTime(DateTime.Today);

        var felvasarolt = await db.FelvasarlasTetelek
            .Where(x => x.Datum == d && x.Helyszin == FelvasarlasHelyszin.Kocsi)
            .Include(x => x.Zoldseg).Include(x => x.RekeszTipus)
            .Select(x => new
            {
                x.Id,
                x.ZoldsegId,
                zoldsegNev = x.Zoldseg.Nev,
                x.RekeszTipusId,
                rekeszTipus = x.RekeszTipus.Nev,
                x.Mennyiseg,
                x.Athozott
            })
            .ToListAsync();

        var eladott = await db.EladasTetelek
            .Where(x => x.Datum == d)
            .GroupBy(x => new { x.ZoldsegId, x.RekeszTipusId })
            .Select(g => new { g.Key.ZoldsegId, g.Key.RekeszTipusId, Mennyiseg = g.Sum(x => x.Mennyiseg) })
            .ToListAsync();

        var sorok = felvasarolt
            .GroupBy(x => new { x.ZoldsegId, x.zoldsegNev, x.RekeszTipusId, x.rekeszTipus })
            .Select(g =>
            {
                var felvasarolva = g.Sum(x => x.Mennyiseg);
                var eladva = eladott.FirstOrDefault(e => e.ZoldsegId == g.Key.ZoldsegId && e.RekeszTipusId == g.Key.RekeszTipusId)?.Mennyiseg ?? 0;
                var kocsinMaradt = Math.Max(0, felvasarolva - eladva);

                return new
                {
                    zoldsegId = g.Key.ZoldsegId,
                    zoldsegNev = g.Key.zoldsegNev,
                    rekeszTipusId = g.Key.RekeszTipusId,
                    rekeszTipus = g.Key.rekeszTipus,
                    felvasarolva,
                    eladva,
                    kocsinMaradt,
                    forrasTetelek = g.Select(x => new
                    {
                        id = x.Id,
                        eredetiMennyiseg = x.Mennyiseg,
                        athozott = x.Athozott
                    }).ToList()
                };
            })
            .Where(x => x.kocsinMaradt > 0)
            .OrderBy(x => x.zoldsegNev).ThenBy(x => x.rekeszTipus)
            .ToList();

        return Ok(sorok);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("rekeszreszletezo")]
    public async Task<IActionResult> RekeszReszletezo([FromQuery] DateOnly? datum)
    {
        var kocsira_kerult = await db.FelvasarlasTetelek
            .Where(x => x.Datum == (datum ?? DateOnly.FromDateTime(DateTime.Today)) && x.Helyszin == FelvasarlasHelyszin.Kocsi)
            .Include(x => x.RekeszTipus)
            .GroupBy(x => new { x.RekeszTipusId, RekeszTipusNev = x.RekeszTipus.Nev })
            .Select(g => new { g.Key.RekeszTipusId, g.Key.RekeszTipusNev, Osszesen = g.Sum(x => x.Mennyiseg) })
            .ToListAsync();

        var visszahozott = await db.EladasTetelek
            .Where(x => x.Datum == (datum ?? DateOnly.FromDateTime(DateTime.Today)))
            .GroupBy(x => x.RekeszTipusId)
            .Select(g => new { RekeszTipusId = g.Key, Visszahozott = g.Sum(x => x.VisszahozottDb) })
            .ToListAsync();

        var sorok = kocsira_kerult
            .Select(f => new
            {
                rekeszTipusId = f.RekeszTipusId,
                rekeszTipus = f.RekeszTipusNev,
                osszesen = f.Osszesen,
                visszahozott = visszahozott.Where(v => v.RekeszTipusId == f.RekeszTipusId).Sum(v => v.Visszahozott)
            })
            .OrderBy(x => x.rekeszTipus)
            .ToList();

        return Ok(sorok);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("konyveles")]
    public async Task<IActionResult> Konyveles([FromQuery] DateOnly? tol, [FromQuery] DateOnly? ig)
    {
        var vegIg = ig ?? DateOnly.FromDateTime(DateTime.Today);
        var vegTol = tol ?? vegIg;

        var felvasarlasok = await db.FelvasarlasTetelek
            .Where(x => x.Datum >= vegTol && x.Datum <= vegIg && !x.Athozott)
            .Include(x => x.Zoldseg)
            .ToListAsync();
        var eladasok = await db.EladasTetelek
            .Where(x => x.Datum >= vegTol && x.Datum <= vegIg)
            .Include(x => x.Zoldseg)
            .ToListAsync();

        var kiadasVasarolt = felvasarlasok.Where(x => !x.SajatTermek && x.Egysegar.HasValue).Sum(x => x.Mennyiseg * x.Egysegar!.Value);
        var kiadasSajatBecsult = felvasarlasok.Where(x => x.SajatTermek && x.Egysegar.HasValue).Sum(x => x.Mennyiseg * x.Egysegar!.Value);
        var bevetel = eladasok.Where(x => x.Egysegar.HasValue).Sum(x => x.Mennyiseg * x.Egysegar!.Value);

        var felvasarlasArNelkul = felvasarlasok.Count(x => !x.Egysegar.HasValue);
        var eladasArNelkul = eladasok.Count(x => !x.Egysegar.HasValue);

        var zoldsegenkent = eladasok
            .Where(x => x.Egysegar.HasValue)
            .GroupBy(x => x.Zoldseg.Nev)
            .Select(g => new { zoldseg = g.Key, mennyiseg = g.Sum(x => x.Mennyiseg), bevetel = g.Sum(x => x.Mennyiseg * x.Egysegar!.Value) })
            .OrderByDescending(x => x.bevetel)
            .ToList();

        return Ok(new
        {
            tol = vegTol,
            ig = vegIg,
            bevetel,
            kiadasVasarolt,
            kiadasSajatBecsult,
            kiadasOsszesen = kiadasVasarolt + kiadasSajatBecsult,
            nyereseg = bevetel - kiadasVasarolt - kiadasSajatBecsult,
            felvasarlasTetelSzam = felvasarlasok.Count,
            eladasTetelSzam = eladasok.Count,
            felvasarlasArNelkul,
            eladasArNelkul,
            zoldsegenkent
        });
    }
}
