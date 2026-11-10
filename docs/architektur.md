# Architektur – Oschterhas

## Überblick

```
duernten.oschterhas.li ─┐
ostern.ceviduernten.ch ─┤ (Suchende, ohne Login)    admin.oschterhas.li  (Plattform-Admin, Organisator/innen)
                        │                                    │
                        ▼                                    ▼
              Caddy (Reverse Proxy, TLS: Wildcard-Zertifikat + On-Demand-Zertifikate für Custom Domains)
                              │
                     Angular PWA (eine App, NgRx)
                              │  REST + SSE, gleiche Origin (/api)
                              ▼
                   ASP.NET Core 10 (Controllers) ──► PostgreSQL (EF Core + Migrationen)
                              │
                              ├─ OIDC (BFF) ─────────► Peakcode Identity (Keycloak, Realm "oschterhas")
                              └─ Admin-REST-API ─────► Keycloak: Organisator/innen anlegen + einladen
```

- **Eine Instanz = eine Suche**, erreichbar unter `<slug>.oschterhas.li` (z. B. `duernten.oschterhas.li`) und optional zusätzlich unter einer **Custom Domain** (z. B. `ostern.ceviduernten.ch`). Im nächsten Jahr wird neu importiert.
- Die API erkennt die Instanz am **Host-Header**. Für Suchende gibt es kein Login.
- Verwaltung nur unter `admin.oschterhas.li`. Dort läuft das Login über Peakcode Identity. Die API ist ein **Backend-for-Frontend (BFF)**: Sie wickelt OIDC ab und gibt dem Browser nur ein httpOnly-Cookie (gilt nur für `admin.oschterhas.li`). Tokens erreichen den Browser nie.

## Backend: Projekte

```
apps/api/
├─ Oschterhas.slnx
├─ global.json                    .NET-10-SDK fixieren
├─ Directory.Build.props          net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors
├─ Directory.Packages.props       zentrale Paketversionen
├─ src/
│  ├─ Oschterhas.Api/             Web-Schicht
│  │  ├─ Controllers/             AuthController, InstancesController, OrganizersController, CustomDomainController,
│  │  │                           HaresController, GpxController, PublicController, UsersController,
│  │  │                           DomainVerificationController (.well-known), InternalTlsController (Caddy ask)
│  │  ├─ Auth/                    OIDC/Cookie-Setup, Policies (PlatformAdmin, InstanceOrganizer)
│  │  ├─ Hosting/                 Middleware: Instanz aus Host-Header, Security-Header, Fehler → Problem Details
│  │  └─ Program.cs               DI, Pipeline
│  ├─ Oschterhas.Core/            Geschäftslogik (ohne ASP.NET-Abhängigkeit)
│  │  ├─ Instances/  Hares/  Users/   Services, DTOs (records), Mapping (ToDto()-Extensions), SlugRules
│  │  ├─ Gpx/                     GpxParser (nur System.Xml)
│  │  ├─ Identity/                IIdentityAdminClient + Keycloak-Implementierung (HttpClient)
│  │  ├─ Domains/                 Normalisierung, Verifizierung, BackgroundService für Nachprüfung
│  │  └─ Events/                  IInstanceEventBroadcaster + In-Memory-Implementierung (SSE)
│  ├─ Oschterhas.Data/            Datenzugriff
│  │  ├─ Entities/                AppUser, Instance, InstanceOrganizer, Hare, FoundEvent
│  │  ├─ AppDbContext.cs
│  │  ├─ Configurations/          IEntityTypeConfiguration<T> pro Entität
│  │  ├─ Repositories/            Interfaces + EF-Implementierungen
│  │  └─ Migrations/              EF-Core-Migrationen (eingecheckt)
│  └─ Oschterhas.Common/          kleine, fachfreie Bausteine
│     ├─ Result.cs                Result/Result<T> mit Fehlercodes
│     ├─ Retry.cs                 Retry mit exponentiellem Backoff (statt Polly)
│     └─ NamingConventions.cs     snake_case für DB-Namen
└─ tests/
   ├─ Oschterhas.Common.Tests/
   ├─ Oschterhas.Core.Tests/      GPX-Parser, Services (Repositories per NSubstitute)
   └─ Oschterhas.Api.Tests/       Controller, Middleware (Services per NSubstitute)
```

