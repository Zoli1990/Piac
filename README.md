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
9. Felvásárlási lista frontend oldali csoportosítása azonos zöldség + rekesztípus + felvásárlási ár alapján.

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

## 10.1. Felvásárlási lista csoportosítása

A felvásárlási lista **csak frontend megjelenítésben csoportosít**. Az adatbázisban az eredeti felvásárlási tételek továbbra is külön rekordok maradnak, saját ID-val és napi sorszámmal.

Egy csoport feltétele pontosan:

- azonos zöldség;
- azonos rekesztípus;
- azonos felvásárlási ár.

A következők **nem** részei a csoportosítási kulcsnak:

- partner / eladó;
- napi sorszám;
- dátum/időpont.

A dátum továbbra is az oldal lekérdezési szűrője: az adott nap rekordjai kerülnek csoportosításra.

Példa:

```text
#101 Alma M10 – 10 db – 500 Ft
#102 Alma M10 – 15 db – 500 Ft
#103 Alma M10 – 20 db – 500 Ft
#104 Alma M10 –  8 db – 600 Ft
```

Megjelenítés:

```text
Alma M10 – 45 db – 500 Ft
Alma M10 –  8 db – 600 Ft
```

A csoport mennyisége az eredeti tételek mennyiségének összege.

Az ár összehasonlítása numerikusan történik, ezért például `500` és `"500"` ugyanabba a csoportba kerül. A `null`/hiányzó ár külön, „ár nélkül” csoport.

### Csoport részletezése

Ha egy csempére több eredeti rekord került, kattintáskor először egy részletező lista jelenik meg. Ebben az eredeti napi sorszámok láthatók.

A felhasználó innen választhatja ki a konkrét tételt szerkesztésre vagy törlésre. Így a csoportosítás nem veszít el semmilyen egyedi adatot, például:

- partnert;
- fizetve állapotot;
- adott rekesz mennyiséget;
- megjegyzést;
- eredeti napi sorszámot.

A csoportosítás **nem módosítja és nem vonja össze az adatbázist**.

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

### Felvásárlási lista frontend csoportosítása

A `FelvasarlasView.vue` a napi felvásárlási rekordokat zöldség + rekesztípus + felvásárlási ár szerint csoportosítja. Az eredeti rekordok megmaradnak, és a csoport részletezőjéből az egyedi napi sorszámú tételek szerkeszthetők/törölhetők.

Commit:

`2b4a6b3f584252e1b9a0594394aadc09918642ae`

### Hiányzó `RaktarHelyszin` EF migráció pótlása

Az éles adatbázison (`db66470`) az `__efmigrationshistory` tábla szerint már régebben lefutott egy `20260902201337_RaktarHelyszin` nevű migráció (feltehetően közvetlenül az adatbázison, migrációs fájl nélkül), amely:

- felvette a `Helyszin` és `AthelyezveDb` oszlopokat a `FelvasarlasTetelek` táblára (raktár funkció);
- átköltöztette a `KepUrl` oszlopot `FelvasarlasTetelek`-ről `Zoldsegek`-re (kategória-szintű kép).

A migrációs fájl viszont sosem került be a repóba, és az `AppDbContextModelSnapshot.cs` sem lett frissítve — így a repo migrációtörténete nem egyezett sem a C# modellel (`Entities.cs`), sem az éles DB tényleges sémájával. Ez egy feltöltött phpMyAdmin dump (`db66470.sql`) alapján derült ki.

Pótoltuk a hiányzó migrációt (`20260902201337_RaktarHelyszin.cs` + `.Designer.cs`, ugyanazzal az ID-val, amit az éles DB már ismer, hogy EF ne akarja újra lefuttatni), és frissítettük a `ModelSnapshot`-ot, hogy az most már pontosan tükrözze a jelenlegi modellt és az éles DB tényleges szerkezetét. Ezt a fájlt Claude kézzel írta (sandbox-ban nincs NuGet-hozzáférés a `dotnet ef` eszközhöz), az EF Core generálási konvencióit követve — **lokálisan érdemes leellenőrizni** a `dotnet ef migrations has-pending-model-changes` paranccsal, hogy tényleg nincs eltérés a modell és a migrációtörténet között.

Ettől kezdve egy teljesen üres adatbázison lefuttatva a 3 migrációt (`InitialCreate` → `EladasHianyFelvasarlasKiegeszites` → `RaktarHelyszin`), a séma pontosan az éles DB jelenlegi állapotát adja vissza — ez lesz az alapja az új, tiszta adatbázisra való átállásnak.

Commit:

`642d289bd3b4917bbb07296e2f207191fab6d3a0`

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
- [x] Felvásárlási lista frontend csoportosítása zöldség + rekesztípus + felvásárlási ár szerint
- [x] Csoport részletező az eredeti napi sorszámú tételekhez
- [x] Csoportosítás adatbázis-összevonás nélkül

## Még tesztelendő

- [ ] Új felvásárlás vásárolt áruval
- [ ] Új felvásárlás saját termékkel
- [ ] Felvásárlás szerkesztése
- [ ] Felvásárlás törlése
- [ ] Azonos zöldség + rekesztípus + ár több rekordból egy csempébe kerül
- [ ] Eltérő felvásárlási ár külön csempét eredményez
- [ ] Eltérő rekesztípus külön csempét eredményez
- [ ] Több partner azonos csoportba kerülhet
- [ ] Csoport részletezőből a megfelelő napi sorszám kiválasztható
- [ ] Csoportból kiválasztott tétel szerkesztése
- [ ] Csoportból kiválasztott tétel törlése
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

A felvásárlási lista külön tesztpéldája:

```text
#101 Alma M10 – 10 db – 500 Ft
#102 Alma M10 – 15 db – 500 Ft
#103 Alma M10 – 20 db – 500 Ft
#104 Alma M10 –  8 db – 600 Ft
```

Elvárt frontend:

```text
Alma M10 – 45 db – 500 Ft
Alma M10 –  8 db – 600 Ft
```

A négy eredeti adatbázisrekord ettől még változatlanul különálló rekord marad.

---

# 17. Mit NE vezessünk be később automatikusan?

Külön felhasználói döntés nélkül nem szabad bevezetni:

- FIFO készletkezelést;
- LIFO készletkezelést;
- átlagáras készletértékelést;
- automatikus felvásárlási tétel-allokációt;
- eladás → konkrét felvásárlás kapcsolatot;
- felvásárlási tételek adatbázis-szintű összevonását.
