# Datenmodell – Oschterhas

Entwurf. Massgebend sind später die Entitäten in `apps/api/src/Oschterhas.Data/Entities/` und die Migrationen in `Oschterhas.Data/Migrations/`. Tabellen und Spalten in `snake_case`, Enums als Text.

```
AppUser n───m Instance   (über InstanceOrganizer)
Instance 1───n Hare
Hare     1───n FoundEvent (Verlauf)
```

## AppUser

Organisator/innen und Plattform-Admins. Angelegt beim Erfassen durch den Plattform-Admin oder per Seed. Keine Passwörter in dieser DB.

| Feld | Typ | Bemerkung |
|---|---|---|
| id | uuid | PK |
| identityIssuer | text | z. B. `http://localhost:8180/realms/oschterhas` |
| identitySubject | text | `sub` aus Keycloak; **eindeutig zusammen mit `identityIssuer`** |
| email | text | Anzeige und Einladung, nicht zur Zuordnung beim Login |
| displayName | text | |
| isPlatformAdmin | bool | |
| isActive | bool | gesperrt → kein Zugang |
| locale | text | `de` \| `gsw` \| `en` |
| createdAt / lastLoginAt | timestamptz | `lastLoginAt` null = Einladung noch nicht angenommen |

## Instance (Instanz = eine Suche)

| Feld | Typ | Bemerkung |
|---|---|---|
| id | uuid | PK |
| slug | text | eindeutig, Subdomain, 3–40 Zeichen `[a-z0-9-]`, nicht reserviert |
| name | text | max. 100 |
| description | text? | max. 2000 |
| eventDate | date? | |
| status | enum `Draft` \| `Active` \| `Ended` | Suchende sehen Hasen nur bei `Active`/`Ended` |
| customDomain | text? | z. B. `ostern.ceviduernten.ch`, normalisiert (klein, Punycode), eindeutig, wenn gesetzt |
| customDomainStatus | enum `None` \| `Pending` \| `Verified` \| `Failed` | bedient wird nur `Verified` |
| customDomainToken | text? | zufälliges Verifizierungs-Token (`RandomNumberGenerator`, 32 Byte, Base64Url) |
| customDomainVerifiedAt | timestamptz? | |
| customDomainLastCheckAt | timestamptz? | |
| customDomainFailedChecks | int | Fehlversuche in Folge, ab 3 → `Failed` |
| createdAt / updatedAt | timestamptz | |

## InstanceOrganizer

| Feld | Typ | Bemerkung |
|---|---|---|
| instanceId | uuid | PK-Teil, FK → Instance (Cascade) |
| userId | uuid | PK-Teil, FK → AppUser |
| createdAt | timestamptz | |

## Hare (Hase = ein GPX-Wegpunkt)

| Feld | Typ | Bemerkung |
|---|---|---|
| id | uuid | PK |
| instanceId | uuid | FK → Instance (Cascade) |
| number | int | laufende Nummer pro Instanz, eindeutig mit `instanceId` |
| name | text | aus `<name>` oder „Hase {number}", max. 100 |
| description | text? | aus `<desc>`/`<cmt>`, max. 500 |
| lat / lon | double precision | WGS84 |
| elevation | double precision? | aus `<ele>` |
| sourceWaypointId | text? | aus `<extensions>/swisstopo:waypoint_id`; eindeutig pro Instanz, wenn gesetzt |
| foundAt | timestamptz? | `null` = versteckt |
| foundBy | text? | freiwilliger Spitzname, max. 40 |
| xmin | xid | Concurrency-Token (PostgreSQL-Systemspalte) |
| createdAt / updatedAt | timestamptz | |

## FoundEvent (Verlauf)

| Feld | Typ | Bemerkung |
|---|---|---|
| id | uuid | PK |
| hareId | uuid | FK → Hare (Cascade) |
| type | enum `Found` \| `Reset` | |
| nickname | text? | |
| userId | uuid? | FK → AppUser, gesetzt bei `Reset` |
| createdAt | timestamptz | |

Keine IP-Adressen speichern (nur flüchtig fürs Rate-Limit im Speicher). Keine Routen/Tracks.
