# CLAUDE.md – Oschterhas

Web-App (PWA) für die Oster-Suche im Gelände. Ein Plattform-Admin legt **Instanzen** an (eine Suche pro Instanz, erreichbar unter `<slug>.oschterhas.li` und optional unter einer eigenen Custom Domain wie `ostern.ceviduernten.ch`) und erfasst dafür Organisator/innen. Diese importieren die Verstecke als GPX-Wegpunkte. Suchende sehen die Hasen ohne Login auf der Karte und markieren sie als gefunden – live für alle.

## Wichtige Dokumente (vor grösseren Änderungen lesen)

- [docs/konzept.md](docs/konzept.md) – Rollen, Abläufe, User Stories, **offene Fragen**
- [docs/architektur.md](docs/architektur.md) – Projekte, Entscheidungen, Subdomains, GPX, Auth, NgRx
- [docs/datenmodell.md](docs/datenmodell.md) – Entitäten und Felder
- [docs/api.md](docs/api.md) – Endpunkte pro Host
- [docs/lizenzen.md](docs/lizenzen.md) – erlaubte und verbotene Pakete
- [docs/owasp-checkliste.md](docs/owasp-checkliste.md) – Sicherheitsanforderungen
- [docs/roadmap.md](docs/roadmap.md) – Meilensteine; **Fortschritt dort abhaken**
- [docs/betrieb.md](docs/betrieb.md) – Produktion: Image, Compose, gemeinsamer Caddy (peakcode_infra), zentrale Identity, Deploy
- [design-system/README.md](design-system/README.md) – Designsystem (Farben, Schriften, Logos, Regeln); `npm run tokens` erzeugt daraus `apps/web/src/styles/tokens.css`

## Tech-Stack (Kurzfassung)

- **Backend** `apps/api`: .NET 10, ASP.NET Core **Controllers**, EF Core 10 + Npgsql, PostgreSQL, SSE, OpenAPI
  - `Oschterhas.Api` – Controllers, Auth, Middleware, `Program.cs`
  - `Oschterhas.Core` – Geschäftslogik, DTOs, Mapping, GPX-Parser, Keycloak-Admin-Client
  - `Oschterhas.Data` – Entitäten, `AppDbContext`, Configurations, Repositories, Migrations
  - `Oschterhas.Common` – kleine wiederverwendbare Bausteine (`Result`/`Error`, `Retry`, `NamingConventions`); Zeit immer über das eingebaute `TimeProvider` (Tests: `FakeTimeProvider`)
  - Abhängigkeiten nur `Api → Core → Data → Common`. Neue Projekte nur mit klarem Grund, Schema `Oschterhas.<Name>`.
- **Login**: Peakcode Identity = Keycloak aus `/Users/ufo/CODE/identity_peakcode`, Realm `oschterhas`, BFF mit httpOnly-Cookie, nur auf `admin.`-Host
- **Frontend** `apps/web`: Angular, Standalone Components, Signals, TypeScript strict, **NgRx**, Leaflet, Transloco, Service Worker
- **Domain**: `oschterhas.li` (`.ch` ist besetzt). Produktion auf dem gemeinsamen Peakcode-Server: gemeinsamer Caddy aus `peakcode_infra` (On-Demand-Zertifikate für alle Hosts), zentrale Keycloak-Instanz aus `identity_peakcode/production`, Deploy per GitHub Actions auf `main` ([docs/betrieb.md](docs/betrieb.md))
- **Lokal**: Suche unter `http://tann.oschterhas.localhost:4300`, Verwaltung unter `http://admin.oschterhas.localhost:4300` (Port 4300 fest, weil Keycloak genau diese Redirect-URI kennt), Keycloak `http://localhost:8180`, Mailpit `http://localhost:8025`
- **Testkonten** (Realm `oschterhas`, Passwort `OSCHTERHAS_TEST_USER_PASSWORD` in `identity_peakcode/.env`): `pia.plattform@oschterhas.test` (Plattform-Admin), `otto.organisator@oschterhas.test` (Organisator `tann`), `olga.organisatorin@oschterhas.test` (Organisatorin `vorschau`), `nina.neu@unbekannt.test` (kein Zugang)
- **Secrets lokal**: `dotnet user-secrets` im Projekt `Oschterhas.Api` (`Oidc:ClientSecret`, `IdentityAdmin:ClientSecret`), gleiche Werte wie in `identity_peakcode/.env`
- **Versionen**: .NET 10 SDK (`apps/api/global.json`), Node 24 LTS (`.nvmrc`)

