# oschterhas

Deine App für die Suche vom „Oschterhas": Freiwillige verstecken Osterhasen im Gelände und erfassen die Verstecke als GPX-Wegpunkte. Jede Suche hat ihre eigene Adresse, z. B. `tann.oschterhas.li`. Die Suchenden sehen die Hasen auf der Karte (swisstopo/OSM) und markieren gefundene Hasen – live für alle.

**Stack:** .NET 10 (Api · Core · Data · Common) · EF Core · PostgreSQL · Angular + NgRx + Leaflet · Login über Peakcode Identity (Keycloak) · Instanzen unter `<slug>.oschterhas.li` · PWA · DE / Schweizerdeutsch / EN

## Loslegen

Voraussetzungen: [.NET 10 SDK](https://dotnet.microsoft.com/download), [nvm](https://github.com/nvm-sh/nvm), Docker.

```bash
docker compose up -d db
docker compose -f ../../identity_peakcode/docker-compose.yml up -d   # Keycloak + Mailpit

cd apps/api
dotnet tool update -g dotnet-ef
dotnet ef database update --project src/Oschterhas.Data --startup-project src/Oschterhas.Api
dotnet run --project src/Oschterhas.Api

cd ../web
nvm install && nvm use
npm ci
npm start                     # http://tann.oschterhas.localhost:4200 und http://admin.oschterhas.localhost:4200
```

> Das App-Grundgerüst wird in Meilenstein M0 erstellt – siehe [docs/roadmap.md](docs/roadmap.md).

## Dokumentation

- [Konzept](docs/konzept.md) – Rollen, Abläufe, offene Fragen
- [Architektur](docs/architektur.md) – Stack und Entscheidungen
- [Datenmodell](docs/datenmodell.md)
- [API](docs/api.md)
- [Lizenz-Richtlinie](docs/lizenzen.md)
- [OWASP-Checkliste](docs/owasp-checkliste.md)
- [Roadmap](docs/roadmap.md)
- [CLAUDE.md](CLAUDE.md) – Anleitung für die Entwicklung mit Claude