**Abhängigkeiten** (nur in diese Richtung):
`Api → Core → Data → Common` (Api und Core dürfen auch direkt `Common` nutzen).

Ein weiteres Projekt nur, wenn ein klarer Grund besteht, und nach dem Schema `Oschterhas.<Name>`. `Oschterhas.Core` ist dazugekommen, damit die Geschäftslogik ohne Web- und DB-Abhängigkeit per Unit-Test prüfbar ist.

## Entscheidungen

| # | Entscheidung | Begründung |
|---|---|---|
| E1 | Web-App/PWA | Läuft auf allen Handys per Link/QR, kein App-Store |
| E2 | Angular + TypeScript, **NgRx** | Wunsch; vorhersehbarer State, Live-Events als Actions |
| E3 | **ASP.NET Core 10 mit Controllers** (`[ApiController]`) | Wunsch; DataAnnotations-Validierung und `ProblemDetails` eingebaut |
| E4 | **EF Core 10 + Npgsql + PostgreSQL**, jede Schemaänderung als **Migration** | Wunsch; versionierte Schemaänderungen |
| E5 | Projekte **Api / Core / Data / Common** | Wunsch; klare Schichten, Core ohne Web- und DB-Abhängigkeit testbar |
| E6 | **Keine Pakete mit problematischen Lizenzen** ([lizenzen.md](lizenzen.md)) | Kein MediatR, AutoMapper, Polly usw. Benötigtes selbst gebaut |
| E7 | **GPX-Parser selbst gebaut** mit `System.Xml.XmlReader`, nur `<wpt>` | Keine Lizenz nötig; nur Wegpunkte relevant |
| E8 | **Nur Unit-Tests** mit xUnit v3 (Microsoft Testing Platform) + NSubstitute, keine Integrationstests | Wunsch; schnell, ohne Docker im Testlauf |
| E19 | Zeit über eingebautes `TimeProvider` statt eigenem `IClock` | In .NET enthalten; `FakeTimeProvider` für Tests |
| E9 | **Instanzen per Subdomain** `<slug>.oschterhas.li` | Wunsch; einfache, merkbare Adresse pro Suche |
| E10 | **Login über Peakcode Identity** (Keycloak, Realm `oschterhas`), BFF + Cookie, nur auf `admin.` | Gemeinsame Peakcode-Anmeldung; gleiches Muster wie Parokia; OWASP-Empfehlung für SPAs |
| E11 | Organisator/innen werden **eingeladen**: Oschterhas legt das Konto über die Keycloak-Admin-API an, Keycloak schickt die Einladung | Vorgabe von Peakcode Identity (keine Selbstregistrierung) |
| E12 | Rollen in der Oschterhas-DB, nicht in Keycloak; Zuordnung über **Issuer + Subject** | Vorgabe von Peakcode Identity |
| E13 | **Server-Sent Events** (`TypedResults.ServerSentEvents`, aus Controller als `IResult`) | Nur Server→Client-Updates; eingebaut |
| E14 | Leaflet + swisstopo WMTS, OSM-Fallback | Landeskarte ideal fürs Gelände |
| E15 | Transloco für i18n (`de`, `gsw`, `en`) | Sprachwechsel zur Laufzeit |
| E16 | OpenAPI eingebaut (`Microsoft.AspNetCore.OpenApi`), TS-Typen per `openapi-typescript` | Ein Vertrag zwischen .NET und Angular |
| E17 | **Custom Domain pro Instanz** (optional, eine), Verifizierung per HTTP-Token | Wunsch; Vereine können ihre eigene Adresse nutzen. HTTP-Prüfung braucht kein DNS-Paket |
| E18 | **Caddy** als Reverse Proxy mit Wildcard- und On-Demand-Zertifikaten | Automatisches HTTPS für beliebig viele Custom Domains, Apache-2.0 |

## Selbst gebaute Bausteine statt Pakete