## Befehle

```bash
docker compose up -d db                                   # PostgreSQL auf Port 5434 (5432/5433 belegen andere Projekte)
docker compose -f /Users/ufo/CODE/identity_peakcode/docker-compose.yml up -d   # Keycloak + Mailpit (ab M4)

# Backend (in apps/api). .NET 10 liegt in ~/.dotnet, nicht im System-dotnet (8.0):
export DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH
dotnet tool restore                                       # dotnet-ef aus dotnet-tools.json
dotnet build
dotnet test                                               # xUnit v3 über Microsoft Testing Platform
dotnet run --project src/Oschterhas.Api                   # http://localhost:5080, wendet Migrationen an (Development)
dotnet ef migrations add <Name> --project src/Oschterhas.Data --startup-project src/Oschterhas.Api --output-dir Migrations
dotnet ef migrations has-pending-model-changes --project src/Oschterhas.Data --startup-project src/Oschterhas.Api
dotnet format --verify-no-changes

# Frontend (in apps/web)
nvm use && npm ci
npm start                                                 # http://tann.oschterhas.localhost:4300, Proxy /api → :5080
npm test
npm run lint
npm run format:check
npm run build
npm run licenses:check
npm run api:types                                         # TS-Typen aus OpenAPI 3.0 neu erzeugen (API muss laufen)
```

Dev-Server für Claude: `.claude/launch.json` (`api`, `web`). Entwicklungs-Seed (leere DB): `tann` (aktiv, Custom Domain `ostern.cevi-test.localhost`), `vorschau` (Entwurf), `vorbei` (beendet). DB zurücksetzen: `docker compose down -v && docker compose up -d db`. CI: `.github/workflows/ci.yml` führt dieselben Prüfungen aus (Pull Requests; auf `main` als Teil von `deploy.yml`). Produktions-Image: `docker build -t oschterhas-api:local .`

## Harte Regeln

- **Lizenzen**: keine Pakete mit kommerziellen oder unklaren Lizenzen. **Verboten u. a.: MediatR, AutoMapper, Polly, FluentAssertions, Moq, MassTransit, Duende.** Vor jedem neuen Paket `docs/lizenzen.md` prüfen und ergänzen. Fehlt etwas, selbst bauen (in `Oschterhas.Common`, mit Tests).
- **Migrationen**: jede Modelländerung → neue Migration mit sprechendem Namen, zusammen mit der Änderung committen. Eingecheckte Migrationen nie ändern oder löschen. Nie `EnsureCreated()`.
- **Tests**: nur **Unit-Tests** mit xUnit v3 und **NSubstitute**. Keine Integrationstests, keine Testcontainers, keine InMemory-DB. Abhängigkeiten über Interfaces abstrahieren, damit sie sich ersetzen lassen.
- **GPX**: Referenz ist der echte swisstopo-Export `samples/sample_data_2.gpx`. Nur `<wpt>` auswerten, Standardname „Wegpunkt" → `Name = null` (das Frontend zeigt „Hase {Nummer}" übersetzt), Routen/Tracks ignorieren und nicht speichern. Nur .NET-Basisbibliothek (`XmlReader`). Limit 1000 Wegpunkte (mindestens 300 müssen gehen). DTD/XXE verboten.
- **Instanzen**: die Instanz kommt bei öffentlichen Endpunkten **immer aus dem Host-Header** (Subdomain oder verifizierte Custom Domain), nie aus Pfad oder Body. Links und QR-Codes nie aus dem Host-Header bauen, sondern aus der gespeicherten Adresse. Organisator-Endpunkte prüfen immer die Zuordnung zur Instanz.
- **Identity**: Benutzer über **Issuer + Subject** zuordnen, nie nur über E-Mail. Rollen leben in der Oschterhas-DB, nicht in Keycloak. Keine Selbstregistrierung; Organisator/innen werden eingeladen.
- **Frontend-State**: fachliches State über NgRx. Komponenten machen keine HTTP-Aufrufe; HTTP und SSE laufen in Effects. Effects-Dateien exportieren **nur Effects** (die Routen geben das ganze Modul an `provideEffects`); Hilfsfunktionen in eigene Dateien.
- **Nach Frontend-Änderungen** neben `npm test` immer `npx tsc -p tsconfig.app.json --noEmit` oder `npm run build`: Vitest findet nicht jeden Typfehler, und der Dev-Server zeigt dann still die alte Version.
- **Sicherheit**: neue Endpunkte gegen `docs/owasp-checkliste.md` prüfen. Prüfstand und Funde: `docs/owasp-bericht.md`.
- **CSP**: keine Inline-Skripte und keine Inline-Event-Handler (`onclick=` usw.), kein `eval`. Neue externe Quellen (Kacheln, Schriften) in `SecurityHeaders.cs` **und** in den Dev-Server-Headern (`angular.json`) eintragen; der Dev-Server nutzt dieselbe CSP, damit Verstösse sofort auffallen.
- **Logging**: Sicherheits- und Audit-Einträge mit IDs, nie mit IP-Adressen, Spitznamen, E-Mails, Tokens oder Cookies.
- **Secrets**: nie ins Repo. Lokal `dotnet user-secrets`, in Produktion Umgebungsvariablen. `.env` im Identity-Repo nicht lesen.

