# Piac / RekeszApp – részletes fejlesztési és üzemeltetési dokumentáció

Ez a dokumentum a projekt aktuális állapotának **tartós fejlesztői naplója és műszaki/üzleti referencia-dokumentuma**. A célja, hogy egy későbbi beszélgetésben vagy fejlesztési szakaszban innen egyértelműen folytathassuk a munkát anélkül, hogy újra végig kellene beszélni a korábbi döntéseket.

> **Aktuális állapot:** a jelenlegi fejlesztési szakasz lezárva, lokális teljes körű tesztelésre előkészítve. A backend jelenleg még nincs élesen publikálva; az új frontend + backend változatot együtt kell tesztelni, és csak sikeres teszt után érdemes élesíteni.

---

## 1. Projekt célja

Az alkalmazás egy piaci/zöldség-kereskedelmi működéshez készült webes rendszer, amelynek fő feladata:

- felvásárlások rögzítése;
- saját termés rögzítése;
- eladások rögzítése;
- rekeszek mozgásának és tartozásának követése;
- kocsi- és raktárkészlet áttekintése;
- eladók és vevők nyilvántartása;
- egyenlegek és tartozások áttekintése;
- egyszerű könyvelési/forgalmi riportok megjelenítése.

A rendszer nem általános vállalatirányítási rendszer. A fejlesztéseknél az elsődleges szempont a tényleges használat egyszerűsége és az üzleti folyamatok pontos követése.

---

## 2. Technológiai felépítés

### Backend

- ASP.NET Core / .NET 8 Web API
- Entity Framework Core
- MySQL
- Pomelo MySQL provider
- REST API
- JWT alapú bejelentkezés/engedélyezés

### Frontend

- Vue 3
- Vite
- Pinia
- Vue Router
- mobil/iPad/asztali használatra optimalizált UI

### Repository

GitHub repository:

`https://github.com/Zoli1990/Piac`

A fő fejlesztési ág:

`main`

A projektben a módosításokat jelenleg közvetlenül a `main` branchbe commitoljuk, mert ez a felhasználóval egyeztetett munkafolyamat.

---

# 3. Jelenlegi fejlesztési mérföldkő

A most lezárt szakasz fő témája:

1. Felvásárlás nézet javítása.
2. Eladás nézet javítása.
3. Egyenleg nézet újratervezése.
4. Kocsi készlet csoportosítása.
5. Raktár készlet csoportosítása.
6. Raktár → kocsi mozgatás kezelése csoportosított készletből.
7. Készlethez kapcsolódó korábbi félreérthető vételár-logika tisztázása.
8. A készletmodell egyszerűsítése: **nem vezetünk be FIFO-t vagy rejtett készletforrás-allokációt.**

A következő lépés elsődlegesen a teljes lokális tesztelés, nem új funkciók azonnali hozzáadása.

---

# 4. FONTOS ÜZLETI SZABÁLYOK

Ezeket a szabályokat a későbbi fejlesztések során is meg kell őrizni, hacsak a felhasználó kifejezetten másként nem kéri.

## 4.1. Felvásárlás

Egy felvásárlási tétel egy konkrét rögzített tranzakció.

Fontos mezők:

- dátum;
- eladó / partner;
- zöldség;
- rekesztípus;
- mennyiség;
- egységár;
- fizetve állapot;
- adott üres rekesz;
- helyszín (Kocsi / Raktár);
- saját termés jelölés;
- megjegyzés.

### Vásárolt áru

Vásárolt árunál az egységár kötelező.

### Saját termés

Saját termésnél nincs eladó. Az ár opcionális, és korábban becsült önköltségként volt használható a könyvelési nézetben.

### Adott üres rekesz

Az „Adott üres rekesz” nem lehet nagyobb a felvásárolt mennyiségnél.

Alapértéke 0.

A mennyiség megváltoztatása **nem írhatja át automatikusan** az adott üres rekesz számát.

---

# 5. Készletmodell – EZ A JELENLEGI VÉGLEGES IRÁNY

## 5.1. Nincs FIFO

A rendszerben **nincs automatikus FIFO készletkezelés**.

Nem szabad később úgy módosítani a rendszert, hogy egy eladás automatikusan a legrégebbi felvásárlásból fogyasszon készletet, hacsak erre a felhasználó külön nem ad utasítást.

