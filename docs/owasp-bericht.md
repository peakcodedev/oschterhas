# OWASP-Bericht – Oschterhas

| | |
|---|---|
| Datum | 2026-11-10 |
| Geprüfter Stand | Branch `chore/project-setup`, Meilensteine M0–M5 plus Korrekturen aus M6 (Commit „M6") |
| Grundlage | OWASP Top 10:2025, OWASP ASVS 5.0 Level 1 (ausgewählte L2-Punkte), [owasp-checkliste.md](owasp-checkliste.md) |
| Umgebung | Lokal: API (Development), Angular-Dev-Server mit Produktions-CSP, PostgreSQL, Keycloak `identity_peakcode` |
| Geprüft von | Claude (Code-Review, automatisierte Scans, Browser-Tests) |

## Zusammenfassung

Keine kritischen oder hohen Schwachstellen gefunden. Die automatisierten Scans (OWASP ZAP, Paket-Prüfungen) zeigen **keine Fehler**. Die manuelle Prüfung fand **acht Lücken**, alle in M6 behoben: fehlende Security-Header, fehlendes Sicherheits-Logging, fehlender Datenschutzhinweis, ein Inline-Skript, das die CSP gebrochen hätte, u. a. Offen sind Punkte, die erst mit dem Produktionsbetrieb (M7) prüfbar sind, sowie einige Tests mit weiteren Konten.

## Vorgehen

1. **Code-Review** gegen jeden Punkt der Checkliste (Suche nach rohem SQL, HTML-Einfügen, Logging von Geheimnissen, Secrets im Repo, `AllowAnonymous`, Fehlerausgaben).
2. **Paket-Prüfung**: `dotnet list package --vulnerable --include-transitive`, `npm audit`, Lizenzprüfung.
3. **OWASP ZAP** (`zaproxy/zap-stable`, Stand 2026-11-03), API-Scan mit aktiven Angriffen gegen alle 31 importierten Operationen (99 URLs) aus dem OpenAPI-Dokument. Ein Header-Ersetzer schickt alle Requests mit Host `tann.oschterhas.localhost`, damit die öffentlichen Endpunkte erreicht werden.
4. **Browser-Tests** (M1–M5 und M6): Zugriffskontrolle, Host-Trennung, CSRF, CSP im Betrieb, Custom Domains, Datenschutzseite.

Befehl für die Wiederholung (API muss laufen):

```bash
W=$(mktemp -d); curl -s http://localhost:5080/openapi/v1.json -o $W/openapi.json; chmod 777 $W
docker run --rm -v $W:/zap/wrk:rw zaproxy/zap-stable zap-api-scan.py -t /zap/wrk/openapi.json -f openapi \
  -O http://host.docker.internal:5080 -r zap-api.html -I \
  -z "-config replacer.full_list(0).description=host -config replacer.full_list(0).enabled=true -config replacer.full_list(0).matchtype=REQ_HEADER -config replacer.full_list(0).matchstr=Host -config replacer.full_list(0).regex=false -config replacer.full_list(0).replacement=tann.oschterhas.localhost"
```

## Ergebnisse der Scans

| Prüfung | Ergebnis |
|---|---|
| ZAP API-Scan (aktiv) | 120 Regeln bestanden, 0 Fehler. 1 Warnung „Unexpected Content-Type" für `/api/public/events`: Fehlalarm, der SSE-Stream liefert absichtlich `text/event-stream` |
| ZAP, erster Lauf | Warnung `Cross-Origin-Resource-Policy` fehlt → behoben (F9) |
| `dotnet list package --vulnerable` | keine anfälligen Pakete (alle 7 Projekte) |
| `npm audit --omit=dev` | 0 Funde |
| `npm audit` (inkl. Entwicklungswerkzeuge) | 10 × mittel in `minimatch`/`glob` (über ESLint) → akzeptiert (F10) |
| Lizenzprüfung | alle Pakete gemäss [lizenzen.md](lizenzen.md) |

## Funde und Massnahmen

| # | Kategorie | Fund | Massnahme | Status |
|---|---|---|---|---|
| F1 | A02 | Keine Security-Header (CSP, `nosniff`, Frame-Schutz, Referrer-, Permissions-Policy), kein HSTS | Middleware `SecurityHeaders` für alle Antworten; strikte CSP (`script-src 'self'`, kein `unsafe-eval`, Kacheln nur von swisstopo/OSM, `frame-ancestors 'none'`, `form-action` nur eigene Origin + Peakcode Identity); HSTS ausserhalb Development; gleiche CSP im Angular-Dev-Server, damit Verstösse früh auffallen | behoben, im Browser geprüft (Suche, Karte, Live-Stream, Datenschutzseite ohne CSP-Verstoss) |
| F2 | A02 | `Server: Kestrel` verrät die Technik | `AddServerHeader = false` | behoben |
| F3 | A02/A05 | Production-Build fügte ein Inline-`<script>` für „kritisches CSS" ein; unter der CSP wäre das Stylesheet nie aktiviert worden (App ohne Gestaltung) | `inlineCritical: false` im Production-Build; Build enthält keine Inline-Skripte mehr | behoben |
| F4 | A10 | Ungültiges JSON liefert Parser-Details (Position, erwartete Zeichen) | `AllowInputFormatterExceptionMessages = false` | behoben |
| F5 | A09 | Kaum Sicherheits-Logging | Middleware `SecurityEventLogging` (alle 401/403/429 unter `/api`, ohne IP-Adressen); Audit-Einträge in Core: Login/Ablehnung, Benutzeränderungen, Instanzen, Organisator/innen, GPX-Import, Löschen/Zurücksetzen von Hasen, Custom Domains, Funde (ohne Spitzname), Logout; JSON-Konsolenlogs ausserhalb Development | behoben |
| F6 | A04/A06 | Kein Vertrauen in Proxy-Header konfigurierbar (Rate-Limit pro Proxy-IP statt pro Client), Cookie-Schlüssel nicht persistent | `ReverseProxy:TrustedNetworks` (nur diese Netze dürfen `X-Forwarded-For/-Proto` setzen; leer = niemand), `ReverseProxy:DataProtectionKeysDirectory`, fester Anwendungsname für die Schlüssel | umgesetzt; Werte werden in M7 gesetzt |
| F7 | Datenschutz | Kein Datenschutzhinweis | Seite `/datenschutz` auf allen Hosts (de, gsw, en), verlinkt von Suche, Info-Seite und Login | behoben; Angaben zum Betreiber fehlen noch (Konzept, offene Frage 11) |
| F8 | A03 | Kein Dependabot, Actions nicht gepinnt | `.github/dependabot.yml` (NuGet, npm, Actions, Docker); Actions auf Commit-SHAs | behoben |
| F9 | A02 | `Cross-Origin-Resource-Policy` fehlt (ZAP) | `same-origin` | behoben |
| F10 | A03 | `minimatch` (ReDoS bei präparierten Glob-Mustern) in ESLint-Abhängigkeiten | Nur Entwicklungswerkzeug mit eigenen Mustern, nicht im ausgelieferten Code; Behebung nur per Major-Update von ESLint. Dependabot schlägt das Update vor | akzeptiert |
| F11 | Prüfbarkeit | ZAP importierte aus dem OpenAPI-3.1-Dokument nur 3 von 29 Operationen | OpenAPI-Dokument als 3.0; Import als Datei mit Ziel-URL | behoben |

## Bedrohungsmodell (Kurzfassung)

| Angreifer / Fall | Mögliche Wirkung | Schutz |
|---|---|---|
| Suchende/r markiert absichtlich alle Hasen als gefunden | Suche verdorben | Rate-Limit 30/min pro IP, Verlauf in `found_events`, Organisator/in setzt zurück; bei Bedarf Limit pro Instanz ergänzen |
| Jemand öffnet eine Suche vor dem Start und notiert Verstecke | Vorteil, Verstecke geplündert | Hasen im Status `Draft` werden nie ausgeliefert |
| Organisator/in A greift auf Instanz B zu (ID-Tausch) | fremde Daten ändern | Zuordnung in jedem Core-Service geprüft, fremde Instanzen → `404` |
| Hase einer anderen Instanz über eine Subdomain markieren | falsche Funde | Instanz kommt aus dem Host, Hase muss zur Instanz gehören |
| CSRF auf Verwaltungs-Endpunkte | Änderungen im Namen eines Admins | Antiforgery-Token, `SameSite=Lax`, Cookie nur auf `admin.` |
| XSS über Namen/Beschreibungen aus GPX oder Spitznamen | Session-Übernahme | Angular-Interpolation, Popups per `textContent`, strikte CSP, httpOnly-Cookie |
| Böswillige Custom Domain zeigt auf interne Dienste | SSRF | eigene DNS-Auflösung, nur öffentliche Adressen, keine Redirects |
| Fremde lassen Zertifikate für beliebige Domains ausstellen | Let's-Encrypt-Limit, Missbrauch | `/internal/tls/allowed` nur aus internem Netz, nur verifizierte Domains |
| Gefälschter Host-Header | falsche Links, Cache-Vergiftung | Host nur zur Auflösung; Links/QR aus gespeicherter Adresse |
| Präparierte GPX-Datei (XXE, Entity-Expansion, Riesendatei) | Dateien lesen, DoS | DTD verboten, 5 MB, 1000 Wegpunkte, Streaming |
| Viele offene Live-Verbindungen | Speicher/Threads | max. 50 pro IP, Heartbeat, Aufräumen bei Abbruch |
| Gesperrtes Konto mit noch gültiger Session | weiter Zugriff | Benutzer wird bei jedem Request aus der DB geladen |

## Offene Punkte

| Punkt | Wann |
|---|---|
| HTTPS/HSTS, `Secure`-Cookies und CSP mit `upgrade-insecure-requests` live prüfen; Header-Check (securityheaders.com) | M7 (Staging) |
| `ReverseProxy:TrustedNetworks`, `DataProtectionKeysDirectory`, `InternalNetwork:AllowedNetworks` setzen | M7 |
| Container nicht als root, Basis-Images gepinnt, DB-Benutzer mit minimalen Rechten, Log-Aufbewahrung | M7 |
| Produktions-Realm bei Peakcode Identity ohne Testbenutzer | M7 |
| CI-Workflow einmal auf GitHub laufen lassen | sobald ein Remote existiert |
| Von Hand: Login als Organisator (Otto) – sieht nur Tann; Login ohne Zugang (Nina); Abmelden (auch unter CSP) | nächster manueller Test |
| Authentifizierter ZAP-Scan der Verwaltung (Session-Cookie + CSRF-Token) | optional, vor Produktivstart |
| Betreiber und Kontakt im Datenschutzhinweis | vor Produktivstart |