## Konventionen

- **Sprache**: Code, Bezeichner, Commits (Conventional Commits) auf Englisch. Doku in `docs/` auf Deutsch. UI-Texte nur über Transloco-Keys, immer in `de`, `gsw` **und** `en` (`apps/web/public/i18n/`).
- **Begriffe im Code**: `Instance` (Instanz), `Hare` (Hase), `Organizer`, `PlatformAdmin`, `Slug`, `CustomDomain`.
- **C#**: Nullable an, Warnungen als Fehler, file-scoped namespaces, `record` für DTOs, primäre Konstruktoren. Controllers dünn: Eingabe prüfen → Core-Service → `ActionResult`/Problem Details. Mapping als `ToDto()`-Extension in Core. `CancellationToken` überall durchreichen.
- **EF Core**: Konfiguration per `IEntityTypeConfiguration<T>`, keine Attribute an Entitäten. Lesen mit `AsNoTracking()` und Projektion.
- **Angular**: Host-Erkennung über `HOST_CONTEXT` (`core/host`), Routen per `canMatch: [hostIs(...)]`. `inject()`, neue Control-Flow-Syntax, `store.selectSignal()`. NgRx mit `createFeature`, `createActionGroup`, funktionalen Effects, `@ngrx/entity`. Mobile-first, Touch-Ziele ≥ 48 px. Gestaltung nur über Tokens und Rollen-Variablen aus dem Designsystem (keine festen Farben).
- **Koordinaten**: intern WGS84 (`lat`, `lon`). Leaflet `[lat, lon]`.
- **Kleine Schritte**: ein Roadmap-Punkt pro Commit/PR, danach in `docs/roadmap.md` abhaken.
- **Datenschutz**: Suchende ohne Konto, keine IP-Speicherung, Spitzname freiwillig. Hosting und Backups in der Schweiz.

## Arbeitsweise mit Claude

1. Nächsten offenen Punkt aus `docs/roadmap.md` nehmen (oder `/naechster-schritt`).
2. Bei Unklarheiten zuerst `docs/konzept.md` → „Offene Fragen" prüfen und nachfragen statt raten.
3. Umsetzen, Unit-Tests schreiben, Build/Tests/Lint laufen lassen.
4. Bei UI-Änderungen im Browser prüfen (mobile Breite 375 px).
5. Roadmap und betroffene Doku aktualisieren.
6. Änderungen am Realm gehören ins Repo `identity_peakcode`, am gemeinsamen Caddy ins Repo `peakcode_infra` (jeweils deren `AGENTS.md` beachten), nicht hierher.
