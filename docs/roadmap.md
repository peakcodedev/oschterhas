# Roadmap – Oschterhas

Ein Punkt ≈ ein Commit/PR. Erledigte Punkte abhaken `[x]`.

## M0 – Projekt-Grundgerüst
- [x] .NET 10 SDK (in `~/.dotnet`) und `dotnet-ef` 10 als lokales Tool (`apps/api/dotnet-tools.json`)
- [x] Node 24 via nvm
- [x] `apps/api`: `Oschterhas.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`
- [x] Projekte `Oschterhas.Api`, `Oschterhas.Core`, `Oschterhas.Data`, `Oschterhas.Common` mit Referenzen `Api → Core → Data → Common`
- [x] Testprojekte `Oschterhas.Common.Tests`, `Oschterhas.Core.Tests`, `Oschterhas.Api.Tests` (xUnit v3 auf Microsoft Testing Platform + NSubstitute)
- [x] Api: Controllers, OpenAPI (nur Development), Problem Details, `/health` mit DB-Prüfung, Hosting-Optionen mit Validierung
- [x] Data: Entitäten gemäss `docs/datenmodell.md` (inkl. Custom-Domain-Feldern), `AppDbContext`, Konfigurationen, snake_case, `xmin`, Migration `InitialCreate`
- [x] Common: `Result`/`Error`, `Retry`, `NamingConventions` + Tests (Zeit über `TimeProvider`)
- [x] Core: `SlugRules` + Tests
- [x] `apps/web` mit Angular 22 (standalone, SCSS, Routing, strict, zoneless, Vitest), `allowedHosts: ['.localhost']`, Proxy `/api`
- [x] NgRx (`store`, `effects`, `entity`, `router-store`, `store-devtools`), Host-Erkennung (`HOST_CONTEXT`), Transloco mit `de`/`gsw`/`en`
- [x] ESLint (angular-eslint) + Prettier (Web), `dotnet format` + `.editorconfig` (API)
- [x] `.claude/launch.json` für Dev-Server; Befehle in `CLAUDE.md`
- [x] GitHub Actions: build, test, lint, format, `has-pending-model-changes`, verbotene Pakete, Schwachstellen, Lizenzprüfung (noch nicht auf GitHub gelaufen – kein Remote)

