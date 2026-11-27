# OWASP-Checkliste – Oschterhas

Grundlage: **OWASP Top 10:2025** und **OWASP ASVS 5.0** (Ziel: Level 1 vollständig, ausgewählte Level-2-Punkte). Während der Entwicklung laufend beachten; vollständige Prüfung in Meilenstein M6. Ergebnis in `docs/owasp-bericht.md` (Datum, geprüfte Version, Funde, Massnahmen).

Status: `[ ]` offen · `[x]` erfüllt und geprüft · `[~]` teilweise / bewusst akzeptiert (Begründung im Bericht)

**Stand der Prüfung: 2026-11-10 (M6), Details und Funde in [owasp-bericht.md](owasp-bericht.md).**

## A01 – Broken Access Control
- [x] Alle Verwaltungs-Endpunkte verlangen Login (Fallback-Policy `RequireAuthenticatedUser`; Ausnahmen nur explizit mit `AllowAnonymous` im `PublicController`/`AuthController`)
- [x] Verwaltungs-Endpunkte nur auf dem Admin-Host, öffentliche nur auf Instanz-Hosts erreichbar
- [x] Organisator/in sieht und ändert nur **zugeordnete** Instanzen (`InstanceOrganizer`); Prüfung serverseitig in jedem Endpunkt, nicht nur in der UI
- [x] Instanzen anlegen/löschen, Organisator/innen erfassen, `/api/users` nur für Plattform-Admin
- [x] Öffentliche Endpunkte liefern Hasen nur bei Status `Active`/`Ended`, nie bei `Draft`
- [x] `hareId` muss zur Instanz des Hosts gehören (kein Markieren fremder Hasen per ID-Tausch über eine andere Subdomain)
- [x] Gesperrte Konten (`isActive = false`) und Konten ohne Eintrag in der Oschterhas-DB haben keinen Zugriff, auch mit gültiger Keycloak-Session
- [x] Kein Open Redirect über `returnUrl` beim Login

