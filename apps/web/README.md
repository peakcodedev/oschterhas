# Oschterhas Web

Angular-App (Angular 22, zoneless, NgRx, Transloco). Befehle und Konventionen: [CLAUDE.md](../../CLAUDE.md).

- `src/app/core/host` – erkennt anhand des Hostnamens Verwaltung, Instanz oder Info-Seite
- `src/app/store` – NgRx-Root-Store (Feature-Stores folgen pro Feature)
- `src/app/features` – Seiten (`admin`, `search`, `landing`)
- `public/i18n` – Übersetzungen `de`, `gsw`, `en`
- `proxy.conf.json` – leitet `/api` an die API auf Port 5080 weiter (Host-Header bleibt erhalten)