| Statt | Eigene Lösung | Ort |
|---|---|---|
| MediatR | Controller rufen Core-Services direkt auf (DI). Querschnitt über Filter/Middleware. | Api, Core |
| AutoMapper | Explizite `ToDto()`-Extension-Methods pro Fachbereich | Core |
| Polly | `Retry.ExecuteAsync(...)` mit Backoff + `CancellationToken`; für `HttpClient` ein `RetryHandler : DelegatingHandler` (nur idempotente Requests) | Common |
| FluentValidation | DataAnnotations + `IValidatableObject` | Api, Core |
| FluentAssertions | xUnit `Assert` | Tests |

## Datenbank & Migrationen

- Entitäten, `AppDbContext`, Konfigurationen und Migrationen liegen in `Oschterhas.Data`. Die Api ist das Startprojekt.
- `snake_case` für Tabellen und Spalten (eigene Konvention im `OnModelCreating`, kein Zusatzpaket). Enums als Text.
- **Jede Modelländerung = neue Migration**:
  `dotnet ef migrations add <Name> --project src/Oschterhas.Data --startup-project src/Oschterhas.Api --output-dir Migrations`
- Eingecheckte Migrationen nie ändern oder löschen. Nie `EnsureCreated()`.
- CI prüft mit `dotnet ef migrations has-pending-model-changes`, dass keine Migration vergessen wurde.
- Entwicklung: Migrationen beim Start anwenden. Produktion: idempotentes SQL-Skript bzw. Migration-Bundle als eigener Deploy-Schritt.
- Gleichzeitiges Markieren als gefunden: PostgreSQL `xmin` als Concurrency-Token.
- Repositories sind bewusst dünn (Abfragen, keine Logik). Die Logik liegt in Core-Services und wird dort getestet. Da es keine Integrationstests gibt, sind Repository-Abfragen einfach zu halten und beim Review besonders zu prüfen.

## Instanzen, Subdomains & Custom Domains

### Subdomains
- `Instance.Slug` = Subdomain. Erlaubt: `^[a-z0-9]([a-z0-9-]{1,38}[a-z0-9])$` (3–40 Zeichen).
- **Reservierte Slugs**: `admin`, `www`, `api`, `identity`, `auth`, `mail`, `static`, `cdn`, `test`, `staging`, `dev`, `custom`.
- Konfiguration `Hosting:BaseDomain` (`oschterhas.li`, lokal `oschterhas.localhost`) und `Hosting:AdminSubdomain` (`admin`).
- Apex `oschterhas.li`: einfache Info-Seite (Inhalt offen).

### Custom Domains
Eine Instanz kann **eine** zusätzliche eigene Domain haben, z. B. `ostern.ceviduernten.ch`. Beide Adressen funktionieren gleichzeitig. QR-Code und angezeigter Link verwenden die Custom Domain, sobald sie verifiziert ist.

Ablauf:
1. Plattform-Admin trägt die Domain bei der Instanz ein → Status `Pending`, ein zufälliges Verifizierungs-Token wird erzeugt.
2. Die Domain-Besitzer/in setzt im eigenen DNS:
   - empfohlen: `CNAME ostern.ceviduernten.ch → custom.oschterhas.li`
   - für Apex-Domains ohne CNAME-Möglichkeit: `A`/`AAAA` auf die IP-Adressen des Servers
3. „Prüfen" (oder automatisch alle 10 Min. während `Pending`): Die API ruft `http://ostern.ceviduernten.ch/.well-known/oschterhas-verification` auf. Kommt die Anfrage bei uns an, antwortet derselbe Server mit dem Token. Stimmt es, ist die Domain `Verified`. So ist bewiesen, dass die Domain auf uns zeigt, und es braucht keinen DNS-Client und kein Zusatzpaket (nur `HttpClient`, ohne Redirects zu folgen).
4. Status, letzte Prüfung und Fehlversuche sieht der Admin auf der Detailseite; Organisator/innen sehen nur den Status.
5. Caddy holt beim ersten HTTPS-Aufruf automatisch ein Let's-Encrypt-Zertifikat (**On-Demand TLS**). Vorher fragt Caddy die API (`GET /internal/tls/allowed?domain=…`, nur aus dem internen Netz erreichbar), ob die Domain verifiziert ist. So gibt es nur Zertifikate für eingetragene, verifizierte Domains.
6. Der `BackgroundService` `CustomDomainMonitor` prüft jede Minute, was fällig ist: `Pending` alle 10 Minuten, `Verified` und `Failed` täglich. Nach 3 Fehlversuchen in Folge → `Failed`, die Domain wird nicht mehr bedient. Ein erfolgreicher Check macht sie wieder `Verified`.