## 5.2. Nincs rejtett forrásallokáció

Az `EladasTetel` jelenleg nem tárolja, hogy az eladott áru konkrétan melyik `FelvasarlasTetel` rekordból származott.

Ez szándékosan nem kerül most bevezetésre.

Ennek oka, hogy az üzleti működéshez nincs szükség felvásárlási ár szerinti készletértékelésre, és az eladási ár állandó.

Ezért nem kell megoldani azt a problémát, hogy például:

- Alma M10 – Zsolti – 20 db – 500 Ft
- Alma M10 – Józsi – 15 db – 600 Ft
- eladás – 10 db

esetén pontosan melyik 10 db melyik felvásárlási tételből fogyott.

## 5.3. A készlet szempontjából a darabszám számít

A készlet elsődleges csoportosítási kulcsa:

**zöldség + rekesztípus**

Nem része a csoportosításnak:

- eladó/partner;
- dátum;
- felvásárlási ár.

Példa:

```text
Alma M10 – Zsolti – 20 db
Alma M10 – Józsi  – 15 db

=> Alma M10 készlet: 35 db
```

Az adatbázisban ettől még a két eredeti felvásárlási rekord külön marad.

## 5.4. Eladás

Az eladás csökkenti a készletet az adott:

- zöldség;
- rekesztípus

kombinációban.

A rendszernek nem kell meghatároznia, hogy az eladás melyik felvásárlási rekordból fogyott.

---

# 6. Miért került ki az átlag vételáras készletlogika?

Korábban felmerült, hogy a csoportosított készletnél jelenjen meg súlyozott átlag vételár.

Ez önmagában csak akkor lenne egzakt, ha tudnánk, hogy a jelenlegi készletből mennyi maradt az egyes eredeti felvásárlási tételekből.

Mivel az eladás jelenleg nem tartalmaz ilyen forráskapcsolatot, ezt csak feltételezéssel lehetne kiszámítani.

Korábban felmerült arányos visszaosztás is, de ez csak becslés lenne. FIFO esetén pedig egy új, eddig nem létező üzleti szabályt vezetnénk be.

**A végleges döntés:**

- nem kell készlet-vételárat számolni;
- nem kell forrástétel-allokáció;
- nem kell FIFO;
- nem kell becsült átlag vételár a készlet működéséhez.

Ha a jövőben mégis szükség lenne önköltség- vagy árrés-számításra, azt külön fejlesztési feladatként kell kezelni, és előtte újra meg kell határozni az üzleti szabályt.

---

# 7. Raktár és kocsi

A rendszer két fontos helyszínt használ:

- `Kocsi`
- `Raktar`

A felvásárlásnál a felhasználó kiválaszthatja, hogy az áru hová kerül.

## Kocsi készlet

A kocsi készlet nézet zöldség + rekesztípus szerint csoportosít.

Példa:

```text
Alma M10 – 35 db
Alma M30 – 30 db
```

A partner és dátum nem jelenik meg csoportosítási kulcsként.

## Raktár készlet

A raktárban szintén zöldség + rekesztípus szerinti csoportosított nézet van.

Az eredeti felvásárlási rekordok nem kerülnek összevonásra az adatbázisban.

### Raktár → kocsi mozgatás

A csoportosított raktárkártya önmagában nem egy adatbázisrekord, ezért **nem szabad szintetikus csoport-ID-t küldeni backend művelethez**.

Ha egy csoport több eredeti felvásárlási tételből áll, a frontendnek ki kell választania az eredeti forrástételt, és annak valódi ID-ját kell elküldenie a mozgatáshoz.

Ez nem FIFO. Ez csak azt biztosítja, hogy a raktár → kocsi művelet egy valódi adatbázisrekordon történjen.

---

# 8. Egyenleg nézet

Az Egyenleg nézet két fő tartozási oldalt kezel:

## Mi tartozunk – eladóknak

A felvásárlási tételek alapján jelenik meg.

A pénzbeli tartozás csak a nem fizetett tételekből számítódik.

Rekesztartozásnál a felvásárlásnál kapott és adott rekeszek különbsége számít.

## Nekünk tartoznak – vevők

Az eladási tételek alapján jelenik meg.

Pénztartozás:

```text
mennyiség × egységár
```

csak a nem fizetett tételeknél.

Rekesztartozás:

```text
elvitt mennyiség - visszahozott rekesz - kifizetett hiány
```

A nézet partnerenként és rekesztípusonként csoportosít, és részletező modalt is tartalmaz.

---

# 9. Eladás nézet

Az Eladás nézetben a következő információk fontosak:

- vevő;
- zöldség;
- rekesztípus;
- mennyiség;
- eladási egységár;
- fizetve;
- elvitte;
- visszahozott rekesz;
- hiányzó rekesz kifizetve;
- megjegyzés.

A csoportosított vevőnézetben megjelenik:

- pénztartozás Ft-ban;
- rekesztartozás darabszámban;
- rendezett állapot.

Az egyedi tételeknél ugyanez a tartozási információ szintén megjelenik.

A rekeszállapotok több helyen azonnal mentődnek, nem igényelnek külön „Mentés” gombot.

---

# 10. Felvásárlás nézet – jelenlegi működés

A `FelvasarlasView.vue` főbb funkciói:

- dátum szerinti lista;
- új felvásárlás;
- saját termés;
- eladó kiválasztása;
- zöldség kiválasztása;
- rekesztípus kiválasztása;
- mennyiség;
- egységár;
- adott üres rekesz;
- fizetve;
- megjegyzés;
- szerkesztés;
- törlés;
- zöldséghez kép feltöltése;
- törzsadat-kezelés;
- raktárkészlet megjelenítése;
- raktár → kocsi mozgatás.

A zöldséghez beállítható alapértelmezett rekesztípus. A zöldség kiválasztásakor ezt a rendszer felajánlja, de a felhasználó felülírhatja.

---

# 11. Backend fontos endpointok

## Felvásárlás

Jellemző endpointok:

```text
GET    /api/felvasarlas
POST   /api/felvasarlas
PUT    /api/felvasarlas/{id}
DELETE /api/felvasarlas/{id}
GET    /api/felvasarlas/raktar-csoportos
POST   /api/felvasarlas/raktarbol-kocsira
```

A pontos route-ok mindig az aktuális controllerből ellenőrizendők.

## Eladás

Jellemző endpointok:

```text
GET    /api/eladas
POST   /api/eladas
PUT    /api/eladas/{id}
DELETE /api/eladas/{id}
```

## Egyenleg

```text
GET   /api/egyenleg
PATCH /api/egyenleg/elado/{id}
PATCH /api/egyenleg/vevo/{id}
```

## Riportok

Fontos riportok:

```text
GET /api/riportok/mi-tartozunk
GET /api/riportok/nekunk-tartoznak
GET /api/riportok/rekeszveszteseg
GET /api/riportok/keszlet
GET /api/riportok/rekeszreszletezo
GET /api/riportok/konyveles
```

A `keszlet` endpoint Admin jogosultságot igényel.

---

# 12. Jelenlegi fontos fájlok

## Backend

```text
RekeszAppBackend/
├── Controllers/
│   ├── AuthController.cs
│   ├── FelvasarlasController.cs
│   ├── EladasController.cs
│   ├── EgyenlegController.cs
│   ├── RiportokController.cs
│   └── TorzsadatokController.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── AppDbContextFactory.cs
│   └── DbInitializer.cs
├── Domain/
│   └── Entities.cs
└── Migrations/
```

## Frontend

```text
RekeszAppFrontend/
├── src/
│   ├── api/
│   │   └── client.js
│   ├── components/
│   │   ├── KocsiKeszletModal.vue
│   │   ├── TorzsadatModal.vue
│   │   ├── ZoldsegModal.vue
│   │   └── Gyorskereso.vue
│   ├── stores/
│   │   └── auth.js
│   ├── router/
│   │   └── index.js
│   └── views/
│       ├── LoginView.vue
│       ├── FelvasarlasView.vue
│       ├── EladasView.vue
│       └── EgyenlegView.vue
```

---

# 13. Jelentősebb korábbi módosítások

Az aktuális `main` branch már tartalmazza az alábbi mérföldköveket.

### Egyenleg újratervezése

PR #1 sikeresen merge-ölve.

Merge commit:

`a3b43a7b5327d2018a6ebd078f098ecd7c2eaac7`

### Felvásárlás validációk

Commit:

`d16faba126c53d3640e7cc20d2b8698c9d4270e9`

### Eladás tartozás-megjelenítés

Commit:

`9456d3600cf2733b95a6acc40f2c8f91e24a40e5`

### Kocsi készlet backend előkészítés

`RiportokController.cs`

Commit:

`73b75d9d9f91dd2023646ec780e5b305a535d0aa`

### Felvásárlás backend módosítás

`FelvasarlasController.cs`

Commit:

`9ed73929ab9d87af512f169a326ec55c62a09a5d`

### Raktár UI csoportosítás és forrástétel-választás

`FelvasarlasView.vue`

Commit:

`0abb4e913f0e7c071afaee82f59e6efc84689177`

### Kocsi készlet modal

`KocsiKeszletModal.vue`

Commit:

`90d007216348446c1174d95b9a1b2cbe3274f485`

### Jelenlegi készletlogika egyszerűsítése

A vételár-alapú becslés eltávolítása és az üzleti modell véglegesítése.

Commit:

`0415e44f3e8938d4b485a039d8ae96e0737a1ea0`

---

# 14. Aktuális fejlesztési állapot

## Elkészült

- [x] Felvásárlás alapfunkciók
- [x] Saját termés
- [x] Vásárolt áru egységár-validáció
- [x] Adott rekesz alapérték 0
- [x] Adott rekesz nem követi automatikusan a mennyiséget
- [x] Eladás
- [x] Eladási tartozások megjelenítése
- [x] Rekesztartozások megjelenítése
- [x] Egyenleg nézet újratervezése
- [x] Partnerenkénti tartozás
- [x] Részletező modalok
- [x] Kocsi készlet
- [x] Raktár készlet
- [x] Zöldség + rekesztípus szerinti készletcsoportosítás
- [x] Raktár → kocsi mozgatás
- [x] Eredeti felvásárlási tétel ID használata mozgatáskor
- [x] Nincs FIFO
- [x] Nincs rejtett készletforrás-allokáció
- [x] Vételár nem része a készletmodellnek

## Még tesztelendő

- [ ] Új felvásárlás vásárolt áruval
- [ ] Új felvásárlás saját termékkel
- [ ] Felvásárlás szerkesztése
- [ ] Felvásárlás törlése
- [ ] Raktárkészlet több partnerből
- [ ] Raktárkészlet több dátumból
- [ ] Több forrástételből álló raktárcsoport mozgatása
- [ ] Raktár → kocsi mozgatás mennyiségi korlátozása
- [ ] Kocsi készlet ellenőrzése
- [ ] Eladás több zöldséggel/rekesztípussal
- [ ] Eladási pénztartozás
- [ ] Eladási rekesztartozás
- [ ] Egyenleg mindkét iránya
- [ ] Mobil nézet
- [ ] Asztali nézet
- [ ] Backend + frontend együtt

---

# 15. Következő tesztelési sorrend

A jelenlegi állapot után ezt a sorrendet érdemes követni:

### 1. Backend lokálisan

```powershell
cd RekeszAppBackend
dotnet restore
dotnet build
dotnet run
```

### 2. Frontend lokálisan

```powershell
cd RekeszAppFrontend
npm install
npm run dev
```

### 3. Funkcionális teszt

Javasolt sorrend:

```text
Felvásárlás
    ↓
Raktár
    ↓
Raktár → Kocsi
    ↓
Kocsi készlet
    ↓
Eladás
    ↓
Egyenleg
```

A tesztelés alatt még **ne kerüljön élesítésre az új backend**.

---

# 16. Tesztpélda

A készletcsoportosítás tesztelésére jó példa:

```text
Alma M10 – Zsolti – 20 db
Alma M10 – Józsi  – 15 db
Alma M30 – Zsolti – 30 db
```

Elvárt csoportosított készlet:

```text
Alma M10 – 35 db
Alma M30 – 30 db
```

Ha történik:

```text
Alma M10 eladás – 10 db
```

akkor:

```text
Alma M10 – 25 db
```

A rendszernek **nem kell megmondania**, hogy a 10 db melyik felvásárlási tételből származott.

---

# 17. Mit NE vezessünk be később automatikusan?

Külön felhasználói döntés nélkül nem szabad bevezetni:

- FIFO készletkezelést;
- LIFO készletkezelést;
- átlagáras készletértékelést;
- automatikus felvásárlási tétel-allokációt;
- eladás → konkrét felvásárlás kapcsolatot;
- felvásárlási tételek adatbázis-szintű összevonását.

