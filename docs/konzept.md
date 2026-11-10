# Konzept – Oschterhas

## Idee

Ein Team von Freiwilligen versteckt Osterhasen im Gelände. Beim Verstecken setzen sie in einer GPS-App (z. B. swisstopo-App) pro Versteck einen Wegpunkt und exportieren diese als GPX-Datei. Die Datei wird in Oschterhas importiert. Die Suchenden öffnen ihre Suche unter einer eigenen Adresse, z. B. **`tann.oschterhas.li`**, sehen die Hasen auf der Karte und markieren gefundene Hasen. Alle sehen live, welche Hasen noch zu finden sind.

Gespeichert werden nur **einzelne Wegpunkte**, keine Routen. Den Weg suchen sich die Suchenden selbst.

## Rollen

| Rolle | Login | Darf |
|---|---|---|
| **Plattform-Admin** | ja (Peakcode Identity) | Instanzen anlegen, bearbeiten, löschen; Organisator/innen erfassen (einladen) und Instanzen zuordnen; Konten sperren; alles, was Organisator/innen dürfen |
| **Organisator/in** | ja (Peakcode Identity, per Einladung) | Nur für die eigenen Instanzen: GPX importieren, Hasen bearbeiten, Suche starten/beenden, Gefunden-Status zurücksetzen |
| **Suchende** | nein | Instanz-Adresse öffnen, Karte ansehen, Hasen als gefunden markieren |

## Begriffe

- **Instanz** (`Instance`): eine Suche, z. B. „Ostersuche Tann 2027", erreichbar unter `<slug>.oschterhas.li`. Status *Entwurf → Aktiv → Beendet*.
- **Hase** (`Hare`): ein Versteck = ein GPX-Wegpunkt mit Position, Name, Beschreibung und Status *versteckt/gefunden*.
- **Slug**: der Teil vor `.oschterhas.li`, z. B. `duernten`.
- **Custom Domain**: optionale eigene Adresse einer Instanz, z. B. `ostern.ceviduernten.ch`. Sie funktioniert zusätzlich zu `duernten.oschterhas.li`.

## Abläufe

