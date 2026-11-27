# Betrieb – Oschterhas

Produktion läuft wie die anderen Peakcode-Anwendungen auf dem gemeinsamen Server:

- **peakcode_infra**: gemeinsamer Caddy (HTTPS für alle Anwendungen), Docker-Netz `proxy`, Self-hosted Runner (Label `peakcode-prod`, Benutzer `deploy`).
- **identity_peakcode**: zentrale Keycloak-Instanz mit dem Realm `oschterhas` (`production/realms/oschterhas.json`, Clients `oschterhas-bff` und `oschterhas-user-admin`).
- **dieses Repo**: Image (API liefert auch die Angular-App aus), Postgres, Caddy-Dateien, Deploy-Workflow.

## Aufbau

```text
Internet ──▶ Caddy (peakcode_infra, :80/:443)
              ├─ auth.<…>           ──▶ identity-keycloak:8080      (identity_peakcode)
              └─ alle übrigen Hosts ──▶ oschterhas-api:8080          (dieses Repo)
                                         └─ postgres (eigenes Netz, nicht am Proxy)
```

| Datei | Zweck |
|---|---|
| `Dockerfile` | Baut Angular (Node) und API (.NET), Laufzeit `aspnet` als Benutzer `app` (nicht root), Web-Build in `wwwroot` |
| `production/docker-compose.yml` | `api`, `migrate` (einmalig, Profil), `postgres`; nur `api` hängt am Netz `proxy` (Alias `oschterhas-api`) |
| `production/deploy.sh` | Läuft auf dem Server: Postgres → Migrationen → neue Version → Caddy-Dateien installieren → alte Images aufräumen |
| `production/caddy/oschterhas.caddy` | Site-Block: Catch-all `https://` mit On-Demand-Zertifikaten, `/internal/*` gesperrt, SSE ungepuffert; `http://` nur für die Domain-Verifizierung |
| `production/caddy/oschterhas.global.caddy` | Globale Option `on_demand_tls { ask … }` → `/internal/tls/allowed` |
| `production/db/10-app-user.sh` | Legt beim ersten Start der DB den App-Benutzer `oschterhas_app` an (nur Daten lesen/schreiben) |
| `.github/workflows/deploy.yml` | CI-Prüfungen, Validierung, dann Build und Deploy auf dem Server |

### Entscheidungen

- **Zertifikate on demand statt Wildcard** (2026-11-27): Jeder Host (`oschterhas.li`, `admin.`, jede Instanz-Subdomain, verifizierte Custom Domains) bekommt beim ersten Aufruf ein eigenes Let's-Encrypt-Zertifikat. Caddy fragt vorher `/internal/tls/allowed`; die API erlaubt nur eigene Hosts mit existierender Instanz und verifizierte Custom Domains. Damit braucht der gemeinsame Caddy kein DNS-Modul und keinen API-Token beim DNS-Anbieter. Let's-Encrypt-Limit: 50 neue Zertifikate pro Woche und registrierter Domain, für Oschterhas reichlich.
- **Globale Option im gemeinsamen Caddy**: Caddy verlangt für On-Demand-TLS ein globales `ask`. Dafür hat peakcode_infra den Mechanismus `caddy-site install-global` (Branch `feat/global-options`). Oschterhas ist damit Besitzerin von `on_demand_tls` und der Catch-all-Site auf dem Server; andere Anwendungen mit eigenen Hostnamen sind nicht betroffen (spezifischere Site-Blöcke gewinnen).
- **Image wird auf dem Server gebaut** (kein Registry): Tag = Commit-SHA. Das vorherige Image bleibt für einen Rollback liegen.
- **Migrationen als eigener Schritt**: `docker compose run --rm migrate` startet das Image mit `--migrate` und dem Schema-Besitzer `oschterhas`; die laufende App nutzt `oschterhas_app` ohne DDL-Rechte.
- **Keycloak über den öffentlichen Hostnamen**: Die API spricht Keycloak über `https://<KEYCLOAK_HOSTNAME>` an (OIDC und Admin-API), damit der Issuer genau dem der Logins entspricht. Der Server muss sich selbst über seine öffentliche Adresse erreichen (Hairpin, bei Cloud-Servern üblich).

## Einrichtung (einmal)

Voraussetzungen: peakcode_infra ist deployed – **mit** dem Mechanismus für globale Optionen (Branch `feat/global-options` gemergt) –, identity_peakcode ist deployed.

1. **DNS** (beim Anbieter von `oschterhas.li`): A/AAAA-Einträge für `oschterhas.li`, `*.oschterhas.li` und `custom.oschterhas.li` auf den Server.
2. **Identity** (`identity_peakcode/production/.env` auf dem Server): `OSCHTERHAS_ADMIN_URL=https://admin.oschterhas.li` und je ein zufälliges Secret für `OSCHTERHAS_OIDC_CLIENT_SECRET` und `OSCHTERHAS_USER_ADMIN_CLIENT_SECRET`, `OSCHTERHAS_SMTP_FROM`. Danach identity neu deployen (wendet den Realm an).
3. **Server**: Verzeichnis `/srv/oschterhas` (Besitzer `deploy`, siehe README von peakcode_infra, Schritt 1). Darin `.env` aus [`production/.env.example`](../production/.env.example) anlegen, mit denselben beiden Secrets wie bei identity und neuen Passwörtern für die DB:

   ```bash
   sudo -u deploy install -m 600 /dev/null /srv/oschterhas/.env
   ```

   ```bash
   openssl rand -base64 32
   ```

