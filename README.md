# SCHOOLBUFFET

SCHOOLBUFFET eshte nje aplikacion full-stack per menaxhimin e byfese se nje shkolle. Studentet shfletojne menyne,
hedhin produktet ne shporte dhe regjistrojne porosine me emer dhe klase. Administratori hyn ne nje panel te mbyllur
me fjalakalim, menaxhon produktet dhe kategorite, ndjek porosite ne kohe reale, ndryshon statusin e tyre dhe shikon
shitjet e dites, javes dhe muajit.

Porosite ruhen realisht ne PostgreSQL. Cdo porosie merr nje kod ndjekjeje ne formatin SB-YYMMDD-XXXX, totali
llogaritet automatikisht nga shporta, dhe ora e shkoll ndjek zonen Europe/Tirane. Te gjitha tekstet e nderfaqes jane
ne shqip dhe cmimet shprehen ne euro.

## Teknologjite

| Shtresa | Teknologjia |
| --- | --- |
| Frontend | HTML, CSS, JavaScript, pa librari te jashtme |
| Backend | C# ASP.NET Core 8 Web API |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Provider | Npgsql.EntityFrameworkCore.PostgreSQL 8 |
| Autentifikimi | Cookie e mbyllur (HttpOnly, SameSite=Strict) me rolin Admin |
| Fjalakalimi | PBKDF2-SHA256 me 210 000 iteracione dhe krip te vecante per perdorues |

## Paneli i studentit

- Menyja e plote e produkteve te renditura sipas kategorive Sandwicha, Pije, Ushqime dhe Embelsira.
- Fotoja, emri, perberja dhe cmimi ne euro per secilin produkt, me etiketa dhe shenje per produktet e rekomanduara.
- Kerkim me emer ose perberje dhe filtre sipas kategorise me nje prekje te vetme.
- Shporte me ndryshim sasie, fshirje artikulli, numerim te perkorte te artikujve dhe total automatik.
- Regjistrim porosie me emer, klase dhe shenim per byfese.
- Kodi i porosise dhe ndjekja e statusit pa logim.
- Historiku i porosive se studenti e ruan ne te njejten pajisje.

## Paneli i administratorit

- Hyrje e mbrojtur me fjalakalim dhe kufizim perpjekesh per adresse IP.
- Board me te ardhurat e dites, porosite e dites, totalin pergjithesh, porosite aktive, produktet aktive dhe
  produktet me te kerkuara.
- Grafiku i trendit te shitjeve per 7, 14 ose 30 dite; klikimi i nje shtylle hap porosite e asaj date.
- Lista e porosive me kerkim, filtre sipas statusit dhe dates, faqosje, detaje te plota dhe ndryshim statusi.
- Statuset e porosive: Ne pritje, Duke u pergatitur, Gati, Perfunduar.
- Kerkim, shtim, redaktim dhe fshirje e produkteve bashke me cilesine, radhen e renditjes, cmimin dhe foton.
- Ngarkim i fotove te reja per produktet direkt nga paneli.
- Menaxhim i kategorive me emer, pershkrim, ikon, ngjyre, radhe renditjeje dhe gjendje aktiv ose e fshehur.
- Raporti i shitjeve per date dhe per periudhe te perzgjedhur, me eksport CSV ne kodim UTF-8.
- Te dhenat e llogarise, gjendja e sistemit dhe ndryshimi i fjalakalimit.

## Struktura e projektit

```text
SchoolBuffet/
  Backend/
    SchoolBuffet.sln
    SchoolBuffet.Api/
      Controllers/                        8 kontrollues REST
      Data/                               DbContext dhe migrimet Entity Framework
      Dtos/                               Modelet e percjelljes se te dhenave
      Models/                             Entity-t Category, Product, Order, OrderItem, User
      Services/                           Auth, Catalog, Orders, Sales, Files, seeding, ora e shkoll
      Program.cs                          Hosting, autentifikim, autorizim, CSP, stenda statike
      appsettings.json                    Parametrat publik
      appsettings.Local.json              Te dhenat e lidhjes me databazen (perjashtuar nga Git)
  Frontend/
    index.html                            Menyja dhe shporta per studentet
    admin.html                            Paneli i administrimit
    css/                                  base.css, student.css, admin.css
    js/                                   api.js, format.js, charts.js, ui.js, student.js, admin-*.js
    assets/                               Ikona SVG, fotot e produkteve, folder per ngarkesa
  Database/
    schema.sql                            Skema referuese PostgreSQL
    create-role-and-database.sql          Urdher per rollin dhe bazen e te dhenave
    setup-postgres.ps1                    Skript konfigurimi me pyetje per fjalakalime
  README.md
  .gitignore
```

