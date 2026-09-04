using RekeszAppBackend.Domain;

namespace RekeszAppBackend.Contracts;

public record LoginRequest(string Felhasznalonev, string Jelszo);

public record PartnerRequest(string Nev, string? Megjegyzes);
public record VevoRequest(string? Nev, string? Megjegyzes);
public record ZoldsegRequest(string Nev, int? AlapertelmezettRekeszTipusId);
public record RekeszTipusRequest(string Nev);

public record FelvasarlasRequest(
    bool SajatTermek,
    int? PartnerId,
    int ZoldsegId,
    int RekeszTipusId,
    int Mennyiseg,
    bool Fizetve,
    int AdottRekeszDb,
    decimal? Egysegar,
    string? Megjegyzes,
    DateOnly? Datum,
    FelvasarlasHelyszin Helyszin = FelvasarlasHelyszin.Kocsi
);

// Kocsin maradt áru átvitele a következő (vagy tetszőleges) napra: automatikus felvásárlás-sort hoz létre.
public record AtvitelRequest(
    int ZoldsegId,
    int RekeszTipusId,
    int Mennyiseg,
    DateOnly CelDatum
);

// Raktáron lévő áru (részleges vagy teljes mennyiség) áthelyezése a kocsira, egy adott napra.
// Egy konkrét raktári tételre hivatkozik (nem zöldség/rekesztípus alapú FIFO-kereséssel),
// mert a raktár nézet mostantól tételenként (eladó, ár is látszik), nem összesítve jelenik meg.
public record RaktarAthelyezesRequest(
    int FelvasarlasTetelId,
    int Mennyiseg,
    DateOnly CelDatum
);

public record EladasRequest(
    int? VevoId,
    string? UjVevoNev,
    string? UjVevoMegjegyzes,
    int ZoldsegId,
    int RekeszTipusId,
    int Mennyiseg,
    bool Fizetve,
    bool Elvitte,
    int VisszahozottDb,
    int HianyFizettDb,
    decimal? Egysegar,
    string? Megjegyzes,
    DateOnly? Datum
);