4. **GitHub** (Repo `peakcodedev/oschterhas`, muss privat bleiben):
   - Organisation → Runner-Gruppe `production`: Repo `oschterhas` freigeben.
   - Settings → Environments → `production`, nur Branch `main`.
   - Rulesets: `main` schützen (Pull Request nötig, kein Force-Push).
   - Optionale Variablen: `DEPLOY_PATH` (Standard `/srv/oschterhas`), `CADDY_SITE` (Standard `/srv/infra/bin/caddy-site`).
5. **Erster Deploy**: Merge nach `main` (oder Actions → Deploy → Run workflow).
6. **Erste Plattform-Admin**: In der Keycloak-Admin-Konsole im Realm `oschterhas` einen Benutzer anlegen (E-Mail, Aktion „Update Password"), dessen ID als `OSCHTERHAS_INITIAL_PLATFORM_ADMIN` in `/srv/oschterhas/.env` eintragen und den Workflow erneut starten. Beim ersten Login unter `https://admin.oschterhas.li` wird diese Person Plattform-Admin (nur solange Oschterhas noch keine Benutzer hat). Alle weiteren Organisator/innen werden aus der App eingeladen.

## Ablauf eines Deploys

Bei jedem Push auf `main` (`.github/workflows/deploy.yml`):

1. `ci` – dieselben Prüfungen wie bei Pull Requests (Tests, Lint, Format, Migrationen, Lizenzen, Audit).
2. `validate` (GitHub-gehostet) – Compose-Datei, Shellcheck, Caddy-Dateien im Aufbau des gemeinsamen Caddy, Image-Build.
3. `deploy` (Runner auf dem Server) – Image `oschterhas-api:<sha>` bauen, `production/` nach `/srv/oschterhas` kopieren (`.env` bleibt), `deploy.sh`:
   - Postgres starten, Migrationen anwenden, neue Version starten und auf `/health` warten,
   - `caddy-site install-global oschterhas …` und `caddy-site install oschterhas …` (mit Rollback bei Fehlern),
   - ältere Images entfernen (aktuelles und vorheriges bleiben).

Pull Requests mit Änderungen an `Dockerfile` oder `production/` laufen nur `validate`; Code aus Pull Requests läuft nie auf dem Server.

## Betrieb

```bash
cd /srv/oschterhas
```

```bash
docker compose ps
```

```bash
docker compose logs -f api
```

**Rollback** auf das vorherige Image (Tag aus `docker image ls oschterhas-api`); Migrationen werden nicht zurückgerollt, darum Modelländerungen abwärtskompatibel halten:

```bash
OSCHTERHAS_IMAGE=oschterhas-api:<vorheriger-sha> sh /srv/oschterhas/deploy.sh
```

**Datenbank-Sicherung** (Ablage und Aufbewahrung noch offen, siehe unten):

```bash
docker compose exec -T postgres pg_dump -U oschterhas -Fc oschterhas > oschterhas-$(date +%F).dump
```

Hinweise:

- `db/10-app-user.sh` läuft nur, wenn das Volume `postgres-data` neu angelegt wird. `POSTGRES_APP_PASSWORD` später ändern: `ALTER ROLE oschterhas_app PASSWORD '…'` und `.env` anpassen.
- Die Data-Protection-Schlüssel (Login- und CSRF-Cookies) liegen im Volume `keys`. Geht es verloren, müssen sich alle neu anmelden.
- Logs sind JSON auf der Konsole (`docker compose logs`), ohne IP-Adressen und Spitznamen.

## Lokal testen

Den ganzen Ablauf ohne Server prüfen (eigener Compose-Projektname, damit die Entwicklungs-DB unberührt bleibt; `local_certs` ersetzt Let's Encrypt):

1. `docker build -t oschterhas-api:local .`
2. Netz `proxy` anlegen und einen Caddy mit `config/` aus peakcode_infra starten, zusätzlich eine globale Datei mit `local_certs`.
3. `production/` in ein Testverzeichnis kopieren, `.env` mit Testwerten anlegen und `deploy.sh` mit `COMPOSE_PROJECT_NAME=oschterhas-e2e`, `CADDY_SITE`, `CADDY_SITES_DIR`, `CADDY_GLOBAL_DIR`, `CADDY_CONTAINER` aufrufen.
4. Mit `curl -k --resolve <host>:<port>:127.0.0.1 …` prüfen.

Geprüft am 2026-11-27: Startseite, Deep Links der Verwaltung, öffentliche API einer Instanz, Live-Stream durch Caddy, Cache-Header und gzip, HSTS (weitergeleitete Header werden nur vom Proxy-Netz akzeptiert), kein Zertifikat für fremde Domains, `/internal/*` von aussen gesperrt, Domain-Verifizierung über HTTP, Weiterleitung HTTP → HTTPS, Site anderer Anwendungen bleibt unberührt, App-Benutzer darf das Schema nicht ändern.

## Offen

- Server/Hoster in der Schweiz (gemeinsam mit peakcode_infra), Domain `oschterhas.li` registrieren.
- Automatische DB-Sicherung mit Ablage in der Schweiz und Aufbewahrungsfrist; Log-Aufbewahrung.
- Live-Prüfung auf dem Server: HTTPS/HSTS, `Secure`-Cookies, Header-Check, Login über die zentrale Identity, Einladung per Mail.
