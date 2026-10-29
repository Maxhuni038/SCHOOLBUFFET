# SCHOOLBUFFET

SCHOOLBUFFET eshte nje sistem full-stack per menaxhimin e byfese shkollore. Studentet shfleton menyne, hedhin
produktet ne shporte dhe regjistrojne porosine me emer dhe klase. Ekrani i kuzhines i merr porositet sapo mberrine,
i fillon e i shenon gati. Paneli i administrimit menaxhon menyne, porositet, stokun, pagesat dhe raportet. Te
gjitha faqet perdorin te njejtin baz te dhenash PostgreSQL dhe rifreskohen vetiu, pa rizarqim manual.

Cdo porosie merr nje kod ndjekjeje ne formatin SB-YYMMDD-XXXX, totali llogaritet nga shporta, cmimet shprehen ne
euro dhe ora e shkoll ndjek zonen Europe/Tirane. Te gjitha tekstet e nderfaqes jane ne shqip.

## Teknologjite

| Shtresa | Teknologjia |
| --- | --- |
| Frontend | HTML, CSS, JavaScript, pa librari te jashtme |
| Backend | C# ASP.NET Core 8 Web API |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Provider | Npgsql.EntityFrameworkCore.PostgreSQL 8 |
| Kohe reale | Server-Sent Events, me radhe ngjarjesh te kufizuara per lidhje nderprerese |
| Autentifikimi | Cookie e mbyllur (HttpOnly, SameSite=Strict) me role perdoruesish |
| Fjalakalimi | PBKDF2-SHA256 me 210 000 iteracione dhe krip te vecante per perdorues |

## Rolet dhe e drejtat

| Roli | Hyrja | E drejtat |
| --- | --- | --- |
| Admin | `admin.html`, `kitchen.html` | Menaxhim i plote: porosi, pagesa, stok, kategori, produkte, raporte, llogari |
| Staff | `admin.html`, `kitchen.html` | Porosi, pagesa, stok dhe raporte; nuk shton, nuk redakton dhe nuk fshin produkte apo kategori |
| Kitchen | `kitchen.html` | Boardi i kuzhines, fillimi i pergatitjes dhe shenimi gati; nuk e mbyll porosine |
| Klient | `index.html` | Porosi te reja pa logim dhe ndjekje e porosive me kod |

Rolet zbatohen ne backend me atribute autorizimi, jo vetem ne nderfaqe. Nese perdoruesi Kitchen hap panelin e
administratorit, aplikacioni e shpie automatikisht te ekrani i kuzhines.

## Paneli i studentit

- Menyja e plote e produkteve te renditura sipas kategorive Sandwicha, Pije, Ushqime dhe Embelsira.
- Fotoja, emri, perberja dhe cmimi ne euro per secilin produkt, me etiketa dhe shenje per produktet e rekomanduara.
- Kerkim me emer ose perberje dhe filtre sipas kategorise me nje prekje te vetme.
- Shporte me ndryshim sasie, fshirje artikulli, numerim te perkorte te artikujve dhe total automatik.
- Regjistrim porosie me emer, klase dhe shenim per kuzhinen.
- Kodi i porosise dhe ndjekja e statusit pa logim.
- Njoftim automatik kur kuzhina e fillon porosine dhe kur porosia behet Gati.
- Historiku i porosive merr gjendjen me te re vetiu, pa rizarqim te faqes.

## Ekrani i kuzhines

- Kolona Ne pritje, Duke u pergatitur dhe Gati per marrje, secila me numerin e porosive.
- Permbledhje te numrit total, pritjes me te gjate dhe porosive per kolone.
- Rreshti i porosise me kod, emer dhe klase, artikuj me sasi, shenim per kuzhinen, oren e regjistrimit dhe matjen
  e kohes per gjendjen e porosise.
- Ngjyre urgjente kur porosia kalon pragun e pritjes, dhe sinjal zanore per porosite e reja.
- Butona Fillo pergatitjen, Mbrapsht dhe Mbyll porosine; butoni i mbylljes shfaqet vetem per Admin dhe Staff.
- Koha e pergatitjes llogaritet nga momenti kur kuzhina e filloi porosine, jo nga momenti i regjistrimit.
- Koha e pritjes pas gatimit tregon sa kohe porosia pret dorzimin.
- Rifreskim automatik sapo ndryshon cdo porosie, dhe kalim ne rifreskim me interval nese lidhja nderpritet.

