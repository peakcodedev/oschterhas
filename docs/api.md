# API – Oschterhas

Basis-Pfad `/api`, gleiche Origin wie die Angular-App. JSON (camelCase), Fehler als **Problem Details** (RFC 9457).
Massgebend ist das generierte OpenAPI-Dokument `/openapi/v1.json` (nur in der Entwicklung erreichbar); diese Seite ist die Planungsübersicht.

Zwei Arten von Hosts:
- **Admin-Host** `admin.oschterhas.li` (lokal `admin.oschterhas.localhost:4300`): Login, Verwaltung. 🔒 = Session-Cookie nötig; ändernde Requests zusätzlich mit Header `X-XSRF-TOKEN`.
- **Instanz-Host** `<slug>.oschterhas.li` (lokal `<slug>.oschterhas.localhost:4300`) **oder die verifizierte Custom Domain** der Instanz (z. B. `ostern.ceviduernten.ch`): öffentliche Endpunkte. Die Instanz ergibt sich aus dem Host, nicht aus dem Pfad.

Rollen: **PA** = Plattform-Admin, **ORG** = der Instanz zugeordnete Organisator/in (PA darf immer).

## Auth (Admin-Host, BFF mit Peakcode Identity)

| Methode | Pfad | Beschreibung |
|---|---|---|
| GET | `/auth/login?returnUrl=/instanzen` | Redirect zu Keycloak (Code + PKCE, per PAR); `returnUrl` nur lokal, sonst `/` |
| GET | `/auth/callback` | OIDC-Callback, setzt Session-Cookie; Konto ohne Zugang → Redirect `/?login=no_access` bzw. `disabled`/`failed` |
| GET | `/auth/me` | 🔒 `{ id, email, displayName, isPlatformAdmin }`; setzt das Cookie `XSRF-TOKEN` für Angular; ohne Session `401` |
| POST | `/auth/logout` | 🔒 als **Formular-POST** mit Feld `__RequestVerificationToken`, damit der Browser dem Redirect zum Keycloak-Logout folgen kann |

Alle Verwaltungs-Endpunkte antworten ohne Session mit `401` (nie mit Redirect). Mit Session, aber auf einem anderen Host als `admin.` → `404` (`admin.not_available`); da das Session-Cookie nur für den Admin-Host gilt, kommt auf Instanz-Adressen in der Praxis immer `401`. Ändernde Requests ohne gültiges CSRF-Token → `403` (`csrf_invalid`).

## Instanzen (Admin-Host)

| Methode | Pfad | Rolle | Beschreibung |
|---|---|---|---|
| GET | `/instances` | 🔒 PA, ORG | PA: alle; ORG: eigene |
| POST | `/instances` | 🔒 PA | `{ slug, name, description?, eventDate? }` → `201`; `409` bei vergebenem Slug |
| GET | `/instances/check-slug?slug=tann` | 🔒 PA | `{ slug, available, reason? }` |
| GET | `/instances/{id}` | 🔒 PA, ORG | inkl. Organisator/innen, `publicUrl` (Custom Domain wenn verifiziert) und `subdomainUrl` |
| PUT | `/instances/{id}` | 🔒 PA, ORG | `{ slug, name, description?, eventDate? }`; Slug ändern nur PA (`403 instance.slug_change_forbidden`) |
| POST | `/instances/{id}/status` | 🔒 PA, ORG | `{ status: Draft \| Active \| Ended }` |
| DELETE | `/instances/{id}` | 🔒 PA | `204` |

Organisator/innen sehen fremde Instanzen nicht: `404` statt `403`.

## Custom Domain (Admin-Host)

| Methode | Pfad | Rolle | Beschreibung |
|---|---|---|---|
| PUT | `/instances/{id}/custom-domain` | 🔒 PA | `{ domain }` → Status `Pending`; Antwort enthält DNS-Anleitung (`CNAME` → `custom.oschterhas.li`); `409` wenn vergeben, `422` wenn ungültig |
| POST | `/instances/{id}/custom-domain/verify` | 🔒 PA | sofort prüfen → `{ status, checkedAt, error? }`; rate-limitiert |
| DELETE | `/instances/{id}/custom-domain` | 🔒 PA | Domain entfernen, wird sofort nicht mehr bedient |

