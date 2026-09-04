# Rekesz — piaci rekeszkövető rendszer (v2)

A rendszerterv: `rekesz_terv_v2.md` / `rekesz_terv_v2.pdf` (korábban átadva).
Ez a projekt annak a tervnek a megvalósítása.

## Amit ez a csomag tartalmaz

- `RekeszAppBackend/` — .NET 8 WebAPI, Entity Framework Core, MySQL (Pomelo).
  A séma **ténylegesen tesztelve lett egy valódi, futó MySQL szerveren** (a
  fejlesztés során), a pontos példaadatokkal (Zsolti/burgonya/10db,
  hdu-925/burgonya/3db törött rekesszel stb.) — mindhárom rekeszegyenleg-riport
  helyes eredményt adott.
- `RekeszAppFrontend/` — Vue 3 + Vite + Pinia + Vue Router. **Ez ténylegesen
  build-elve és böngészőben ellenőrizve lett** (`npm run build` hibamentes,
  mindhárom fő képernyő JS-hiba nélkül renderel).

## ⚠️ Fontos korlát, amiről tudnod kell

A backendet **nem tudtam `dotnet build`-elni** a fejlesztői környezetemben,
mert a NuGet.org nincs onnan elérve. A kód gondosan, a korábbi (nálad már
ténylegesen működő) projekt bevált mintáit követve készült, és a MySQL séma
minden része valós adatbázison le lett tesztelve — de az első `dotnet build`
és `dotnet ef database update` lefuttatása nálad lesz az első valódi
fordítási próba. Ha bármi hibát dob, küldd el ide, és javítjuk, ugyanúgy,
ahogy eddig is.

## Telepítés

### 1. Backend

```powershell
cd RekeszAppBackend
dotnet restore
dotnet build
```

Az `appsettings.json` már XAMPP-nak megfelelően van beállítva (root, jelszó
nélkül, `rekeszkezelo` adatbázisnév):
```
Server=localhost;Port=3306;Database=rekeszkezelo;User=root;Password=;
```
Ha nálad más az adatbázisnév vagy van jelszó, itt írd át.

Az adatbázis és a séma automatikusan létrejön induláskor:
```powershell
dotnet run
```
Ez létrehozza a `rekeszkezelo` adatbázist, lefuttatja a migrációt, és
felveszi a kezdő adminfelhasználót (`admin` / `admin` — **élesítés előtt
mindenképp változtasd meg**), valamint az M10/M30 rekesztípusokat.

Ha külön, az alkalmazás elindítása nélkül akarod lefuttatni a migrációt:
```powershell
dotnet ef database update
```

Az API alapértelmezetten a `http://localhost:5030` címen fut, Swagger UI-jal
fejlesztői módban (`/swagger`).

### 2. Frontend

```powershell
cd RekeszAppFrontend
npm install
npm run dev
```

Ez elindítja a Vite dev szervert `http://localhost:5173`-on. A backend CORS
konfigurációja már fel van készítve erre a portra.

Bejelentkezés: **admin / admin**.

## A három fül

- **Felvásárlás** — eladótól vásárolt tételek, vagy **saját termés** (checkbox — ilyenkor nincs eladó, és nincs "adott rekesz" mező, mert nincs kinek visszaadni). Fizetve + Egységár (Ft/db, opcionális, saját árunál becsült önköltségként) + Adott üres rekesz (db) mezőkkel. Az eladó/zöldség/rekesztípus mellett lévő ✏️ ikon nyitja a kezelő ablakot (felvitel/szerkesztés/törlés). A zöldségnél megadható egy **alapértelmezett rekesztípus** — ha ez be van állítva, a zöldség kiválasztásakor a rendszer automatikusan felajánlja azt a rekesztípust (marad felülírható).
- **Eladás** — vevőnek eladott tételek. Fizetve / Elvitte / Visszahozott rekesz (db) / A hiányzó rekeszt kifizette mezőkkel — ezek a checkboxok (és a visszahozott darabszám) a listában **azonnal** menti az állapotot, nem kell külön menteni. "Ismeretlen / új vevő" módban névvel vagy anélkül (pl. csak rendszámmal) rögzíthető egy alkalmi vevő. A **"📦 Kocsin lévő áru"** gomb megnyit egy ablakot, ami megmutatja, aznap zöldségenként/rekesztípusonként mennyi lett felvéve és mennyi maradt még el nem adva.
- **Egyenleg** — **részletes könyvelés** dátumszűrővel: bevétel, kiadás (felvásárolt + saját becsült önköltség bontásban), nyereség, zöldségenkénti eladási bontás, figyelmeztetés az árat nélkülöző tételekről. Emellett a három rekesz-riport: Mi tartozunk (eladóknak), Nekünk tartoznak (vevők), Kifizetett rekeszveszteség.

## Reszponzivitás

A felület iPad és telefon használatra lett optimalizálva: 16px-es beviteli mezők (iOS Safari zoom-bug elkerülése), min. 40px-es érintési célpontok, összecsukódó form-elrendezés keskeny képernyőn, és görgethető táblázatok ott, ahol sok oszlop van. Ez ténylegesen tesztelve lett Playwrighttal 390px (telefon), 820px (iPad) és 1280px (asztali) szélességben — mindhárom nézeten, vízszintes túlfolyás nélkül.

## Struktúra

```
RekeszAppBackend/
  Domain/Entities.cs          — a 6 tábla (User, Partner, Vevo, Zoldseg, RekeszTipus, FelvasarlasTetel, EladasTetel)
  Data/AppDbContext.cs
  Data/AppDbContextFactory.cs — dotnet ef migrációhoz, appsettings.json-t olvassa
  Data/DbInitializer.cs       — automatikus migráció + seed induláskor
  Controllers/                — Auth, Torzsadatok (Partnerek/Vevek/Zoldsegek/RekeszTipusok), Felvasarlas, Eladas, Riportok
  Migrations/                 — InitialCreate (kézzel írva, séma valós MySQL-en tesztelve)

RekeszAppFrontend/
  src/views/                  — LoginView, FelvasarlasView, EladasView, EgyenlegView
  src/components/TorzsadatModal.vue — újrafelhasználható lista+CRUD modal a 4 törzsadathoz
  src/stores/auth.js, src/api/client.js, src/router/index.js
```