## M1 – Instanzen per Subdomain & Karte
- [x] Host-Auflösung: `HostResolver` (Core, mit 30-s-Cache) + `HostResolutionMiddleware` (Api) für Subdomain, Admin, Basis-Domain, reservierte Slugs und verifizierte Custom Domains + Tests
- [x] Entwicklungs-Seed: `tann` (aktiv, 65 Hasen aus `samples/sample_data_2.gpx`, Custom Domain `ostern.cevi-test.localhost`), `vorschau` (Entwurf), `vorbei` (beendet)
- [x] `GET /api/public/instance` (PublicController, `PublicInstanceService`, Mapping, Problem Details mit `code`) + Tests
- [x] Hasenname optional (`null` → „Hase {Nummer}" übersetzt im Frontend), Migration `MakeHareNameOptional`
- [x] Angular: Host-Erkennung und Routing (seit M0)
- [x] OpenAPI → TS-Typen (`npm run api:types`, API muss laufen), `PublicApiService`
- [x] NgRx-Feature `search` (Actions, Reducer mit `@ngrx/entity`, Selektoren, Effects inkl. Geolocation) + Tests
- [x] Karten-Komponente (Leaflet, Landeskarte/Luftbild/OSM, Attribution), nummerierte Marker offen/gefunden, Popup ohne HTML-Strings
- [x] Eigene Position + „Meine Position", Fortschritt „x von y Hasen gefunden"
- [x] Hinweise für `Draft` („noch nicht begonnen"), `Ended` und unbekannte Adresse

## M2 – Gefunden markieren & Live-Updates
- [x] `POST /api/public/hares/{id}/found`: `FoundService` (Core) mit Verlauf (`found_event`), Concurrency über `xmin`, `409` bei bereits gefunden oder nicht aktiver Suche, `422` bei ungültigem Spitznamen
- [x] Rate-Limits (eingebaut): 30 Markierungen/Minute und 50 offene Live-Verbindungen pro IP (`RateLimits` in appsettings), `429` mit `Retry-After`
- [x] `IInstanceEventBroadcaster` (in-memory, ein Kanal pro Verbindung, älteste Ereignisse fallen bei Überlauf weg) + `GET /api/public/events` (SSE mit `ready` zu Beginn und `ping` alle 25 s)
- [x] NgRx-Effect für `EventSource`: Ereignisse → Actions, Nachladen nach jedem Wiederverbinden, Backoff 1 s → 30 s, Wächter bei 60 s Funkstille
- [x] „Gefunden!"-Knopf im Popup (nur bei aktiver Suche), Dialog mit optionalem Spitznamen, Fehlermeldungen in drei Sprachen
- [x] Marker werden gezielt aktualisiert; offene Popups bleiben bei Live-Updates offen

## M3 – GPX-Parser
- [x] `GpxParser` in Core mit `XmlReader`: nur `<wpt>`, Felder gemäss `docs/architektur.md`, Limit `Gpx:MaxWaypoints = 1000`, XXE-Schutz (in M1 vorgezogen für den Seed)
- [x] Standardname „Wegpunkt" → kein Name (`Gpx:DefaultNames`), `waypoint_id` unabhängig vom Namespace-Präfix
- [x] Unit-Tests: `sample_data_2.gpx` (65 Hasen), `beispiel-hasen.gpx`, 300/1000/1001 Wegpunkte, doppelte `waypoint_id`, DTD/XXE, fehlender Name, ungültige Koordinaten, `<trk>` ignoriert, GPX 1.0, Namespace-Varianten

## M4 – Peakcode Identity & Verwaltung
- [x] Im Repo `identity_peakcode` (Branch `feat/oschterhas-realm`): Realm `oschterhas` mit `oschterhas-bff`, `oschterhas-user-admin` und Testbenutzern (feste IDs)
- [x] OIDC + Cookie (BFF) auf dem Admin-Host, `/api/auth/*`, Zuordnung über Issuer + Subject, Antiforgery, `401` statt Redirect für API-Aufrufe
- [x] Berechtigungen in den Core-Services (Plattform-Admin, Organisator/in der Instanz; fremde Instanzen → `404`) + Tests
- [x] Angular: `auth`-Feature, Login-Seite in der Verwaltung, Abmelden per Formular-POST
- [x] Plattform-Admin: Instanzen anlegen/bearbeiten/löschen (mit Slug-Prüfung)
- [x] `IIdentityAdminClient` (Keycloak-Admin-API, Token-Cache, `RetryHandler`) + Tests mit Fake-`HttpMessageHandler`
- [x] Organisator/in erfassen, einladen, zuordnen, entfernen; Einladung erneut senden
- [x] NgRx-Feature `admin`; Instanz-Übersicht für Organisator/innen
- [x] GPX-Upload mit Kartenvorschau, Modi `Replace`/`Merge`, Warnungen
- [x] Hasen bearbeiten (Drag, umbenennen, löschen, hinzufügen), Gefunden zurücksetzen
- [x] Suche starten/beenden, Adresse + QR-Code (druckbar), Liste „nur nicht gefundene"
- [x] Konten-Übersicht (sperren, Plattform-Admin setzen)
- [x] Im Browser mit echtem Keycloak-Login durchgespielt: Login als Plattform-Admin, Suche anlegen, GPX-Import (Zusammenführen/Ersetzen), Hase umbenennen, Suche starten, Organisatorin einladen (Mail in Mailpit), Gefunden zurücksetzen (live auf der Suchseite), Benutzerseite
- [ ] Noch von Hand prüfen: Hase per Drag verschieben, QR-Code drucken, Login als Organisator/in (Otto) und Ablehnung ohne Zugang (Nina), Abmelden

## M4b – Custom Domains
- [x] Domain-Normalisierung und -Validierung (URL einfügen, IDN/Punycode, keine IPs, nicht `oschterhas.li`) in Core + Tests
- [x] Host-Auflösung kennt verifizierte Custom Domains (mit `IMemoryCache`) + Tests (in M1 erledigt)
- [x] `PUT/DELETE /instances/{id}/custom-domain`, `POST …/verify` (10/min), `/.well-known/oschterhas-verification`
- [x] Verifizierung per HTTP: ohne Redirects, 10 s Timeout, max. 1 KB, Token-Vergleich in konstanter Zeit, eigene DNS-Auflösung mit Sperre privater Adressen (SSRF, DNS-Rebinding) + Tests
- [x] `BackgroundService`: `Pending` alle 10 Min., `Verified`/`Failed` täglich prüfen, nach 3 Fehlern `Failed`, bei Erfolg wieder `Verified`
- [x] `/internal/tls/allowed` für Caddy (nur aus `InternalNetwork:AllowedNetworks`, sonst `404`)
- [x] Admin-UI: Domain eintragen, DNS-Anleitung, Status, „Jetzt prüfen", entfernen; QR-Code/Link mit `publicUrl` (seit M4)
- [x] Angular: unbekannter Host → Suchansicht, `404` → „Suche nicht gefunden" (in M1 erledigt)
- [x] Im Browser mit lokaler Custom Domain durchgespielt: Domain als URL eingegeben (normalisiert), „Jetzt prüfen" mit echter HTTP-Prüfung → aktiv, Suche unter der Domain erreichbar, QR-Code wechselt, Fehlermeldungen (vergeben, Plattform-Domain, IP), Entfernen wirkt sofort

## M5 – Mehrsprachigkeit & PWA
- [ ] Sprachwahl im Store, Browser-Sprache als Standard (Transloco ist seit M0 eingerichtet); Datumsformate (`DatePipe`) in der gewählten Sprache (Locale-Daten de-CH/en registrieren)
- [ ] Alle UI-Texte übersetzt (Mundart gegenlesen lassen); API-Fehlercodes als Keys
- [ ] Angular Service Worker, Manifest, App-Icons
- [ ] Mobile-UX-Feinschliff, Barrierefreiheit; dicht liegende Marker (65 Hasen in Tann überlappen bei Zoom 14): Clustering oder grösserer Start-Zoom prüfen

## M6 – OWASP-Prüfung
- [ ] Prüfung nach `docs/owasp-checkliste.md` (OWASP Top 10:2025, ASVS 5.0 Level 1, ausgewählte L2-Punkte)
- [ ] OWASP ZAP: Baseline- und API-Scan (OpenAPI) gegen lokale/Staging-Instanz, Admin- und Instanz-Host
- [ ] `dotnet list package --vulnerable --include-transitive` und `npm audit` ohne kritische Funde
- [ ] Funde beheben, Ergebnis in `docs/owasp-bericht.md`

## M7 – Betrieb
- [ ] Dockerfile (API liefert Web-Build aus), `docker-compose.prod.yml`
- [ ] Migrationen als eigener Deploy-Schritt
- [ ] Domain `oschterhas.li` registrieren (DNS-Anbieter mit API für DNS-01)
- [ ] Hosting in der Schweiz, Wildcard-DNS `*.oschterhas.li` + `custom.oschterhas.li`
- [ ] Caddy: Wildcard-Zertifikat per DNS-01, On-Demand TLS mit `ask` → `/internal/tls/allowed`, `/internal/*` von aussen sperren, `/.well-known/oschterhas-verification` per HTTP ohne Umleitung auf HTTPS (Vorlage in `docs/architektur.md`)
- [ ] `InternalNetwork:AllowedNetworks` auf das Docker-Netz von Caddy setzen; `CustomDomains:DnsTarget` und DNS-Eintrag `custom.oschterhas.li`
- [ ] Produktions-Realm bei Peakcode Identity (Redirect-URIs, SMTP)
- [ ] Datenbank-Backup
- [ ] Testlauf mit echten Freiwilligen und echter GPX-Datei