### 1. Instanz einrichten (Plattform-Admin)
1. Unter `admin.oschterhas.li` über Peakcode Identity einloggen.
2. Neue Instanz anlegen: Name, Slug (z. B. `tann`), Datum, Beschreibung.
3. Organisator/in erfassen: E-Mail und Name. Gibt es das Konto bei Peakcode Identity noch nicht, wird es angelegt, und die Person erhält eine Einladungsmail, mit der sie ihr Passwort setzt. Danach wird sie der Instanz zugeordnet.
4. Optional Custom Domain eintragen, z. B. `ostern.ceviduernten.ch`. Oschterhas zeigt die DNS-Anleitung an („CNAME auf `custom.oschterhas.li`"), die der Verein bei seinem DNS-Anbieter umsetzt. Sobald die Domain auf Oschterhas zeigt, wird sie automatisch erkannt und mit HTTPS-Zertifikat freigeschaltet. Danach zeigen QR-Code und Link die Custom Domain.

### 2. Suche vorbereiten (Organisator/in)
1. Einladung annehmen, unter `admin.oschterhas.li` einloggen, eigene Instanz wählen.
2. GPX-Datei hochladen. Jeder `<wpt>` wird zu einem Hasen (Name aus `<name>`, Beschreibung aus `<desc>`). Alles andere in der Datei wird ignoriert.
3. Vorschau auf der Karte prüfen. Hasen bei Bedarf umbenennen, verschieben, löschen oder hinzufügen. Erneuter Import: *ersetzen* oder *zusammenführen* (gleiche swisstopo-Wegpunkt-ID → aktualisieren).
4. Suche aktivieren → Adresse und QR-Code zum Ausdrucken werden angezeigt.

### 3. Suchen (Suchende)
1. QR-Code scannen oder `tann.oschterhas.li` öffnen – keine Registrierung.
2. Karte mit eigener Position (wenn erlaubt) und allen Hasen: offene farbig, gefundene grau.
3. Am Versteck: Hase antippen → „Gefunden!" → optional Spitzname angeben.
4. Alle Geräte sehen die Änderung sofort. Fortschritt „12 von 30 gefunden".
5. Vor dem Start (Entwurf) zeigt die Adresse „Die Suche hat noch nicht begonnen".

### 4. Abschluss (Organisator/in)
- Liste der nicht gefundenen Hasen (zum Einsammeln).
- Suche beenden → Suchende sehen „Suche beendet" mit Ergebnis.

## User Stories (MVP)

- Als Plattform-Admin möchte ich eine Instanz mit eigener Subdomain anlegen und eine Organisatorin dafür erfassen.
- Als Organisator/in möchte ich eine GPX-Datei importieren, damit ich die Verstecke nicht von Hand eintippen muss.
- Als Organisator/in möchte ich die importierten Hasen auf der Karte kontrollieren und korrigieren.
- Als Organisator/in möchte ich einen QR-Code zur Suche ausdrucken.
- Als Suchende/r möchte ich ohne Konto sofort die Karte sehen.
- Als Suchende/r möchte ich meine Position auf der Karte sehen.
- Als Suchende/r möchte ich einen Hasen als gefunden markieren, damit andere nicht umsonst suchen.
- Als Organisator/in möchte ich eine irrtümliche Gefunden-Markierung rückgängig machen.
- Als Nutzer/in möchte ich die App auf Hochdeutsch, Schweizerdeutsch oder Englisch verwenden.

## Später (nach MVP)

- Gefunden-Markierung nur in der Nähe erlauben (GPS-Distanz-Check)
- Teams/Ranglisten, Foto beim Finden
- Umkreis statt exakter Position, stufenweise Enthüllung
- Offline-Karte
- Mehrere Suchen pro Instanz (z. B. pro Jahr mit Archiv)
- Statistik nach der Suche

## Offene Fragen

> Beim Umsetzen klären und Antwort hier eintragen.

1. Sollen Suchende die **exakte Position** sehen oder nur einen Umkreis? (Annahme: exakt)
2. Darf ein gefundener Hase von Suchenden wieder auf „versteckt" gesetzt werden? (Annahme: nein, nur Organisator/innen)
3. Missbrauchsschutz beim Markieren ohne Login? (Umgesetzt: 30 Markierungen pro Minute und IP, grosszügig wegen Mobilfunk-NAT; Rücksetzen durch Organisator/in folgt in M4)
4. Hosting in der Schweiz: welcher Anbieter? Der DNS-Anbieter von `oschterhas.li` muss Wildcard-Einträge und eine API für DNS-01 (Caddy-Modul) unterstützen.
5. Was zeigt `oschterhas.li` selbst? (Annahme: kurze Info-Seite ohne Liste der Instanzen)
6. Standard-Sprache für Schweizer Browser (`de-CH`): umgesetzt ist Hochdeutsch, Mundart nur auf Wunsch. Soll stattdessen Mundart der Standard sein?
7. Sprache der Keycloak-Einladungsmail: Realm `parokia` ist nur Deutsch. Für Oschterhas auch Englisch? (Annahme: nur Deutsch, Standardvorlage)
8. Wer darf die Custom Domain eintragen? (Annahme: nur Plattform-Admin)
9. Soll `duernten.oschterhas.li` auf die Custom Domain weiterleiten, sobald diese verifiziert ist? (Annahme: nein, beide Adressen funktionieren)
10. Die automatisch von swisstopo ergänzten Adressen in `<desc>` (z. B. „(ZH) - Dürnten"): übernehmen oder verwerfen? (Annahme: übernehmen; Organisator/innen können sie in der Vorschau löschen)

## Geklärt

- Eine Instanz = eine Suche; im nächsten Jahr wird neu importiert (2026-11-09).
- Domain: `oschterhas.ch` ist besetzt, verwendet wird **`oschterhas.li`** (frei, noch zu registrieren) (2026-11-09).
- Namespace der swisstopo-Erweiterung: `https://prod-static.swisstopo-app.ch/xmlschemas/SwisstopoExtensions` (aus `samples/sample_data_2.gpx`).
- Custom Domains pro Instanz sind gewünscht (z. B. `ostern.ceviduernten.ch` zusätzlich zu `duernten.oschterhas.li`).
