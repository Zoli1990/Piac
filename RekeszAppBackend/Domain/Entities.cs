namespace RekeszAppBackend.Domain;

public enum FelhasznaloSzerepkor { Admin, Vendeg }

// Hol van fizikailag az áru: a kocsin (eladásra kész) vagy a raktárban (tárolva, még nem a kocsin).
// A raktár funkcionálisan "egy másik kocsinak" tekintendő - ugyanúgy valódi kiadás, csak nem eladható,
// amíg át nem kerül a Kocsi helyszínre.
public enum FelvasarlasHelyszin { Kocsi, Raktar }

public class User
{
    public int Id { get; set; }
    public string Felhasznalonev { get; set; } = "";
    public string JelszoHash { get; set; } = "";
    public FelhasznaloSzerepkor Role { get; set; }
}

// Eladók, akiktől felvásárolunk. Szűk kör (4-5 állandó), bővíthető.
public class Partner
{
    public int Id { get; set; }
    public string Nev { get; set; } = "";
    public string? Megjegyzes { get; set; }
}

// Akiknek eladunk. Lehet állandó (valódi név) vagy alkalmi (pl. rendszám), akár üres névvel is.
public class Vevo
{
    public int Id { get; set; }
    public string? Nev { get; set; }
    public string? Megjegyzes { get; set; }
}

// Törzsadat: burgonya, paprika, stb.
public class Zoldseg
{
    public int Id { get; set; }
    public string Nev { get; set; } = "";
    // A felviteléskor megadott, szokásos rekesztípus - Eladásnál ez tölti ki alapértelmezésként a választót.
    public int? AlapertelmezettRekeszTipusId { get; set; }
    public RekeszTipus? AlapertelmezettRekeszTipus { get; set; }
    // A zöldségtípushoz (kategóriához) tartozó kép, pl. felvásárláskor a wizardban készített fotó.
    // Kategória-szintű, nem tételenkénti: minden "burgonya" felvásárlás ugyanazt a képet kapja,
    // függetlenül attól, melyik eladótól vettük. Az utoljára feltöltött kép felülírja a korábbit.
    public string? KepUrl { get; set; }
}

// Törzsadat, adatbázisban tárolt, nem szabad szöveg. Jelenleg M10, M30.
public class RekeszTipus
{
    public int Id { get; set; }
    public string Nev { get; set; } = "";
}

// Bejövő tétel: amikor mi veszünk egy Partnertől.
// Nincs "Elvitte" mező: amit felvásárolunk, azt rögtön magunkkal is visszük.
// Nincs "hiány kifizetve" mező: a Partnerekben megbízunk, utólag szerkeszthető a rekord.
public class FelvasarlasTetel
{
    public int Id { get; set; }
    public int NapiSorszam { get; set; }
    public DateOnly Datum { get; set; }
    public DateTime Ido { get; set; }
    // Saját áru esetén nincs partner (nem vásároltuk senkitől).
    public bool SajatTermek { get; set; }
    public int? PartnerId { get; set; }
    public Partner? Partner { get; set; }
    public int ZoldsegId { get; set; }
    public Zoldseg Zoldseg { get; set; } = null!;
    public int RekeszTipusId { get; set; }
    public RekeszTipus RekeszTipus { get; set; } = null!;
    public int Mennyiseg { get; set; }
    public bool Fizetve { get; set; }
    public int AdottRekeszDb { get; set; }
    // Vásárolt árunál a beszerzési ár, saját árunál opcionális becsült önköltség. Ft/db.
    public decimal? Egysegar { get; set; }
    public string? Megjegyzes { get; set; }
    // Igaz, ha ez a sor egy előző napról "átvitt" (kocsin maradt) áru, VAGY raktárból a kocsira
    // áthelyezett áru automatikusan létrehozott felvásárlás-sora. Nem valódi (új) vásárlás,
    // ezért a könyvelés kiadás-számításából kizárva - a pénz már elszámolva lett az eredeti soron.
    public bool Athozott { get; set; }
    // Kocsi (eladásra kész, alapértelmezett) vagy Raktár (fizikailag tárolva, még nem a kocsin).
    public FelvasarlasHelyszin Helyszin { get; set; } = FelvasarlasHelyszin.Kocsi;
    // Raktári tételnél: eddig összesen hány db-ot helyeztek át ebből a sorból a kocsira.
    // A raktáron ténylegesen maradt mennyiség = Mennyiseg - AthelyezveDb.
    public int AthelyezveDb { get; set; }
}

// Kimenő tétel: amikor mi adunk el egy Vevőnek.
// Részletesebb állapotkezelés, mert a vevőt sokszor nem ismerjük, nincs utólagos önkéntes rendezés.
public class EladasTetel
{
    public int Id { get; set; }
    public int NapiSorszam { get; set; }
    public DateOnly Datum { get; set; }
    public DateTime Ido { get; set; }
    public int? VevoId { get; set; }
    public Vevo? Vevo { get; set; }
    public int ZoldsegId { get; set; }
    public Zoldseg Zoldseg { get; set; } = null!;
    public int RekeszTipusId { get; set; }
    public RekeszTipus RekeszTipus { get; set; } = null!;
    public int Mennyiseg { get; set; }
    public bool Fizetve { get; set; }
    public bool Elvitte { get; set; }
    public int VisszahozottDb { get; set; }
    // Hány darab hiányzó (vissza nem hozott) rekeszt fizetett ki készpénzben a vevő.
    // 0 = semennyit (nyitott tartozás), Mennyiseg-VisszahozottDb = a teljes hiányt kifizette.
    public int HianyFizettDb { get; set; }
    // Eladási ár, Ft/db - a könyveléshez (bevétel számításához).
    public decimal? Egysegar { get; set; }
    public string? Megjegyzes { get; set; }
}
