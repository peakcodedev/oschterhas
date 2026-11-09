# oschterhas

Deine App für die Suche vom „Oschterhas": Freiwillige verstecken Osterhasen im Gelände und erfassen die Verstecke als GPX-Wegpunkte. Jede Suche hat ihre eigene Adresse, z. B. `tann.oschterhas.li`. Die Suchenden sehen die Hasen auf der Karte (swisstopo/OSM) und markieren gefundene Hasen – live für alle.

**Stack:** .NET 10 (Api · Core · Data · Common) · EF Core · PostgreSQL · Angular + NgRx + Leaflet · Login über Peakcode Identity (Keycloak) · Instanzen unter `<slug>.oschterhas.li` · PWA · DE / Schweizerdeutsch / EN

## Loslegen

Voraussetzungen: .NET 10 SDK, [nvm](https://github.com/nvm-sh/nvm), Docker.

```bash
docker compose up -d db                     # PostgreSQL auf Port 5434

cd apps/api
dotnet tool restore
dotnet run --project src/Oschterhas.Api     # http://localhost:5080, wendet Migrationen an

cd ../web
nvm install && nvm use
npm ci
npm start                                   # http://tann.oschterhas.localhost:4300
```

Lokale Adressen: `tann.oschterhas.localhost:4300` (Suche), `admin.oschterhas.localhost:4300` (Verwaltung), `oschterhas.localhost:4300` (Info-Seite). Für die Verwaltung muss Peakcode Identity laufen (`identity_peakcode`, Realm `oschterhas`). Chrome und Firefox lösen `*.localhost` selbst auf, für Safari braucht es Einträge in `/etc/hosts`.

Details und alle Befehle: [CLAUDE.md](CLAUDE.md).

## Dokumentation

- [Konzept](docs/konzept.md) – Rollen, Abläufe, offene Fragen
- [Architektur](docs/architektur.md) – Stack und Entscheidungen
- [Datenmodell](docs/datenmodell.md)
- [API](docs/api.md)
- [Lizenz-Richtlinie](docs/lizenzen.md)
- [OWASP-Checkliste](docs/owasp-checkliste.md)
- [Roadmap](docs/roadmap.md)
- [CLAUDE.md](CLAUDE.md) – Anleitung für die Entwicklung mit Claude
