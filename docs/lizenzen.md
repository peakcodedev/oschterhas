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
| xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | Apache-2.0 / MIT | Unit-Tests |
| NSubstitute (+ NSubstitute.Analyzers.CSharp) | BSD-3-Clause | Test-Doubles |

Keycloak-Admin-API: eigener schlanker `HttpClient`-Client in `Oschterhas.Core/Identity`, kein Keycloak-SDK-Paket.

### Frontend (npm)
| Paket | Lizenz | Zweck |
|---|---|---|
| @angular/* | MIT | Framework, Service Worker |
| @ngrx/store, effects, entity, router-store, store-devtools | MIT | State-Management |
| @jsverse/transloco | MIT | i18n |
| leaflet | BSD-2-Clause | Karte |
| qrcode | MIT | QR-Code für die Instanz-Adresse |

### Betrieb
| Software | Lizenz | Zweck |
|---|---|---|
| Caddy (+ DNS-Modul des DNS-Anbieters, Lizenz beim Auswählen prüfen) | Apache-2.0 | Reverse Proxy, automatisches HTTPS, On-Demand TLS für Custom Domains |
| PostgreSQL | PostgreSQL | Datenbank |
| openapi-typescript | MIT | TS-Typen aus OpenAPI |

## Prüfen

- Vor jedem neuen Paket: Lizenz auf nuget.org bzw. npmjs.com prüfen und diese Tabelle ergänzen.
- Frontend: `npx license-checker-rseidelsohn --onlyAllow "MIT;Apache-2.0;BSD-2-Clause;BSD-3-Clause;ISC;0BSD"` (in CI).
- Backend: Lizenzen der direkten Pakete in `Directory.Packages.props` gegen diese Liste prüfen (CI-Skript in M0).
