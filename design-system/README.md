# Oschterhas

Oschterhas ist die mobile Webapp für die Osterhasensuche: Kinder und Jugendliche suchen in der Gemeinde versteckte Osterhasen. Das Erscheinungsbild ist **fröhlich, warm und klar**. Es soll Kinder ansprechen, darf aber nie kindisch oder unruhig wirken. Auf dem Handy, draussen und im Sonnenlicht muss alles gut lesbar bleiben.

## Logo

Das Logo zeigt einen freundlichen Hasen, der in einer Lupe steckt. So sind die beiden Ideen der App verbunden: der **Osterhase** und die **Suche**. Die Ohren ragen über den Lupenring hinaus und machen das Zeichen verspielt. Logo und Zeichnung sind eigens für Oschterhas entworfen. Der Schriftzug ist in Poppins Bold gesetzt (SIL Open Font License) und in Pfade umgewandelt, damit er überall gleich aussieht.

| Datei | Wofür |
| --- | --- |
| `oschterhas-logo-quer.svg` | Standard-Logo, z. B. Kopfzeile, Website, Plakate |
| `oschterhas-logo-hoch.svg` | Startscreen, quadratische Flächen, Social Media |
| `oschterhas-signet.svg` | Nur die Lupe mit Hase, ab 24px; Favicon, Ladeanimation |
| `…-negativ.svg` | Auf dunklem Grund (ink): Schriftzug in eierschale, Griff hell |
| `oschterhas-app-icon*.svg/.png` | Homescreen-Icon (PWA), mit eigelb-Hintergrund |

**Regeln**

- «Oschter» steht immer in `ink`, «has» in `rueebli`. Auf dunklem Grund wird «Oschter» zu `eierschale`.
- Rund um das Logo bleibt so viel Freiraum, wie die Lupe breit ist (mindestens die halbe Lupe).
- Das Logo nicht verzerren, nicht drehen, keine Schatten und keine Farbverläufe hinzufügen.
- Unter 120px Breite das Signet statt des querformatigen Logos verwenden.

## Farben

Die Palette kommt aus der Osterwiese: eierschalenfarbener Grund, dazu Rüebli-Orange als Markenfarbe, Eigelb als Akzent und Wiesengrün für Erfolge.

- **Grundflächen:** `eierschale` als Seitenhintergrund, `weiss` für Karten.
- **Text:** immer `ink`. Sekundärtext in `ink-muted`.
- **Primär-Button:** Fläche `rueebli`, Text `ink`, Form `radius-pill`. Weisser Text auf Orange ist zu schwach (2.8:1) und darf nicht verwendet werden.
- **Erfolg («Hase gefunden!»):** `wiese` mit weissem Text, dazu gern ein `eigelb`-Badge.
- **Info/Hinweis:** Fläche `himmel` mit `ink`-Text; blaue Icons und Links in `himmel-text`.
- **Fehler:** `fehler`.
- `karamell` und `rosa` sind dem Hasen vorbehalten und werden nicht für Bedienelemente verwendet.

Alle Text-Farbkombinationen erreichen mindestens 4.5:1 (WCAG AA).

## Schriften

Beide Schriften sind kostenlos bei Google Fonts erhältlich (SIL Open Font License).

- **Poppins** (600, 700) für Titel. Sie ist geometrisch und freundlich und passt zum Schriftzug im Logo.
- **Atkinson Hyperlegible Next** (400, 700) für allen Fliesstext. Sie wurde für maximale Lesbarkeit entwickelt, und Buchstaben wie I, l und 1 lassen sich gut unterscheiden. Das ist ideal für Kinder und für Rätseltexte, die man draussen auf dem Handy liest.

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Atkinson+Hyperlegible+Next:wght@400;700&family=Poppins:wght@600;700&display=swap" rel="stylesheet">
```

Lauftext nie kleiner als 16px (`text`). Hinweise und Rätsel in `text-gross` (18px).

## Formen und Abstände

Runde, weiche Formen: Buttons als Pillen (`radius-pill`) wie die Hasenohren, Karten mit `radius-md`. Abstände folgen dem 4px-Raster (`space-1` bis `space-8`); der Seitenrand auf dem Handy ist `space-4`. Buttons sind mindestens 48px hoch, damit sie auch mit Handschuhen gut treffbar sind.

## Sprache

Kurz, direkt und ermutigend. Die Kinder werden mit «du» angesprochen. Dialekt bleibt dem Namen und kleinen Momenten vorbehalten («Super, gfunde!»), die eigentlichen Anleitungen sind auf Hochdeutsch, damit alle sie verstehen.
