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
    // Eladóknak tartozunk: Σ(Mennyiseg - AdottRekeszDb) partnerenként és rekesztípusonként.
    // Saját termék (és az áthozott) tételeknek nincs partnere, azok nem termelnek rekesztartozást.
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

    // Nekünk tartoznak: Σ((Mennyiseg - VisszahozottDb) - HianyFizettDb) vevőnként és rekesztípusonként
    // - azaz a vissza nem hozott rekeszekből az, amit se rekeszben, se készpénzben nem rendeztek.
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

    // Kifizetett rekeszveszteség - azok a tételek, ahol a vevő készpénzben rendezte a hiányzó rekeszt.
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

    // Kocsi tartalma: adott napon felvásárolt (ide értve az áthozott tételeket is) mennyiség mínusz
    // az aznap eladott mennyiség, zöldségenként és rekesztípusonként. Ez mutatja, mennyi maradt a kocsin.
    [Authorize(Roles = "Admin")]
    [HttpGet("keszlet")]
    public async Task<IActionResult> Keszlet([FromQuery] DateOnly? datum)
    {
        var d = datum ?? DateOnly.FromDateTime(DateTime.Today);

        var felvasarolt = await db.FelvasarlasTetelek
            .Where(x => x.Datum == d && x.Helyszin == FelvasarlasHelyszin.Kocsi)
            .Include(x => x.Zoldseg).Include(x => x.RekeszTipus)
            .GroupBy(x => new { x.ZoldsegId, ZoldsegNev = x.Zoldseg.Nev, x.RekeszTipusId, RekeszTipusNev = x.RekeszTipus.Nev })
            .Select(g => new { g.Key.ZoldsegId, g.Key.ZoldsegNev, g.Key.RekeszTipusId, g.Key.RekeszTipusNev, Mennyiseg = g.Sum(x => x.Mennyiseg) })
            .ToListAsync();

        var eladott = await db.EladasTetelek
            .Where(x => x.Datum == d)
            .GroupBy(x => new { x.ZoldsegId, x.RekeszTipusId })
            .Select(g => new { g.Key.ZoldsegId, g.Key.RekeszTipusId, Mennyiseg = g.Sum(x => x.Mennyiseg) })
            .ToListAsync();

        var sorok = felvasarolt
            .Select(f => new
            {
                zoldsegId = f.ZoldsegId,
                zoldsegNev = f.ZoldsegNev,
                rekeszTipusId = f.RekeszTipusId,
                rekeszTipus = f.RekeszTipusNev,
                felvasarolva = f.Mennyiseg,
                eladva = eladott.Where(e => e.ZoldsegId == f.ZoldsegId && e.RekeszTipusId == f.RekeszTipusId).Sum(e => e.Mennyiseg),
                kocsinMaradt = f.Mennyiseg - eladott.Where(e => e.ZoldsegId == f.ZoldsegId && e.RekeszTipusId == f.RekeszTipusId).Sum(e => e.Mennyiseg)
            })
            .OrderBy(x => x.zoldsegNev)
            .ToList();

        return Ok(sorok);
    }

    // Rekesz-mennyiség részletező: rekesztípusonként összesítve, hány rekesz került ki aznap a kocsira
    // (saját + felvásárolt + áthozott, zöldségtől függetlenül), és ebből ténylegesen hány jött vissza
    // az aznapi eladásokon keresztül. Pl. "M10: 110/97".
    [Authorize(Roles = "Admin")]
    [HttpGet("rekeszreszletezo")]
    public async Task<IActionResult> RekeszReszletezo([FromQuery] DateOnly? datum)
    {
        var d = datum ?? DateOnly.FromDateTime(DateTime.Today);

        var kocsira_kerult = await db.FelvasarlasTetelek
            .Where(x => x.Datum == d && x.Helyszin == FelvasarlasHelyszin.Kocsi)
            .Include(x => x.RekeszTipus)
            .GroupBy(x => new { x.RekeszTipusId, RekeszTipusNev = x.RekeszTipus.Nev })
            .Select(g => new { g.Key.RekeszTipusId, g.Key.RekeszTipusNev, Osszesen = g.Sum(x => x.Mennyiseg) })
            .ToListAsync();

        var visszahozott = await db.EladasTetelek
            .Where(x => x.Datum == d)
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

    // Részletes pénzügyi könyvelés: bevétel, kiadás, nyereség egy időszakra.
    // Az áthozott (előző napról átvitt) felvásárlás-sorok nem valódi vásárlások, ezért kimaradnak a kiadásból.
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