Sicherheit der Prüfung (`HttpDomainVerifier`):
- Eigene DNS-Auflösung im `ConnectCallback`; verbunden wird nur mit öffentlichen Adressen (`AddressPolicy`: keine Loopback-, privaten, Link-Local-, CGNAT- oder Multicast-Adressen). Weil genau die geprüfte Adresse verbunden wird, hilft auch DNS-Rebinding nicht.
- Keine Redirects, kein Proxy, keine Cookies, 10 s Timeout, höchstens 1 KB Antwort, Token-Vergleich in konstanter Zeit.
- Lokal (`appsettings.Development.json`): `AllowLocalhostDomains` und `AllowPrivateAddresses` erlauben `*.localhost` über den Dev-Server auf Port 4300.

Caddy-Vorlage (Produktion, DNS-Modul je nach Anbieter):

```caddyfile
{
	on_demand_tls {
		ask http://api:5080/internal/tls/allowed
	}
}

# Plattform-Domain: Wildcard-Zertifikat per DNS-01
oschterhas.li, *.oschterhas.li {
	tls {
		dns <anbieter> {env.DNS_API_TOKEN}
	}
	@internal path /internal/*
	respond @internal 404
	reverse_proxy api:5080
}

# Custom Domains: Zertifikat erst, wenn die API "ja" sagt
https:// {
	tls {
		on_demand
	}
	@internal path /internal/*
	respond @internal 404
	reverse_proxy api:5080
}

# HTTP: nur die Verifizierung direkt beantworten, sonst auf HTTPS umleiten
http:// {
	handle /.well-known/oschterhas-verification {
		reverse_proxy api:5080
	}
	handle {
		redir https://{host}{uri} permanent
	}
}
```

Regeln:
- Domain wird normalisiert: klein geschrieben, ohne Punkt am Ende, IDN als Punycode (`System.Globalization.IdnMapping`). Weder `oschterhas.li` selbst noch Subdomains davon sind erlaubt. IP-Adressen und `localhost` werden abgelehnt.
- Eindeutig über alle Instanzen.
- Entfernen oder Löschen der Instanz → Domain wird sofort nicht mehr bedient, Caddy stellt keine neuen Zertifikate aus.

### Host-Auflösung (`HostResolver` in Core, `HostResolutionMiddleware` in Api)
1. Host = `admin.<BaseDomain>` → Verwaltung (keine Instanz).
2. Host = `<slug>.<BaseDomain>` → Instanz per Slug.
3. Host = `<BaseDomain>` → Info-Seite.
4. Sonst: Instanz mit diesem `CustomDomain` **und** Status `Verified` → Instanz.
5. Sonst → `404` (kein Hinweis darauf, ob eine Instanz existiert).

Die Zuordnung Host → Instanz wird 30 Sekunden mit `IMemoryCache` zwischengespeichert (auch negative Ergebnisse). Ab M4 wird der Cache beim Ändern einer Instanz zusätzlich invalidiert. Die Middleware läuft nur für `/api`-Pfade; Controller lesen das Ergebnis mit `HttpContext.GetHostResolution()`. Die eigene Host-Auflösung ersetzt das starre `AllowedHosts`.

### Lokale Entwicklung
- `http://tann.oschterhas.localhost:4300` (Instanz) und `http://admin.oschterhas.localhost:4300` (Verwaltung). Chrome und Firefox lösen `*.localhost` automatisch auf. Für Safari Einträge in `/etc/hosts` nötig.
- Custom Domain testen: z. B. `ostern.duernten-test.localhost` bei einer Instanz eintragen und „Jetzt prüfen" klicken. Die Prüfung läuft echt: Die API ruft `http://ostern.duernten-test.localhost:4300/.well-known/oschterhas-verification` über den Dev-Server auf.
- Angular-Dev-Server mit `allowedHosts: ['.localhost']`, Proxy `/api` → API **mit unverändertem Host-Header**.

