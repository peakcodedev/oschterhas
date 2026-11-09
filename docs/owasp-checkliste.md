# OWASP-Checkliste – Oschterhas

Grundlage: **OWASP Top 10:2025** und **OWASP ASVS 5.0** (Ziel: Level 1 vollständig, ausgewählte Level-2-Punkte). Während der Entwicklung laufend beachten; vollständige Prüfung in Meilenstein M6. Ergebnis in `docs/owasp-bericht.md` (Datum, geprüfte Version, Funde, Massnahmen).

Status: `[ ]` offen · `[x]` erfüllt und geprüft · `[~]` teilweise / bewusst akzeptiert (Begründung im Bericht)

## A01 – Broken Access Control
- [ ] Alle Verwaltungs-Endpunkte verlangen Login (Fallback-Policy `RequireAuthenticatedUser`; Ausnahmen nur explizit mit `AllowAnonymous` im `PublicController`/`AuthController`)
- [ ] Verwaltungs-Endpunkte nur auf dem Admin-Host, öffentliche nur auf Instanz-Hosts erreichbar
- [ ] Organisator/in sieht und ändert nur **zugeordnete** Instanzen (`InstanceOrganizer`); Prüfung serverseitig in jedem Endpunkt, nicht nur in der UI
- [ ] Instanzen anlegen/löschen, Organisator/innen erfassen, `/api/users` nur für Plattform-Admin
- [ ] Öffentliche Endpunkte liefern Hasen nur bei Status `Active`/`Ended`, nie bei `Draft`
- [ ] `hareId` muss zur Instanz des Hosts gehören (kein Markieren fremder Hasen per ID-Tausch über eine andere Subdomain)
- [ ] Gesperrte Konten (`isActive = false`) und Konten ohne Eintrag in der Oschterhas-DB haben keinen Zugriff, auch mit gültiger Keycloak-Session
- [ ] Kein Open Redirect über `returnUrl` beim Login