## Kerkesat paraprake

- .NET SDK 8.0 ose me i ri.
- PostgreSQL 14, 15 ose 16 i instaluar dhe i nisur.
- Komandat `dotnet` dhe `psql` te disponueshme ne PATH.
- Veglat e Entity Framework: `dotnet tool install --global dotnet-ef --version 8.0.11`.

## Konfigurimi i PostgreSQL

Krijo rollin e aplikacionit dhe bazen e te dhenave duke u lidhur si superperdorues. Skripti i pyet fjalakalimet ne
konsol dhe nuk i ruan ato ne asnje file:

```powershell
cd Database
.\setup-postgres.ps1 -PgBin 'C:\Program Files\PostgreSQL\16\bin'
```

Nese preferon urdhera manual, ekzekuto keto dy komanda si superperdorues:

```sql
CREATE ROLE schoolbuffet_app LOGIN PASSWORD 'fjalakalim-i-forte';
CREATE DATABASE "SchoolBuffet" OWNER schoolbuffet_app;
```

Pastaj krijo skedarin `Backend/SchoolBuffet.Api/appsettings.Local.json` me kete permbajtje:

```json
{
  "ConnectionStrings": {
    "SchoolBuffet": "Host=127.0.0.1;Port=5432;Database=SchoolBuffet;Username=schoolbuffet_app;Password=fjalakalim-i-forte"
  }
}
```

Skedari `appsettings.Local.json` permban te dhena private dhe perjashtohet nga Git me `.gitignore`. Mos e publiko
kurre.

## Nisja e Backend

Migrimet dhe mbjellja e te dhenave demo kryhen automatikisht ne nisen e API-se.

```bash
cd Backend/SchoolBuffet.Api
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://localhost:5086
export SCHOOLBUFFET_ADMIN_PASSWORD='fjalakalim-i-ndryshueshem'
dotnet run
```

Ne Windows me PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5086'
$env:SCHOOLBUFFET_ADMIN_PASSWORD = 'fjalakalim-i-ndryshueshem'
dotnet run
```

API-ja degjon ne `http://localhost:5086` dhe gjendjen e ka te aksesueshme ne `GET /api/health`.

Nese nuk vendos `SCHOOLBUFFET_ADMIN_PASSWORD`, aplikacioni gjeneron nje fjalakalim te rastit dhe e printon nje here
ne log gjate nises se pare. Ndryshoje pas hyrjes se pare nga faqja Cilesite.

## Nisja e Frontend

Frontend nuk ka proces ndertimi. API-ja e shtron folderin `Frontend` si stend statike, prandaj mjaftojne dy adresat:

- Menyja per studentet: `http://localhost:5086/index.html`
- Paneli i administrimit: `http://localhost:5086/admin.html`

Per zhvillim me rifreskim te menjehershem mund te perdorsh cdo server statik brenda folderit `Frontend`, por kerkesat
`fetch` dhe cookie kerkojne te njejtin origin, prandaj adresa e API-se duhet te perputhet me te.

## Perdorimi

1. Hap `http://localhost:5086/index.html`, zgjidh produktet dhe rrit sasin ne shporte.
2. Kliko `Perfundo porosine`, shkruaj emrin dhe klasen, dhe merr kodin e porosise.
3. Hap `http://localhost:5086/admin.html` dhe hyr me perdoruesin `admin` dhe fjalakalimin e mjedisit.
4. Ne Board shiko te ardhurat dhe trendin; ne Porosite ndrysho statusin nga Ne pritje ne Duke u pergatitur, Gati ose
   Perfunduar.