Ha ezek közül bármelyik szükségessé válik, előbb az üzleti folyamatot kell tisztázni.

---

# 18. Ha később pontos önköltség/árrés kell

A jelenlegi rendszer ezt szándékosan nem kezeli készletszinten.

Ha később szükség lesz rá, akkor külön projektként kell megtervezni.

Lehetséges irányok:

### A) Felhasználó választja ki a forrást

Eladáskor megadható, hogy melyik felvásárlásból ment az áru.

### B) FIFO

A rendszer automatikusan a legrégebbi készletet fogyasztja.

### C) Átlagköltség

A rendszer időszakonként átlagos beszerzési költséget számol.

**Ezek közül jelenleg egyik sincs bevezetve.**

---

# 19. Adatbázisra vonatkozó fontos szabály

Az adatbázisban az eredeti tranzakciókat meg kell őrizni.

Például két felvásárlást nem szabad azért összevonni, mert a frontend egy készletkártyán egyetlen csoportként mutatja őket.

A frontend csoportosítása csak **megjelenítési és összesítési logika**.

Az eredeti tranzakciós adatok maradjanak visszakereshetők.

---

# 20. Biztonsági és élesítési megjegyzés

A fejlesztés során használt alapértelmezett admin bejelentkezést (`admin` / `admin`) éles környezetben meg kell változtatni, ha az aktuális konfiguráció még ezt használja.

Az adatbázis éles módosítása előtt mindig legyen mentés.

A jelenlegi fejlesztési stratégia:

1. frontend + backend együtt elkészül;
2. lokális teszt;
3. hibák javítása;
4. ismételt teszt;
5. csak ezután új backend publikálása;
6. éles frontend/backend együtt ellenőrzése.

---

# 21. Korábbi technikai korlátozások

A fejlesztői környezetből korábban nem volt elérhető a NuGet.org, ezért a backend teljes `dotnet build` ellenőrzése nem minden fejlesztési lépésnél volt lehetséges.

A frontend korábban `npm run build` segítségével ellenőrizve lett.

Ha a helyi gépen a backend fordítása hibát jelez, a teljes hibaüzenetet kell visszaadni, és abból folytatni a javítást.

---

# 22. Fejlesztési szabályok a folytatáshoz

Ha a munkát később folytatjuk, először ezt a README-t kell alapul venni.

Módosítás előtt mindig ellenőrizni kell:

1. az aktuális `main` állapotot;
2. az érintett backend controllert;
3. az érintett Vue komponenst;
4. az adatmodellt, ha adatkezelés változik;
5. az API szerződést;
6. hogy az új funkció nem vezet-e be véletlenül FIFO-t vagy más rejtett üzleti szabályt.

Különösen fontos:

> **A frontend csoportosítása nem jelent adatbázis-szintű összevonást.**

> **Az eladás nem kapcsolódik automatikusan konkrét felvásárlási tételhez.**

> **A felvásárlási ár jelenleg nem része a készletlogikának.**

> **Nincs FIFO.**

---

# 23. Aktuális „folytatási pont”

A következő beszélgetésben a projektet innen kell folytatni:

**„A jelenlegi `main` branch tartalmazza a lezárt fejlesztési szakaszt. Először a teljes lokális tesztet végezzük el, és csak a teszt közben talált hibákat javítjuk. Új készletértékelési vagy FIFO-logikát nem vezetünk be.”**

Ez a mondat szándékosan szerepel itt, hogy később egyértelmű legyen a kiindulási állapot.

---

# 24. Rövid állapotösszefoglaló

**Projekt:** Piac / RekeszApp

**Stack:** Vue 3 + Vite + ASP.NET Core .NET 8 + EF Core + MySQL

**Branch:** `main`

**Legutóbbi fontos commit:**

`0415e44f3e8938d4b485a039d8ae96e0737a1ea0`

**Állapot:** fejlesztési szakasz lezárva, teljes lokális teszt következik.

**Készletmodell:**

```text
zöldség + rekesztípus + darabszám
```

**Nincs:**

```text
FIFO
forrásallokáció
készlet-vételár
rejtett készletelosztás
```

**Következő feladat:**

```text
lokális frontend + backend indítás
        ↓
teljes funkcionális teszt
        ↓
hibák javítása
        ↓
új backend publikálása
        ↓
éles teszt
```