## Paneli i administratorit

- Hyrje e mbrojtur me fjalakalim dhe kufizim perpjekesh per adresse IP.
- Board me te ardhurat e dites, porosite e dites, totalin pergjithesh, porosite aktive, produktet aktive dhe
  produktet me te kerkuara.
- Grafiku i trendit te shitjeve per 7, 14 ose 30 dite; klikimi i nje shtylle hap porosite e asaj date.
- Lista e porosive me kerkim, filtre sipas statusit dhe dates, faqosje, detaje te plota, gjurma e kohave te kuzhines
  dhe ndryshim statusi.
- Statuset e porosive: Ne pritje, Duke u pergatitur, Gati, Perfunduar.
- Inventari me sasin ne dorze, pragun e ulet, shitjet e dites, vleren e stokut dhe librin e levizjeve.
- Pagesat per interval datesh me gjendjen, mjetin e pageses, konfirmim manual, kthim ne pritje dhe anulim.
- Kerkim, shtim, redaktim dhe fshirje e produkteve bashke me cilesine, radhen e renditjes, cmimin, gjurmimin e
  stokut dhe foton.
- Ngarkim i fotove te reja per produktet direkt nga paneli.
- Menaxhim i kategorive me emer, pershkrim, ikon, ngjyre, radhe renditjeje dhe gjendje aktiv ose e fshehur.
- Raporti i shitjeve per date dhe per periudhe te perzgjedhur, me eksport CSV ne kodim UTF-8.
- Shenje e gjendjes se lidhjes me serverin dhe rifreskim automatik i cdo pamjeje kur ndryshon ndonje e dhen.
- Te dhenat e llogarise, gjendja e sistemit, roli i perdoruesit dhe ndryshimi i fjalakalimit.

## Si qarkullon porosia

1. Studenti e dergon porosine nga `index.html`; totali llogaritet dhe porosia ruhet me gjendje Ne pritje.
2. API-ja ul stokun per produktet e gjurmuara, hap pagesen per porosine dhe regjistron levizjet e stokut.
3. Serveri dergon sinjalin kohe reale; ekrani i kuzhines dhe paneli e shikojne porosine menjehere.
4. Kuzhina klikon Fillo pergatitjen; koha e fillimit ruhet dhe studenti merr njoftim.
5. Kuzhina klikon Sheno gati; gjendja Gati shfaqet ne kuzhine, ne panel dhe tek studenti.
6. Punonjesi e dorizon porosine dhe e mbyll; statusi behet Perfunduar dhe pagesa shenohet e paguar automatikisht.
7. Kohat e fillimit, gatimit dhe mbylljes ruhen ne databaze dhe shfaqen ne detajet e porosise.

## Struktura e projektit

```text
SchoolBuffet/
  Backend/
    SchoolBuffet.sln
    SchoolBuffet.Api/
      Controllers/                        12 kontrollues REST, mes tyre kuzhina dhe kohe reale
      Data/                               DbContext dhe migrimet Entity Framework
      Dtos/                               Modelet e percjelljes se te dhenave
      Models/                             Category, Product, Order, OrderItem, Payment, StockMovement, User
      Services/                           Auth, Catalog, Order, Kitchen, Stock, Payment, Sales, Realtime, Files, seeding
      Program.cs                          Hosting, autentifikim, role, CSP, stenda statike, kontrolle hyrjesh
      appsettings.json                    Parametrat publik
      appsettings.Local.json              Te dhenat e lidhjes me databazen (perjashtuar nga Git)
  Frontend/
    index.html                            Menyja dhe shporta per studentet
    kitchen.html                          Ekrani i kuzhines
    admin.html                            Paneli i administrimit
    css/                                  base.css, student.css, kitchen.css, admin.css
    js/                                   api.js, format.js, ui.js, charts.js, realtime.js, student.js,
                                          kitchen.js, admin-core.js, admin-dashboard.js, admin-orders.js,
                                          admin-catalog.js, admin-inventory.js, admin-reports.js
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
export SCHOOLBUFFET_STAFF_PASSWORD='fjalakalim-per-punonjesin'
export SCHOOLBUFFET_KITCHEN_PASSWORD='fjalakalim-per-kuzhinen'
dotnet run
```