Die Instanz-Antworten (`GET /instances/{id}`) enthalten `customDomain`, `customDomainStatus` und `publicUrl` (Custom Domain, falls verifiziert, sonst Subdomain).

## Domain-Verifizierung & TLS (ohne `/api`-Präfix)

| Methode | Pfad | Host | Beschreibung |
|---|---|---|---|
| GET | `/.well-known/oschterhas-verification` | jede Custom Domain mit Status `Pending`/`Verified` | antwortet mit dem Token dieser Domain (`text/plain`), sonst `404` |
| GET | `/internal/tls/allowed?domain=…` | nur internes Netz (Caddy) | `200`, wenn die Domain zu einer Instanz gehört und `Verified` ist, oder wenn sie eine gültige Subdomain der Basis-Domain ist; sonst `404`. Von aussen nicht erreichbar (Caddy blockiert den Pfad, die API prüft zusätzlich die Quell-IP) |

## Organisator/innen (Admin-Host)

| Methode | Pfad | Rolle | Beschreibung |
|---|---|---|---|
| POST | `/instances/{id}/organizers` | 🔒 PA | `{ email, displayName }` → Konto bei Keycloak anlegen/finden, Einladung senden, zuordnen |
| DELETE | `/instances/{id}/organizers/{userId}` | 🔒 PA | Zuordnung entfernen |
| POST | `/instances/{id}/organizers/{userId}/resend-invitation` | 🔒 PA | Einladung erneut senden |
| GET | `/users` | 🔒 PA | alle Konten |
| PATCH | `/users/{id}` | 🔒 PA | `{ isActive?, isPlatformAdmin? }` |

## Hasen & GPX (Admin-Host)

| Methode | Pfad | Rolle | Beschreibung |
|---|---|---|---|
| GET | `/instances/{id}/hares` | 🔒 PA, ORG | alle Hasen |
| POST | `/instances/{id}/hares` | 🔒 PA, ORG | Hase manuell `{ name, description?, lat, lon }` |
| PUT | `/instances/{id}/hares/{hareId}` | 🔒 PA, ORG | `{ name, description, lat, lon }` (Name leer → „Hase {Nummer}") |
| DELETE | `/instances/{id}/hares/{hareId}` | 🔒 PA, ORG | |
| POST | `/instances/{id}/hares/{hareId}/reset` | 🔒 PA, ORG | Gefunden-Status zurücksetzen |
| POST | `/instances/{id}/gpx/preview` | 🔒 PA, ORG | `multipart/form-data` `file` → `{ waypoints, warnings }`, speichert nichts |
| POST | `/instances/{id}/gpx?mode=Replace\|Merge` | 🔒 PA, ORG | `file` → `{ created, updated, deleted, warnings }`; max. 20 Uploads/Minute |

Upload max. 5 MB, max. 1000 Wegpunkte (`413`/`422` mit Problem Details).

## Öffentlich (Instanz-Host, ohne Login)

| Methode | Pfad | Beschreibung |
|---|---|---|
| GET | `/public/instance` | Instanz (Name, Beschreibung, Status, Fortschritt); bei `Active`/`Ended` inkl. Hasen; unbekannter Slug → `404` |
| POST | `/public/hares/{hareId}/found` | `{ nickname? }` → `{ hareId, foundAt, foundBy }`. `404` wenn der Hase nicht zur Instanz des Hosts gehört, `409` mit `code` `hare.already_found` oder `instance.not_active`, `422` bei Spitzname > 40 Zeichen oder Steuerzeichen, `429` (`rate_limited`) bei mehr als 30 Anfragen/Minute pro IP |
| GET | `/public/events` | SSE-Stream der Instanz (max. 50 offene Verbindungen pro IP) |

### SSE-Events

Der Stream beginnt sofort mit `ready` (inkl. `retry: 3000`) und sendet alle 25 s `ping`, wenn sonst nichts passiert. Bleibt der Client 60 s ohne Nachricht, baut er die Verbindung neu auf. Nach jedem erneuten `ready` lädt er die Instanz vollständig nach.

```
event: ready
data: {}
retry: 3000

event: ping
data: {}

event: hare.found
data: {"hareId":"…","foundAt":"2027-03-28T10:12:00Z","foundBy":"Lina"}

event: hare.reset
data: {"hareId":"…"}

event: hares.changed
data: {}

event: instance.status
data: {"status":"Ended"}
```