## A02 – Security Misconfiguration
- [ ] Produktion: keine Developer Exception Page, keine Stacktraces, OpenAPI/Swagger nicht öffentlich
- [ ] Security-Header: CSP (ohne `unsafe-inline` für Skripte; Kachel-Hosts von swisstopo/OSM erlaubt), HSTS, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy: geolocation=(self)`, `frame-ancestors 'none'`
- [ ] CORS aus oder strikt auf die eigene Origin
- [ ] **Host-Header-Prüfung**: nur Basis-Domain, gültige Subdomains und **verifizierte** Custom Domains; reservierte Slugs (`admin`, `www`, `api`, `custom` …) können nicht als Instanz angelegt werden
- [ ] Links, Redirects und QR-Codes werden aus der gespeicherten Instanz-Adresse gebaut, **nie aus dem Host-Header** (keine Host-Header-Injection)
- [ ] Custom Domains: nur `Verified` wird bedient; Verifizierung mit zufälligem Token; HTTP-Prüfung folgt keinen Redirects, hat ein kurzes Timeout und lehnt Ziele mit privaten/internen IP-Adressen ab (SSRF)
- [ ] `/internal/tls/allowed` ist von aussen nicht erreichbar und gibt nur für verifizierte Domains `200` (keine Zertifikate für fremde Domains, Let's-Encrypt-Limits geschützt)
- [ ] Entfernte Custom Domains und gelöschte Instanzen werden sofort nicht mehr bedient (Cache invalidiert)
- [ ] Admin-Bereich ist nur über `admin.oschterhas.li` erreichbar, nicht über Custom Domains
- [ ] Session-Cookie **ohne `Domain`-Attribut**, damit Instanz-Subdomains es weder lesen noch setzen können
- [ ] Keycloak-Service-Account `oschterhas-user-admin` nur mit `manage-users`, `view-users`, `query-users`
- [ ] Standard-Passwörter der DB nur lokal; Produktion mit eigenem DB-Benutzer mit minimalen Rechten
- [ ] Container läuft nicht als root

## A03 – Software Supply Chain Failures
- [ ] Nur Pakete gemäss `docs/lizenzen.md`; zentrale Versionen in `Directory.Packages.props`, `package-lock.json` eingecheckt
- [ ] `dotnet list package --vulnerable --include-transitive` ohne hohe/kritische Funde
- [ ] `npm audit --omit=dev` ohne hohe/kritische Funde
- [ ] Dependabot (oder Renovate) aktiv; GitHub Actions auf feste Versionen/SHAs gepinnt
- [ ] Basis-Images mit fester Version, regelmässig aktualisiert

## A04 – Cryptographic Failures
- [ ] Nur HTTPS in Produktion (HSTS), TLS beim Reverse Proxy korrekt konfiguriert
- [ ] Cookies `Secure`, `HttpOnly`, `SameSite=Lax`; Data-Protection-Keys persistent und geschützt gespeichert
- [ ] OIDC-Client-Secret nicht im Repo (User Secrets lokal, Umgebungsvariable/Secret-Store in Produktion)

## A05 – Injection
- [ ] Nur EF Core LINQ bzw. parametrisierte Queries; kein `FromSqlRaw` mit String-Verkettung
- [ ] **GPX/XML**: `DtdProcessing.Prohibit`, `XmlResolver = null`, Grössenlimit (5 MB) und Wegpunkt-Limit (1000) → Test für XXE und „Billion Laughs"
- [ ] Ausgabe von Name/Hinweis/Spitzname in Angular nur per Interpolation (kein `innerHTML`, kein `bypassSecurityTrust*`); Leaflet-Popups mit Text-Knoten statt HTML-Strings
- [ ] Längenbegrenzung aller Textfelder (Name, Hinweis, Spitzname) serverseitig

## A06 – Insecure Design
- [ ] Missbrauch „Gefunden" ohne Login: Rate-Limit pro IP (30/min, umgesetzt in M2), Rücksetzen durch Organisator/in (M4), Verlauf in `found_event` (umgesetzt). Grosszügig wegen Mobilfunk-NAT; bei Missbrauch zusätzlich Limit pro Instanz prüfen
- [ ] Live-Verbindungen: max. 50 pro IP, Heartbeat, Aufräumen bei Abbruch (umgesetzt in M2); Gesamtzahl offener Verbindungen pro Server im Betrieb beobachten
- [ ] Produktion: `ForwardedHeaders` nur vom bekannten Reverse Proxy akzeptieren, sonst teilen sich alle Clients das Rate-Limit der Proxy-IP bzw. Clients können ihre IP fälschen
- [ ] Bedrohungsmodell kurz dokumentiert (wer kann was missbrauchen, z. B. alle Hasen als gefunden markieren, Positionen abgreifen vor Start, Subdomain einer fremden Instanz)
- [ ] Hasen von Instanzen im Status `Draft` sind öffentlich nicht abrufbar

## A07 – Authentication Failures
- [ ] Login ausschliesslich über Peakcode Identity (OIDC Authorization Code + PKCE); `state`/`nonce` geprüft (Standard der ASP.NET-Middleware nicht abschalten)
- [ ] Issuer, Audience und Signatur der ID-Tokens werden validiert
- [ ] Session-Timeout und Sliding Expiration festgelegt; Logout beendet lokale Session und ruft Identity-Logout auf
- [ ] Session-Cookie wird nach Login neu ausgestellt (keine Session Fixation)

## A08 – Software or Data Integrity Failures
- [ ] Antiforgery-Token für alle ändernden Requests mit Cookie-Auth
- [ ] Keine unsichere Deserialisierung (nur `System.Text.Json` mit festen DTO-Typen)
- [ ] Service Worker cached nur eigene Assets; Updates über Angular `SwUpdate`
- [ ] Build-Artefakte in CI erzeugt, nicht lokal

## A09 – Security Logging and Alerting Failures
- [ ] Logins, Logouts, fehlgeschlagene Autorisierungen, GPX-Importe, Einladungen, Instanz-Änderungen, Rücksetzungen und Rate-Limit-Treffer werden geloggt
- [ ] Keine Tokens, Cookies oder Secrets in Logs; Spitznamen nicht unnötig loggen
- [ ] Strukturierte Logs (eingebautes `ILogger`, JSON-Konsole), Aufbewahrung festgelegt

## A10 – Mishandling of Exceptional Conditions
- [ ] Globale Fehlerbehandlung liefert Problem Details ohne interne Details
- [ ] Fehler beim GPX-Parsen führen zu verständlicher Meldung, nicht zu `500`
- [ ] Abgebrochene SSE-Verbindungen werden sauber aufgeräumt (kein Speicherleck)
- [ ] Datenbank nicht erreichbar → definierter Fehler, eigener `Retry`-Helfer nur beim Start

## Datenschutz (Schweiz, revDSG)
- [ ] Suchende: keine Konten, keine IP-Speicherung, Spitzname freiwillig
- [ ] Datenschutzhinweis in der App (Geolocation bleibt im Browser, wird nicht übertragen)
- [ ] Kartenkacheln werden von swisstopo/OSM geladen: im Hinweis erwähnen

## Werkzeuge für die Prüfung (M6)
- OWASP ZAP: Baseline-Scan und API-Scan mit `/openapi/v1.json` gegen lokale oder Staging-Instanz
- `dotnet list package --vulnerable --include-transitive`, `npm audit`
- Manuelle Tests der Zugriffskontrolle (zweites Organisator-Konto, fremde IDs)
- Header-Check (z. B. `curl -I` bzw. securityheaders.com auf Staging)