Ne Windows me PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5086'
$env:SCHOOLBUFFET_ADMIN_PASSWORD = 'fjalakalim-i-ndryshueshem'
$env:SCHOOLBUFFET_STAFF_PASSWORD = 'fjalakalim-per-punonjesin'
$env:SCHOOLBUFFET_KITCHEN_PASSWORD = 'fjalakalim-per-kuzhinen'
dotnet run
```

API-ja degjon ne `http://localhost:5086` dhe gjendjen e ka te aksesueshme ne `GET /api/health`.

Llogarite krijohen vetem nese nuk ekzistojne: perdoruesi `admin`, punonjesi `staff` dhe kuzhina `kuzhina`. Nese nuk
vendos ndonje nga ndryshoret e permendura me siper, aplikacioni gjeneron nje fjalakalim te rastit dhe e printon nje
here ne log gjate nises se pare. Ndryshoje pas hyrjes se pare nga faqja Cilesite.

## Nisja e Frontend

Frontend nuk ka proces ndertimi. API-ja e shtron folderin `Frontend` si stend statike, prandaj mjaftojne tri adresat:

- Menyja per studentet: `http://localhost:5086/index.html`
- Ekrani i kuzhines: `http://localhost:5086/kitchen.html`
- Paneli i administrimit: `http://localhost:5086/admin.html`

HTML, CSS dhe JavaScript sherbehen me `Cache-Control: no-cache`, keshtu qe ndryshimet e reja arrijne tek klienti
menjehere dhe versioni i ruajtur ne memorie nuk mbetet i vlefshem. Fotot e ngarkuara ruhen ne memorien e klientit
per 10 minuta.

Per zhvillim me rifreskim te menjehershem mund te perdorsh cdo server statik brenda folderit `Frontend`, por kerkesat
`fetch` dhe cookie kerkojne te njejtin origin, prandaj adresa e API-se duhet te perputhet me te.

## Perdorimi

1. Hap `http://localhost:5086/index.html`, zgjidh produktet dhe rrit sasin ne shporte.
2. Kliko `Perfundo porosine`, shkruaj emrin dhe klasen, dhe merr kodin e porosise.
3. Hap `http://localhost:5086/kitchen.html` dhe hyr me perdoruesin `kuzhina`. Porosia e re shfaqet pa rizarqim.
4. Kliko `Fillo pergatitjen` dhe pastaj `Sheno gati`. Studenti merr njoftim dhe paneli shek te njejten gjendje.
5. Hap `http://localhost:5086/admin.html`, hyr me `admin` ose `staff`, dhe mbyll porosine nga Porosite; pagesa
   shenohet e paguar automatikisht.
6. Ne Inventar shiko gjendjen e produkteve dhe librin e levizjeve; ne Pagesat konfirmo, kthe ose anulo pagesat.
7. Ne Produktet dhe Kategorite menaxho menyne; ne Shitjet merr historikun ose eksporto CSV.
8. Studenti e ndjek porosine me kod nga `index.html` pa u loguar.

## Lista e endpoint-eve