### Produktion
- DNS bei `oschterhas.li`: Wildcard `*.oschterhas.li` und `custom.oschterhas.li` auf den Server.
- Caddy (Apache-2.0) als Reverse Proxy:
  - Wildcard-Zertifikat `*.oschterhas.li` per DNS-01 (benötigt das Caddy-DNS-Modul des DNS-Anbieters)
  - On-Demand TLS mit `ask`-Endpunkt für Custom Domains
  - leitet den Host-Header unverändert an die API weiter
- Let's-Encrypt-Limits beachten. Das `ask` verhindert Zertifikate für fremde Domains.

## GPX-Import

Relevant ist nur `<wpt>`. Referenz ist der echte swisstopo-App-Export `samples/sample_data_2.gpx` (65 Wegpunkte, IDs 101–165, keine Tracks). Namespace der Erweiterung: `https://prod-static.swisstopo-app.ch/xmlschemas/SwisstopoExtensions`.

```xml
<wpt lat="47.2705102840" lon="8.8527658385">
  <ele>514.0</ele>
  <name>8632 Tann</name>
  <desc>Hauptstrasse 64 </desc>
  <extensions>
    <swisstopo:waypoint_id>101</swisstopo:waypoint_id>
  </extensions>
</wpt>
```

| GPX | → Hare |
|---|---|
| `@lat`, `@lon` | `Lat`, `Lon` (Pflicht, `InvariantCulture`, Bereich prüfen) |
| `<ele>` | `Elevation` (optional) |
| `<name>` | `Name` (getrimmt). Leer **oder Standardname der App** (`Gpx:DefaultNames`, vorerst `["Wegpunkt"]`, ohne Gross-/Kleinschreibung) → `null`; das Frontend zeigt dann „Hase {Nummer}" in der UI-Sprache |
| `<desc>` (sonst `<cmt>`) | `Description` (getrimmt, leer → `null`). swisstopo füllt hier oft automatisch eine Adresse ein, z. B. ` (ZH) - Dürnten` |
| `<extensions>/*:waypoint_id` | `SourceWaypointId`, erkannt am lokalen Namen `waypoint_id` (unabhängig von Präfix und Namespace) |

- Die Nummer ergibt sich aus der Reihenfolge in der Datei (1, 2, 3 …), nicht aus der `waypoint_id`.
- Doppelte `waypoint_id` in einer Datei → Warnung, der zweite Wegpunkt erhält keine `SourceWaypointId`.

