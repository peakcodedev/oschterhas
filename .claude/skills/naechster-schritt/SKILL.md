---
name: naechster-schritt
description: Nimmt den nächsten offenen Punkt aus docs/roadmap.md, setzt ihn um, testet ihn und hakt ihn ab. Verwenden, wenn der/die Nutzer/in "weiter", "nächster Schritt" oder /naechster-schritt sagt.
---

# Nächster Roadmap-Schritt

1. Lies `docs/roadmap.md` und wähle den **ersten** offenen Punkt `[ ]` (oder den vom Nutzer genannten).
2. Lies die dazu relevanten Abschnitte in `docs/konzept.md`, `docs/architektur.md`, `docs/datenmodell.md`, `docs/api.md`.
3. Nenne kurz, was du umsetzt. Ist etwas unklar oder betrifft es eine „Offene Frage" im Konzept: **zuerst nachfragen**.
4. Umsetzen gemäss „Harte Regeln" und Konventionen in `CLAUDE.md` (Lizenzen, Migrationen, NgRx, i18n-Keys in allen drei Sprachen, OWASP-Checkliste).
5. Tests schreiben/ergänzen. In `apps/api`: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`; in `apps/web`: `npm run lint`, `npm test` – alles grün. Bei Modelländerung: Migration erzeugt und geprüft?
6. Bei UI-Änderungen: Dev-Server starten und im Browser prüfen (mobile Breite 375 px).
7. Punkt in `docs/roadmap.md` abhaken; Doku anpassen, falls sich Entscheidungen, API oder Datenmodell geändert haben.
8. Commit-Vorschlag machen (englisch, z. B. `feat(api): add GPX parser`), aber nur committen, wenn der Nutzer zustimmt.