## A02 – Security Misconfiguration
- [x] Produktion: keine Developer Exception Page, keine Stacktraces, OpenAPI/Swagger nicht öffentlich
- [x] Security-Header: CSP (ohne `unsafe-inline` für Skripte; Kachel-Hosts von swisstopo/OSM erlaubt), HSTS, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy: geolocation=(self)`, `frame-ancestors 'none'`
- [x] CORS aus oder strikt auf die eigene Origin
- [x] **Host-Header-Prüfung**: nur Basis-Domain, gültige Subdomains und **verifizierte** Custom Domains; reservierte Slugs (`admin`, `www`, `api`, `custom` …) können nicht als Instanz angelegt werden
- [x] Links, Redirects und QR-Codes werden aus der gespeicherten Instanz-Adresse gebaut, **nie aus dem Host-Header** (keine Host-Header-Injection)
- [x] Custom Domains: nur `Verified` wird bedient; Verifizierung mit zufälligem Token (32 Byte); HTTP-Prüfung folgt keinen Redirects, hat ein kurzes Timeout und lehnt Ziele mit privaten/internen IP-Adressen ab (SSRF, auch gegen DNS-Rebinding: Verbindung zur geprüften Adresse). Umgesetzt in M4b, Prüfung in M6
- [x] `/internal/tls/allowed` ist von aussen nicht erreichbar und gibt nur für verifizierte Domains `200` (keine Zertifikate für fremde Domains, Let's-Encrypt-Limits geschützt)
- [x] Entfernte Custom Domains und gelöschte Instanzen werden sofort nicht mehr bedient (Cache invalidiert)
- [x] Admin-Bereich ist nur über `admin.oschterhas.li` erreichbar, nicht über Custom Domains
- [x] Session-Cookie **ohne `Domain`-Attribut**, damit Instanz-Subdomains es weder lesen noch setzen können
- [x] Keycloak-Service-Account `oschterhas-user-admin` nur mit `manage-users`, `view-users`, `query-users`
- [x] Standard-Passwörter der DB nur lokal; Produktion: App-Benutzer `oschterhas_app` nur mit SELECT/INSERT/UPDATE/DELETE, Migrationen mit dem Schema-Besitzer
- [x] Container läuft nicht als root (`app`, uid 1654), Dateisystem read-only, `cap_drop: ALL`, `no-new-privileges`

## A03 – Software Supply Chain Failures
- [x] Nur Pakete gemäss `docs/lizenzen.md`; zentrale Versionen in `Directory.Packages.props`, `package-lock.json` eingecheckt
- [x] `dotnet list package --vulnerable --include-transitive` ohne hohe/kritische Funde
- [x] `npm audit --omit=dev` ohne hohe/kritische Funde
- [x] Dependabot (oder Renovate) aktiv; GitHub Actions auf feste Versionen/SHAs gepinnt
- [x] Basis-Images mit fester Version (`sdk:10.0.403`, `aspnet:10.0.14`, `node:24.22-alpine`, `postgres:17`), Dependabot für Dockerfile und Compose

## A04 – Cryptographic Failures
- [~] Nur HTTPS in Produktion (HSTS), TLS beim Reverse Proxy korrekt konfiguriert → HSTS-Middleware aktiv ausserhalb Development; TLS/Caddy in M7
- [~] Cookies `Secure`, `HttpOnly`, `SameSite=Lax`; Data-Protection-Keys persistent und geschützt gespeichert → Persistenz konfigurierbar (`ReverseProxy:DataProtectionKeysDirectory`), Wert in M7
- [x] OIDC-Client-Secret nicht im Repo (User Secrets lokal, Umgebungsvariable/Secret-Store in Produktion)

## A05 – Injection
- [x] Nur EF Core LINQ bzw. parametrisierte Queries; kein `FromSqlRaw` mit String-Verkettung
- [x] **GPX/XML**: `DtdProcessing.Prohibit`, `XmlResolver = null`, Grössenlimit (5 MB) und Wegpunkt-Limit (1000) → Test für XXE und „Billion Laughs"
- [x] Ausgabe von Name/Hinweis/Spitzname in Angular nur per Interpolation (kein `innerHTML`, kein `bypassSecurityTrust*`); Leaflet-Popups mit Text-Knoten statt HTML-Strings
- [x] Längenbegrenzung aller Textfelder (Name, Hinweis, Spitzname) serverseitig

## A06 – Insecure Design
- [x] Missbrauch „Gefunden" ohne Login: Rate-Limit pro IP (30/min, umgesetzt in M2), Rücksetzen durch Organisator/in (M4), Verlauf in `found_event` (umgesetzt). Grosszügig wegen Mobilfunk-NAT; bei Missbrauch zusätzlich Limit pro Instanz prüfen
- [~] Live-Verbindungen: max. 50 pro IP, Heartbeat, Aufräumen bei Abbruch (umgesetzt in M2); Gesamtzahl offener Verbindungen pro Server im Betrieb beobachten → Überwachung im Betrieb (M7)
- [~] Produktion: `ForwardedHeaders` nur vom bekannten Reverse Proxy akzeptieren, sonst teilen sich alle Clients das Rate-Limit der Proxy-IP bzw. Clients können ihre IP fälschen → umgesetzt (`ReverseProxy:TrustedNetworks`), Wert in M7
- [x] Bedrohungsmodell kurz dokumentiert (wer kann was missbrauchen, z. B. alle Hasen als gefunden markieren, Positionen abgreifen vor Start, Subdomain einer fremden Instanz)
- [x] Hasen von Instanzen im Status `Draft` sind öffentlich nicht abrufbar

## A07 – Authentication Failures
- [x] Login ausschliesslich über Peakcode Identity (OIDC Authorization Code + PKCE); `state`/`nonce` geprüft (Standard der ASP.NET-Middleware nicht abschalten)
- [x] Issuer, Audience und Signatur der ID-Tokens werden validiert
- [~] Session-Timeout und Sliding Expiration festgelegt; Logout beendet lokale Session und ruft Identity-Logout auf → Abmelden im Browser noch von Hand prüfen
- [x] Session-Cookie wird nach Login neu ausgestellt (keine Session Fixation)

## A08 – Software or Data Integrity Failures
- [x] Antiforgery-Token für alle ändernden Requests mit Cookie-Auth
- [x] Keine unsichere Deserialisierung (nur `System.Text.Json` mit festen DTO-Typen)
- [x] Service Worker cached nur eigene Assets; Updates über Angular `SwUpdate`
- [~] Build-Artefakte in CI erzeugt, nicht lokal → Workflow vorhanden, auf GitHub noch nicht gelaufen

## A09 – Security Logging and Alerting Failures
- [x] Logins, Logouts, fehlgeschlagene Autorisierungen, GPX-Importe, Einladungen, Instanz-Änderungen, Rücksetzungen und Rate-Limit-Treffer werden geloggt
- [x] Keine Tokens, Cookies oder Secrets in Logs; Spitznamen nicht unnötig loggen
- [~] Strukturierte Logs (eingebautes `ILogger`, JSON-Konsole), Aufbewahrung festgelegt → Aufbewahrung in M7 festlegen

## A10 – Mishandling of Exceptional Conditions
- [x] Globale Fehlerbehandlung liefert Problem Details ohne interne Details
- [x] Fehler beim GPX-Parsen führen zu verständlicher Meldung, nicht zu `500`
- [x] Abgebrochene SSE-Verbindungen werden sauber aufgeräumt (kein Speicherleck)
- [x] Datenbank nicht erreichbar → definierter Fehler, eigener `Retry`-Helfer nur beim Start

## Datenschutz (Schweiz, revDSG)
- [x] Suchende: keine Konten, keine IP-Speicherung, Spitzname freiwillig
- [x] Datenschutzhinweis in der App (`/datenschutz`; Geolocation bleibt im Browser, wird nicht übertragen); Betreiber/Kontakt fehlt noch
- [x] Kartenkacheln werden von swisstopo/OSM geladen: im Hinweis erwähnen

## Werkzeuge für die Prüfung (M6)
- OWASP ZAP: Baseline-Scan und API-Scan mit `/openapi/v1.json` gegen lokale oder Staging-Instanz
- `dotnet list package --vulnerable --include-transitive`, `npm audit`
- Manuelle Tests der Zugriffskontrolle (zweites Organisator-Konto, fremde IDs)
- Header-Check (z. B. `curl -I` bzw. securityheaders.com auf Staging)