- `GpxParser` in `Oschterhas.Core/Gpx`, nur .NET-Basisbibliothek: `GpxParseResult Parse(Stream gpx, GpxParserOptions options)` → Wegpunkte + Warnungen.
- `<trk>`, `<rte>` und alles andere werden **übersprungen** (`XmlReader.Skip()`). Routen werden nicht gespeichert.
- **Limit**: mindestens 300 Wegpunkte müssen gehen; konfiguriert sind **1000** (`Gpx:MaxWaypoints`). Mehr → Abbruch mit klarer Meldung.
- **Sicherheit**: `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersInDocument` begrenzt, Upload max. 5 MB, Streaming statt DOM.
- **Import-Modi**: `replace` (alle Hasen der Instanz ersetzen; Warnung, wenn schon welche gefunden sind) und `merge` (Hasen mit gleicher `SourceWaypointId` aktualisieren, neue hinzufügen).
- Unit-Tests gegen `samples/` und Inline-XML: `sample_data_2.gpx` (65 Hasen, 63 davon als „Hase {Nummer}", IDs 101–165), `beispiel-hasen.gpx`, 300 und 1000 Wegpunkte (ok), 1001 (abgelehnt), doppelte `waypoint_id`, DTD/XXE, fehlender Name, ungültige/fehlende Koordinaten, Komma statt Punkt, `<trk>` wird ignoriert, GPX 1.0, Namespace-Varianten bei `waypoint_id`.

## Authentifizierung & Benutzer (Peakcode Identity)

- Keycloak aus `/Users/ufo/CODE/identity_peakcode` (lokal `http://localhost:8180`, Mailpit `http://localhost:8025`).
- Neuer Realm **`oschterhas`** (gleiches Muster wie `parokia`; anzulegen im Identity-Repo):
  - `oschterhas-bff`: vertraulicher Client, Authorization Code + PKCE (S256), Redirect-URI `${OSCHTERHAS_ADMIN_URL}/api/auth/callback`, Post-Logout `${OSCHTERHAS_ADMIN_URL}/*`.
  - `oschterhas-user-admin`: Service-Account nur mit `manage-users`, `view-users`, `query-users`.
  - Testbenutzer mit festen IDs: ein Plattform-Admin, zwei Organisator/innen, ein Konto ohne Zugang.
- Oschterhas-Konfiguration: `Oidc:Authority` = `http://localhost:8180/realms/oschterhas`, `Oidc:ClientId`, `Oidc:ClientSecret`, `IdentityAdmin:BaseUrl`, `IdentityAdmin:ClientId`, `IdentityAdmin:ClientSecret`. Secrets lokal per `dotnet user-secrets`.
- ASP.NET: `Microsoft.AspNetCore.Authentication.OpenIdConnect` + Cookie. Callback `/api/auth/callback`, Authorization Request per PAR (Standard in .NET 10). Beim Login wird `AppUser` über **Issuer + Subject** gesucht. Ohne Eintrag in der Oschterhas-DB → Redirect `/?login=no_access`.
- Standard-Challenge ist das Cookie (API antwortet `401`); nur `/api/auth/login` startet OIDC. Das Cookie enthält nur die User-ID und das ID-Token (für den Keycloak-Logout); Access- und Refresh-Token werden verworfen.
- Bei **jedem** Request lädt `OnValidatePrincipal` den Benutzer aus der DB: gesperrte oder entfernte Konten verlieren sofort den Zugriff, Rollenänderungen gelten sofort.
- Berechtigungen prüfen die Core-Services (`InstanceAccess`), nicht Controller-Attribute: so sind sie mit Unit-Tests abgedeckt.
- **Organisator/in erfassen** (durch Plattform-Admin): E-Mail + Name eingeben → Core ruft `IIdentityAdminClient` → Keycloak-Benutzer anlegen (oder vorhandenen per E-Mail finden) → `execute-actions-email` (`UPDATE_PASSWORD`) → `AppUser` mit Issuer + Subject anlegen → Zuordnung `InstanceOrganizer`.
- **Plattform-Admin**: Flag `IsPlatformAdmin` am `AppUser`. Erster Admin per Entwicklungs-Seed (fester Testbenutzer) bzw. in Produktion per Konfiguration `Auth:InitialPlatformAdmins` (Liste von Subjects).
- Cookie: `HttpOnly`, `Secure`, `SameSite=Lax`, **ohne `Domain`-Attribut** (gilt nur für `admin.`, nicht für Instanz-Subdomains). Antiforgery-Token als Header `X-XSRF-TOKEN`, geprüft vom eigenen Filter `ValidateCsrfToken` (MVCs `[AutoValidateAntiforgeryToken]` braucht die Views-Dienste).
- OIDC mit `ResponseMode = query` und `SameSite=Lax` für die Correlation-/Nonce-Cookies. Der .NET-Standard `form_post` führt zu einem Cross-Site-POST, bei dem der Browser diese Cookies nicht mitschickt.
- Einladungslink: Keycloak akzeptiert als `redirect_uri` nur registrierte Adressen; Oschterhas nutzt `{Admin-URL}/login` (im Realm registriert, Angular leitet auf `/instanzen` weiter).
- Autorisierung: Fallback-Policy „angemeldet" für alles ausser `[AllowAnonymous]` (öffentliche Endpunkte, Login, Health). Plattform-Admin und Zuordnung zur Instanz prüft Core bei jeder Operation.

## Frontend: Aufbau & NgRx

- **Eine** Angular-App. Beim Start entscheidet der Host:
  - `admin.<basis>` → Verwaltung (`/instanzen`, `/instanzen/:id`, `/benutzer`)
  - `<slug>.<basis>` oder jeder andere Host (Custom Domain) → Suchansicht; ob es die Instanz gibt, entscheidet die API (`404` → Seite „Suche nicht gefunden")
  - Basis-Domain → Info-Seite
- Die Basis-Domain steht in der Angular-Umgebungskonfiguration (`environment.baseDomain`).
- Store:
  - Root: `router` (`@ngrx/router-store`), `auth`, `ui` (Sprache, Kartenebene).
  - `search` (öffentliche Suche): Instanz, Hasen als `EntityState<Hare>`, SSE-Verbindungsstatus.
  - `admin`: Instanzen (`EntityState`), Organisator/innen, Hasen der gewählten Instanz, GPX-Vorschau.
- `createFeature`, `createActionGroup`, funktionale Effects. HTTP und SSE (`EventSource`) nur in Effects; Komponenten lesen per `store.selectSignal()`. Reconnect mit Backoff, danach vollständiges Nachladen.
- Store-Devtools nur im Dev-Build. Reducer, Selektoren und Effects mit Unit-Tests.

## Karten

- **swisstopo Landeskarte farbig**: `https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.pixelkarte-farbe/default/current/3857/{z}/{x}/{y}.jpeg`
- **swisstopo Luftbild**: `https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.swissimage/default/current/3857/{z}/{x}/{y}.jpeg`
- **OpenStreetMap**: `https://tile.openstreetmap.org/{z}/{x}/{y}.png` (Nutzungsrichtlinien beachten)
- Attribution „© swisstopo" bzw. „© OpenStreetMap-Mitwirkende"; Ebenen-Umschalter, Standard swisstopo.

## Live-Updates

- `GET /api/public/events` auf der Instanz-Subdomain liefert SSE.
- `IInstanceEventBroadcaster` (Core) mit In-Memory-Kanälen pro Instanz. Bei mehreren API-Instanzen später PostgreSQL `LISTEN/NOTIFY`.
- Events: `ready` (sofort, damit Header gesendet werden), `hare.found`, `hare.reset`, `hares.changed` (nach Import/Bearbeitung → neu laden), `instance.status`, `ping` (alle 25 s).
- Pro Verbindung ein begrenzter Kanal (100 Ereignisse, älteste fallen weg). Verpasste Ereignisse sind unkritisch, weil der Client nach jedem Wiederverbinden alles neu lädt.
- Client: `EventSource` in einem NgRx-Effect. Der Browser verbindet sich bei kurzen Unterbrüchen selbst neu; bei endgültig geschlossener Verbindung Neuversuch mit Backoff (1 s, 2 s, 4 s … max. 30 s). Ein Wächter erkennt „halboffene" Verbindungen (z. B. Netzwechsel am Handy, oder der Dev-Proxy nach einem API-Neustart) an 60 s Funkstille.

## Sicherheit

Laufend gemäss [owasp-checkliste.md](owasp-checkliste.md); vollständige Prüfung in Meilenstein M6.

- Rate-Limiting mit eingebautem `Microsoft.AspNetCore.RateLimiting` (`POST …/found`, GPX-Upload, Einladungen).
- Host-Header-Prüfung (nur Basis-Domain und deren Subdomains).
- Security-Header per eigener Middleware (CSP, HSTS, `nosniff`, Referrer-Policy, `Permissions-Policy: geolocation=(self)`).
- Problem Details (RFC 9457); keine Stacktraces in Produktion.

## Deployment (vorläufig)

- Ein Docker-Image: API liefert die gebaute Angular-App aus (`wwwroot`), plus PostgreSQL.
- Caddy davor: Wildcard-Zertifikat `*.oschterhas.li`, On-Demand-Zertifikate für Custom Domains (siehe oben).
- Migrationen als eigener Schritt vor dem Start der neuen Version.
- Hosting in der Schweiz (Vorgabe Peakcode Identity: Daten und Backups in der Schweiz). Anbieter noch offen.