| Metoda | Adresa | Qellimi |
| --- | --- | --- |
| GET | `/api/health` | Gjendja e API-se dhe e databazes |
| GET | `/api/menu` | Menyu e produkteve aktive, e ndare me kategori |
| GET | `/api/realtime/stream` | Rrjedha Server-Sent Events per ndryshimet |
| POST | `/api/orders` | Regjistron porosine e re |
| GET | `/api/orders/track/{code}` | Njek porosine me kod |
| POST | `/api/auth/login` | Hyrja e perdoruesit te byfese |
| POST | `/api/auth/logout` | Dalja nga paneli |
| GET | `/api/auth/me` | Te dhenat e llogarise dhe roli |
| POST | `/api/auth/password` | Ndryshimi i fjalakalimit |
| GET | `/api/kitchen/board` | Porositet aktive per ekranin e kuzhines |
| POST | `/api/kitchen/{id}/start` | Fillon pergatitjen |
| POST | `/api/kitchen/{id}/ready` | Shenon porosine gati |
| POST | `/api/kitchen/{id}/back` | Kthen porosine ne pritje |
| POST | `/api/kitchen/{id}/done` | Dorizon dhe mbyll porosine (Admin, Staff) |
| GET | `/api/admin/orders` | Lista e porosive me filtre dhe faqosje |
| GET | `/api/admin/orders/{id}` | Detajet e nje porosie |
| PATCH | `/api/admin/orders/{id}/status` | Ndryshimi i statusit |
| DELETE | `/api/admin/orders/{id}` | Fshirja e nje porosie |
| GET | `/api/admin/inventory` | Gjendja e stokut me permbledhje |
| GET | `/api/admin/inventory/movements` | Libri i levizjeve te stokut |
| PATCH | `/api/admin/inventory/{productId}/adjust` | Shtim, zbritje ose korrektim stokut |
| GET | `/api/admin/payments` | Pagesat me filtre datesh dhe gjendjeje |
| PATCH | `/api/admin/payments/{id}/paid` | Konfirmon pagesen |
| PATCH | `/api/admin/payments/{id}/pending` | Kthen pagesen ne pritje |
| PATCH | `/api/admin/payments/{id}/void` | Anulon pagesen |
| PATCH | `/api/admin/payments/{id}/method` | Ndryshon mjetin e pageses |
| GET | `/api/admin/products` | Lista e produkteve |
| POST | `/api/admin/products` | Produkt i ri (Admin) |
| PUT | `/api/admin/products/{id}` | Ndryshimi i produktit (Admin) |
| DELETE | `/api/admin/products/{id}` | Fshirja e produktit (Admin) |
| GET | `/api/admin/categories` | Lista e kategorive |
| POST | `/api/admin/categories` | Kategori e re (Admin) |
| PUT | `/api/admin/categories/{id}` | Ndryshimi i kategorise (Admin) |
| DELETE | `/api/admin/categories/{id}` | Fshirja e kategorise (Admin) |
| POST | `/api/admin/uploads/image` | Ngarkimi i fotos se produktit |
| GET | `/api/admin/reports/summary` | Te dhenat e boardit |
| GET | `/api/admin/reports/history` | Historiku i shitjeve |
| GET | `/api/admin/reports/export.csv` | Eksporti CSV |

## Kohe reale

- Serveri mban nje rrjedhe SSE te `GET /api/realtime/stream` dhe dergon ngjarjen `ndryshim` me nje nga llojet
  `porosi`, `pagese`, `inventar`, `meny`.
- Ngjarja mban kodin e porosise, gjendjen e re dhe oren, por nuk permban te dhena private.
- Secila lidhje ka nje radhe ngjarjesh me kapacitet te kufizuar; ngjarjet e vjetra hiqen qe serveri te mos
  bllokohet nga nje klient i ngadalshem.
- Klientet pa EventSource, dhe lidhjet e nderprura pas kater perpjekesh, kalojne ne rifreskim me interval.
- Faqet e fshehta ne sfond nuk kerkojne te dhen pa nevoje; ato rifreskohen kur faqja behet perseri e dukshme.

## Siguria

- Fjalakalimet ruhen si hash PBKDF2-SHA256 me 210 000 iteracione, me krip te vecante per secilin perdorues.
- Seanca mban cookie `HttpOnly`, `SameSite=Strict`, `Path=/` me kohe gjatesie 8 oresh.
- `api/admin/*` kerkon Admin ose Staff; shkrimet e menyse kerkojne Admin; `api/kitchen/*` lejohet per
  Admin, Staff dhe Kitchen.
