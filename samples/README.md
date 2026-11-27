# Beispiel-GPX-Dateien

Testdaten für den `GpxParser` (Unit-Tests in `Oschterhas.Core.Tests`) und den Entwicklungs-Seed.

| Datei | Inhalt | Erwartung beim Import |
|---|---|---|
| `sample_data_2.gpx` | **Echter Export der swisstopo-App** (Tann/Dürnten): 65 Wegpunkte, `waypoint_id` 101–165, keine Tracks | 65 Hasen; 63 mit Standardnamen „Wegpunkt" → „Hase {Nummer}"; „8632 Tann" und „Loch" behalten ihren Namen; 2 mit Beschreibung |
| `beispiel-hasen.gpx` | 5 Wegpunkte, konstruierte Sonderfälle: `<cmt>` statt `<desc>`, ohne `<name>`, ohne `waypoint_id` | 5 Hasen, einer automatisch nummeriert, einer ohne `SourceWaypointId` |

Grössere Dateien (300, 1000, 1001 Wegpunkte) erzeugen die Tests selbst. Sie werden nicht eingecheckt.

Eigenschaften des swisstopo-Exports:
- XML-Deklaration ohne `encoding` (also UTF-8), Einrückung mit Tabs
- Namespace `xmlns:swisstopo="https://prod-static.swisstopo-app.ch/xmlschemas/SwisstopoExtensions"`
- `<desc>` enthält teilweise eine automatisch ergänzte Adresse mit Leerzeichen am Anfang oder Ende

Weitere Exporte aus anderen Apps (Garmin, OsmAnd, Komoot) sind willkommen.