5. Ne Produktet dhe Kategorite menaxho menyne; ne Shitjet merr historikun ose eksporto CSV.
6. Studenti e ndjek porosine me kod nga `index.html` pa u loguar.

## Lista e endpoint-eve

| Metoda | Adresa | Qellimi |
| --- | --- | --- |
| GET | `/api/health` | Gjendja e API-se dhe e databazes |
| GET | `/api/menu` | Menyu e produkteve aktive, e ndare me kategori |
| POST | `/api/orders` | Regjistron porosine e re |
| GET | `/api/orders/track/{code}` | Njek porosine me kod |
| POST | `/api/auth/login` | Hyrja e administratorit |
| POST | `/api/auth/logout` | Dalja nga paneli |
| GET | `/api/auth/me` | Te dhenat e administratorit |
| POST | `/api/auth/password` | Ndryshimi i fjalakalimit |
| GET | `/api/admin/orders` | Lista e porosive me filtre dhe faqosje |
| GET | `/api/admin/orders/{id}` | Detajet e nje porosie |
| PATCH | `/api/admin/orders/{id}/status` | Ndryshimi i statusit |
| DELETE | `/api/admin/orders/{id}` | Fshirja e nje porosie |
| GET | `/api/admin/products` | Lista e produkteve |
| POST | `/api/admin/products` | Produkt i ri |
| PUT | `/api/admin/products/{id}` | Ndryshimi i produktit |
| DELETE | `/api/admin/products/{id}` | Fshirja e produktit |
| GET | `/api/admin/categories` | Lista e kategorive |
| POST | `/api/admin/categories` | Kategori e re |
| PUT | `/api/admin/categories/{id}` | Ndryshimi i kategorise |
| DELETE | `/api/admin/categories/{id}` | Fshirja e kategorise |
| POST | `/api/admin/uploads/image` | Ngarkimi i fotos se produktit |
| GET | `/api/admin/reports/summary` | Te dhenat e boardit |
| GET | `/api/admin/reports/history` | Historiku i shitjeve |
| GET | `/api/admin/reports/export.csv` | Eksporti CSV |

## Siguria

- Fjalakalimet ruhen si hash PBKDF2-SHA256 me 210 000 iteracione dhe me krip te vecante per perdorues.
- Seanca mban cookie `HttpOnly`, `SameSite=Strict`, `Path=/` me kohe gjatesie 8 oresh.
- Te gjitha endpoint-et `/api/admin/*` kerkojne rolin `Admin`; perndryshim kthehen 401 ose 403 ne format JSON.
- Hyrja kufizohet ne 8 perpjekje per minute per adresse IP.
- Validim ne dy ane: frontend mban formatin, backend rregullon gjatesite, numerat, kategorite dhe statuset.
- Header-at `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` dhe `Referrer-Policy` vendosen ne
  cdo pergjigje.
- Ngarkesat e fotove kontrollohen per tipin, madhesine dhe emrin, dhe ruhen ne folder te menaxhuar.
- Te dhenat e lidhjes me databazen dhe fjalakalimi i administratorit nuk futen ne repository.

## Testet e kryera

- Ndertimi i zgjidhjes pa gabime dhe pa paralajmerime.
- Lidhja me PostgreSQL, aplikimi i migrimeve dhe mbjellja e te dhenave demo.
- Hyrja e administratorit, mbrojtja 401 pa cookie, dalja dhe ndryshimi i fjalakalimit.
- Krijo, redakto dhe fshi produkt dhe kategori, me validime dhe mesazhe gabimi.
- Ngarkim, shfrytezim dhe fshirje e fotos se produktit.
- Regjistrim porosie nga frontend, llogaritje e totalit, ndjekje me kod te sakte dhe me kod te pasakte.
- Ndryshim statusi nga paneli dhe verifikim i ruajtjes ne databaze.
- Board, Porosite, Produktet, Kategorite, Shitjet dhe Cilesite ne pamje desktop, tablet dhe telefon.