- Hyrja kufizohet ne 8 perpjekje per minute per adresse IP.
- Validim ne dy ane: frontend mban formatin, backend rregullon gjatesite, numerat, kategorite, statuset dhe stokun.
- Sasia e stokit ulet me urdher te kushtezuar ne nje hap; porosine qe nuk ka stok mjaftueshem refuzohet me 409,
  keshtu qe asnje pjese e porosise nuk shkruhet gabimisht.
- Header-at `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` dhe `Referrer-Policy` vendosen ne
  cdo pergjigje; asnje skript ose stil i jashtem nuk lejohet.
- Pergjigjet `api/*` sherbehen me `Cache-Control: no-store` qe te dhenat financiare te mos ruhen ne memorie.
- Ngarkesat e fotove kontrollohen per tipin, madhesine dhe emrin, dhe ruhen ne folder te menaxhuar.
- Te dhenat e lidhjes me databazen dhe fjalakalimet e perdoruesve nuk futen ne repository.

## Rregullat e kodit ne kete projekt

- Asnje koment ne asnje file dhe ne asnje rresht: as ne C#, as ne JavaScript, as ne CSS, as ne HTML, as ne SQL, as ne
  PowerShell.
- Shkronja e gjashte e alfabetit shqip, e me dy pika mbi te, nuk perdoret kurre ne kod, tekste, emra variablash,
  klasash, funksionesh, ne databaze dhe ne dokumentim.
- Te gjitha tekstet e nderfaqes, mesazhet e suksesshme dhe mesazhet e gabimit jane ne shqip.
- Pa librari JavaScript ose CSS te jashtme, pa proces ndertimi dhe pa paket npm.

## Testet e kryera

- Ndertimi i zgjidhjes pa gabime dhe pa paralajmerime.
- Lidhja me PostgreSQL, aplikimi i migrimeve dhe mbjellja e te dhenave demo.
- `Database/schema.sql` u ushtrua ne nje baz testues dhe perputhet sakte me bazen ne pun: emra kolonash, indekse dhe
  kufizime te njejta.
- Hyrja e administratorit, mbrojtja 401 pa cookie, dalja dhe ndryshimi i fjalakalimit.
- `api/kitchen/board`, `api/admin/inventory` dhe `api/admin/payments` kthejne 401 kur kerkohen pa sesion.
- Krijo, redakto dhe fshi produkt dhe kategori, me validime dhe mesazhe gabimi.
- Ngarkim, shfrytezim dhe fshirje e fotos se produktit.
- Porosi e re nga `index.html`: gjendja Ne pritje, ulja e stokut, pagesa e hapur dhe kodi i ndjekjes.
- Rruga e plote Ne pritje, Duke u pergatitur, Gati, Perfunduar u ndoq me nje rast real ne kuzhine, ne panel dhe
  tek studenti, pa rizarqim faqesh, me kohmatese qe llogariten nga fillimi i pergatitjes dhe nga shenimi gati.
- Njoftimet tek studenti per fillimin e pergatitjes dhe per gjendjen Gati u verifikuan ne faqe.
- Dorzimi nga ekrani i kuzhines: porosia hiqet nga boardi, gjendja behet Perfunduar dhe pagesa shenohet e paguar.
- Pagesa manuale: konfirmimi me mjet dhe shenim, rlogaritja e permbledhjeve dhe kthimi i gjendjes ne pritje.
- Inventari: zbritje dhe shtim manual, libri i levizjeve me datesen, arsyen, referencen dhe perdoruesin.
- Mbrojtja e stokut: porosia me sasi me te madhe se gjendja refuzohet me 409 dhe sasia e stokut mbetet e pandryshuar.
- Validimet e porosise: emer bosh, klase bosh, pa artikuj dhe sasi jashte kufijve kthejne 400 me gabime per cdo fushe.
- Ndryshimi i cmimit nga paneli e rifreskoi menyne e studentit pa rizarqim te faqes.
- Boardi, Porositet, Produktet, Kategorite, Inventari, Pagesat, Shitjet dhe Cilesite u verifikuan me te dhenat reale te
  databazes; asnje numer nuk vjen nga te dhenat e sajuara.
- Ekrani i kuzhines, paneli dhe menyja u kontrolluan ne pamje desktop, tablet dhe telefon.
