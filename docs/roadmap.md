# Roadmap – Oschterhas

Ein Punkt ≈ ein Commit/PR. Erledigte Punkte abhaken `[x]`.

## M0 – Projekt-Grundgerüst
- [ ] .NET 10 SDK installieren, `dotnet-ef` auf Version 10 aktualisieren
- [ ] Node 24 via nvm installieren
- [ ] `apps/api`: `Oschterhas.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`
- [ ] Projekte `Oschterhas.Api`, `Oschterhas.Core`, `Oschterhas.Data`, `Oschterhas.Common` mit Referenzen `Api → Core → Data → Common`
- [ ] Testprojekte `Oschterhas.Common.Tests`, `Oschterhas.Core.Tests`, `Oschterhas.Api.Tests` (xUnit v3 + NSubstitute)
- [ ] Api: Controllers, OpenAPI, Problem Details, `/health`
- [ ] Data: Entitäten gemäss `docs/datenmodell.md`, `AppDbContext`, Konfigurationen, snake_case, Migration `InitialCreate`
- [ ] Common: `Result`, `Retry`, `IClock` + Tests
- [ ] `apps/web` mit Angular CLI (standalone, SCSS, Routing, strict, zoneless), `allowedHosts: ['.localhost']`, Proxy `/api` mit Host-Header
- [ ] NgRx einrichten (`store`, `effects`, `entity`, `router-store`, `store-devtools`)
- [ ] ESLint + Prettier (Web), `dotnet format` (API)
- [ ] `.claude/launch.json` für Dev-Server; Befehle in `CLAUDE.md` bestätigen
- [ ] GitHub Actions: build, test, lint, `dotnet ef migrations has-pending-model-changes`, Lizenzprüfung

## M1 – Instanzen per Subdomain & Karte
- [ ] Middleware `InstanceResolution` (Host → Instanz, Basis-Domain, reservierte Slugs) + Tests
- [ ] Entwicklungs-Seed: Instanz `tann` (Status `Active`) mit Hasen aus `samples/sample_data_2.gpx`
- [ ] `GET /api/public/instance` (PublicController, Core-Service, Mapping) + Tests
- [ ] Angular: Host-Erkennung (admin / Instanz / Basis-Domain) und Routing
- [ ] OpenAPI → TS-Typen, API-Service
- [ ] NgRx-Feature `search` (Actions, Reducer mit `@ngrx/entity`, Selektoren, Effects) + Tests
- [ ] Karten-Komponente (Leaflet, swisstopo/OSM, Attribution), Hasen-Marker offen/gefunden, Popup
- [ ] Eigene Position + „auf mich zentrieren", Fortschritt „x von y"
- [ ] Hinweise für Status `Draft` („noch nicht begonnen") und `Ended`

## M2 – Gefunden markieren & Live-Updates
- [ ] `POST /api/public/hares/{id}/found` mit Rate-Limit, Concurrency-Token, `409`
- [ ] `IInstanceEventBroadcaster` + `GET /api/public/events` (SSE)
- [ ] NgRx-Effect für `EventSource`, Reconnect mit Backoff
- [ ] „Gefunden!"-Dialog mit optionalem Spitznamen

## M3 – GPX-Parser
- [ ] `GpxParser` in Core mit `XmlReader`: nur `<wpt>`, Felder gemäss `docs/architektur.md`, Limit `Gpx:MaxWaypoints = 1000`, XXE-Schutz
- [ ] Standardname „Wegpunkt" → „Hase {Nummer}" (`Gpx:DefaultNames`), swisstopo-Namespace für `waypoint_id`
- [ ] Unit-Tests: `sample_data_2.gpx` (65 Hasen), `beispiel-hasen.gpx`, 300/1000/1001 Wegpunkte, doppelte `waypoint_id`, DTD/XXE, fehlender Name, ungültige Koordinaten, `<trk>` ignoriert, GPX 1.0, Namespace-Varianten

## M4 – Peakcode Identity & Verwaltung
- [ ] Im Repo `identity_peakcode`: Realm `oschterhas` mit `oschterhas-bff`, `oschterhas-user-admin` und Testbenutzern (feste IDs)
- [ ] OIDC + Cookie (BFF) auf dem Admin-Host, `/api/auth/*`, Zuordnung über Issuer + Subject, Antiforgery
- [ ] Policies `PlatformAdmin` und `InstanceOrganizer` + Tests
- [ ] Angular: `auth`-Feature, Guard, Login/Logout
- [ ] Plattform-Admin: Instanzen anlegen/bearbeiten/löschen (mit Slug-Prüfung)
- [ ] `IIdentityAdminClient` (Keycloak-Admin-API, `RetryHandler`) + Tests mit Fake-`HttpMessageHandler`
- [ ] Organisator/in erfassen, einladen, zuordnen, entfernen; Einladung erneut senden
- [ ] NgRx-Feature `admin`; Instanz-Übersicht für Organisator/innen
- [ ] GPX-Upload mit Kartenvorschau, Modi `replace`/`merge`, Warnungen
- [ ] Hasen bearbeiten (Drag, umbenennen, löschen, hinzufügen), Gefunden zurücksetzen
- [ ] Suche aktivieren/beenden, Adresse + QR-Code (druckbar), Liste „nicht gefunden"
- [ ] Konten-Übersicht (sperren, Plattform-Admin setzen)

## M4b – Custom Domains
- [ ] Domain-Normalisierung und -Validierung (IDN/Punycode, keine IPs, nicht `oschterhas.li`) in Core + Tests
- [ ] Felder an `Instance` + Migration `AddCustomDomain`
- [ ] `InstanceResolution` um verifizierte Custom Domains erweitern (mit `IMemoryCache`) + Tests
- [ ] `PUT/DELETE /instances/{id}/custom-domain`, `POST …/verify`, `/.well-known/oschterhas-verification`
- [ ] Verifizierung per HTTP (ohne Redirects, Timeout, keine privaten IPs) + Tests mit Fake-`HttpMessageHandler`
- [ ] `BackgroundService`: `Pending` alle 10 Min., `Verified` täglich prüfen, nach 3 Fehlern `Failed`
- [ ] `/internal/tls/allowed` für Caddy (nur internes Netz)
- [ ] Admin-UI: Domain eintragen, DNS-Anleitung, Status, „Jetzt prüfen", entfernen; QR-Code/Link mit `publicUrl`
- [ ] Angular: unbekannter Host → Suchansicht, `404` → „Suche nicht gefunden"

## M5 – Mehrsprachigkeit & PWA
- [ ] Transloco mit `de`, `gsw`, `en`; Sprachwahl im Store, Browser-Sprache als Standard
- [ ] Alle UI-Texte übersetzt (Mundart gegenlesen lassen); API-Fehlercodes als Keys
- [ ] Angular Service Worker, Manifest, App-Icons
- [ ] Mobile-UX-Feinschliff, Barrierefreiheit

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
- [ ] Caddy: Wildcard-Zertifikat per DNS-01, On-Demand TLS mit `ask` → `/internal/tls/allowed`, `/internal/*` von aussen sperren
- [ ] Produktions-Realm bei Peakcode Identity (Redirect-URIs, SMTP)
- [ ] Datenbank-Backup
- [ ] Testlauf mit echten Freiwilligen und echter GPX-Datei
