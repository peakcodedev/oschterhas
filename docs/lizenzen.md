# Lizenz-Richtlinie – Oschterhas

Ziel: Nur Abhängigkeiten mit klaren, freizügigen Open-Source-Lizenzen. Keine Pakete, die eine kommerzielle Lizenz, einen Lizenzschlüssel, Registrierung oder Telemetrie verlangen, auch nicht ab einer bestimmten Firmengrösse. Was fehlt, bauen wir selbst.

## Erlaubt

MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC, PostgreSQL License, 0BSD, Unlicense.

Alles andere (LGPL, MPL, GPL, AGPL, Dual-/Source-available-Lizenzen, „Community License") **nur nach Rücksprache**.

## Bewusst nicht verwendet

| Paket | Grund | Ersatz |
|---|---|---|
| MediatR | seit v13 kommerzielle Lizenz | direkte Service-Aufrufe |
| AutoMapper | seit v15 kommerzielle Lizenz | eigene Mapping-Methoden |
| Polly | Projektentscheid: keine Resilience-Bibliothek | eigener `Retry`-Helfer |
| FluentAssertions ≥ 8 | kommerzielle Lizenz | xUnit `Assert` |
| Moq | SponsorLink/Telemetrie-Vorfall | NSubstitute |
| MassTransit ≥ 9 | kommerzielle Lizenz | nicht nötig |
| Duende IdentityServer | kommerzielle Lizenz | Peakcode Identity |
| ImageSharp | Six Labors Split License | nicht nötig |
| QuestPDF | Community-Lizenz mit Umsatzgrenze | QR-Code im Frontend, Druck über Browser |
| Kommerzielle GPX-/GIS-Bibliotheken | Lizenzpflicht | eigener `GpxParser` mit `System.Xml` |

## Vorgesehene Abhängigkeiten

### Backend (.NET)
| Paket | Lizenz | Zweck |
|---|---|---|
| Microsoft.AspNetCore.* (OpenApi, Authentication.OpenIdConnect) | MIT | Web, OIDC, OpenAPI |
| Microsoft.EntityFrameworkCore (+ Design, Tools) | MIT | ORM, Migrationen |
| Npgsql.EntityFrameworkCore.PostgreSQL | PostgreSQL | PostgreSQL-Provider |
| xunit.v3 | Apache-2.0 | Unit-Tests (Microsoft Testing Platform) |
| Microsoft.Extensions.TimeProvider.Testing | MIT | `FakeTimeProvider` in Tests |
| Microsoft.Extensions.Options, Microsoft.Extensions.Caching.Memory, Microsoft.Extensions.Http | MIT | Optionen, Cache und HttpClientFactory in Core (ohne ASP.NET-Abhängigkeit) |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | MIT | Health-Check mit DB-Prüfung |
| NSubstitute (+ NSubstitute.Analyzers.CSharp) | BSD-3-Clause | Test-Doubles |

Keycloak-Admin-API: eigener schlanker `HttpClient`-Client in `Oschterhas.Core/Identity`, kein Keycloak-SDK-Paket.

### Frontend (npm)
| Paket | Lizenz | Zweck |
|---|---|---|
| @angular/* | MIT | Framework, Service Worker |
| @ngrx/store, effects, entity, router-store, store-devtools | MIT | State-Management |
| @jsverse/transloco | MIT | i18n |
| leaflet | BSD-2-Clause | Karte |
| qrcode (+ @types/qrcode, dev) | MIT | QR-Code für die Instanz-Adresse |
| @fontsource/poppins 5.3.0 | OFL-1.1 (Schrift), MIT (Paket) | Titelschrift des Designsystems, selbst gehostet |
| @fontsource/atkinson-hyperlegible-next 5.3.0 | OFL-1.1 (Schrift), MIT (Paket) | Textschrift des Designsystems, selbst gehostet |

### Betrieb
| Software | Lizenz | Zweck |
|---|---|---|
| Caddy (+ DNS-Modul des DNS-Anbieters, Lizenz beim Auswählen prüfen) | Apache-2.0 | Reverse Proxy, automatisches HTTPS, On-Demand TLS für Custom Domains |
| PostgreSQL | PostgreSQL | Datenbank |
| openapi-typescript 7.13.0 | MIT | TS-Typen aus OpenAPI. Nur per `npx` mit fester Version aufgerufen, nicht installiert (verlangt TypeScript 5, Angular 22 nutzt 6) |
| @types/leaflet (dev) | MIT | Typen für Leaflet |
| license-checker-rseidelsohn (dev) | BSD-3-Clause | Lizenzprüfung in CI |

**Ausnahme OFL-1.1 (nur Schriften)**: Die SIL Open Font License ist die übliche freie Lizenz für Schriften: Nutzung, Einbetten und Weitergabe sind frei, auch kommerziell; nur die Schrift selbst darf nicht allein verkauft werden. Das Designsystem (`design-system/`) schreibt Poppins und Atkinson Hyperlegible Next vor. Die Schriften werden selbst ausgeliefert (kein Google-Fonts-CDN, keine Daten an Dritte, CSP `font-src 'self'`). OFL-1.1 ist deshalb in `npm run licenses:check` erlaubt, aber nur für Schriften (Stand 2026-11-10).

**Ausnahme Python-2.0**: `argparse` (Python Software Foundation License, freizügig, OSI-anerkannt) kommt transitiv über `@jsverse/transloco` → `transloco-utils` → `cosmiconfig` → `js-yaml`. Es ist Build-Tooling und landet nicht im ausgelieferten Bundle. Deshalb ist Python-2.0 in `npm run licenses:check` erlaubt (Stand 2026-11-09, zu bestätigen).

## Prüfen

- Vor jedem neuen Paket: Lizenz auf nuget.org bzw. npmjs.com prüfen und diese Tabelle ergänzen.
- Frontend: `npm run licenses:check` (in CI).
- Backend: CI bricht ab, wenn ein verbotenes Paket in einer `.csproj`/`.props` auftaucht. Lizenzen neuer Pakete vor dem Eintrag in `Directory.Packages.props` manuell prüfen.
