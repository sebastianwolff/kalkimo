# GRAV-Integration (Entwurf)

## Einbettung der UIs
- Inline-Einbindung (ohne iFrame) über eigene HTML-Tags, die die Vue-App an Ort und Stelle mounten:
  - `<ge-heizlast lang="de"></ge-heizlast>`
  - `<ge-solar lang="de"></ge-solar>`
  - `<ge-waermepumpe lang="de"></ge-waermepumpe>`
  Die Grav-Seiten (Markdown) enthalten diese Tags direkt im Inhalt; ein Grav‑Plugin lädt die passenden JS‑Bundles und Tokens und mountet die Komponenten.

## Layout
- Navigation links, Inhalte rechts (responsive)
- Gemeinsames Styling über Tailwind Design Tokens

## Routing & SEO
- Saubere URLs pro Rechner, Deeplinks
- Meta-Tags/OpenGraph, Sitemap aus GRAV

## Mehrsprachigkeit (i18n)
- GRAV als Quelle der aktiven Sprache nutzen (Sprach-Prefix im Pfad oder GRAV-Language-Kontext)
- Sprache an die eingebettete SPA übergeben (z. B. `?lang=de`), HTML `lang` synchron halten
- Pflichtsprachen: de, en, fr, it, es, ru, pt, nl, da, sv, pl; passende CSV aus `translations/` laden
- Fallback vermeiden: Bei nicht vorhandenen Keys Build/CI fehlschlagen lassen

---

## KRITISCH Verlinkung in GRAV-Artikeln

Dieser Abschnitt ist **PFLICHTLEKTÜRE** beim Schreiben oder Übersetzen von GRAV-Artikeln.

### Markdown vs HTML Links

GRAV behandelt Markdown-Links und HTML-Links unterschiedlich bezüglich Sprach-Prefixes:

| Link-Typ | Sprach-Prefix | Beispiel |
|----------|---------------|----------|
| `[text](/path)` Markdown | **NEIN** — GRAV fügt automatisch hinzu | `[Mehr](/blog/heizlast/artikel)` |
| `<a href="/lang/path">` HTML | **JA** — explizit erforderlich | `<a href="/de/blog/heizlast/artikel">` |

**Falsch** (doppelter Prefix):
```markdown
[Artikel](/de/blog/heizlast/artikel)  <!-- FALSCH: Wird zu /de/de/blog/... -->
```

**Richtig**:
```markdown
[Artikel](/blog/heizlast/artikel)  <!-- RICHTIG: GRAV fügt /de/ automatisch hinzu -->
```

### page.find() mit Raw Routes (Twig)

In Twig-Templates **IMMER** Raw Routes (Ordnernamen) verwenden, **NIEMALS** übersetzte Slugs:

```twig
<!-- RICHTIG: Raw Route basierend auf Ordnerstruktur -->
{% set p = page.find('/blog/photovoltaik/photovoltaik-ratgeber') %}

<!-- FALSCH: Übersetzter Slug (funktioniert nur in einer Sprache) -->
{% set p = page.find('/knowledge/photovoltaics/complete-guide') %}
```

**Warum?**
- Raw Routes sind sprachunabhängig — GRAV löst sie zur korrekten Seite auf
- `p.url` gibt automatisch die lokalisierte URL zurück
- Ein Pfad für alle 11 Sprachen

**Raw Route erstellen** (Ordnernamen ohne numerische Prefixe):

| Verzeichnis | Raw Route |
|-------------|-----------|
| `04.blog/` | `/blog/` |
| `04.blog/107.heizlast/` | `/blog/heizlast/` |
| `04.blog/107.heizlast/01.grundlagen/` | `/blog/heizlast/grundlagen` |

### rawlink Plugin

Das Plugin ermöglicht Raw Routes direkt in Markdown-Links:

```markdown
<!-- GLEICH in default.de.md, default.en.md, default.it.md, etc. -->
[Weiterlesen](raw:/blog/heizlast/grundlagen "Heizlast verstehen")
```

Das Plugin löst automatisch zur lokalisierten URL auf:
- DE: `/de/blog/heizlast/grundlagen`
- EN: `/en/knowledge/heating-load/basics`
- IT: `/it/sapere/carico-termico/basi`

**Wann rawlink verwenden?**

Eigentlich immer, wenn interne markdown links gesetz werden! Es ist der sicherste weg für korrekte links in allen sprachen, egal wie die slugs geändert oder angepasst werden. Ausserdem erlabt es ein auffinden von links über alle sprachen hinweg in den sourcen.

| Anwendungsfall | Empfehlung |
|----------------|------------|
| Links in mehreren Sprachversionen | `raw:` Prefix verwenden |
| Dynamischer Content (Twig) | `page.find()` + `p.url` |
| Externe Links | Normale `https://...` URLs |

### Multi-Level Slug-Übersetzung

**KRITISCH**: JEDE Ebene einer URL kann sprachspezifische Slugs haben!

```
/{SPRACHE}/{KATEGORIE-SLUG}/{UNTERKATEGORIE-SLUG}/{ARTIKEL-SLUG}
```

**Hauptkategorie-Slugs**:

| Sprache | Blog | Rechner |
|---------|------|---------|
| DE | `blog` | `rechner` |
| EN | `knowledge` | `calculators` |
| FR | `savoir` | `calculateurs` |
| IT | `sapere` | `calcolatori` |
| ES | `conocimiento` | `calculadoras` |

**Unterkategorie-Slugs (Beispiel Heizlast)**:

| Sprache | Slug | Volle URL |
|---------|------|-----------|
| DE | `heizlast` | `/de/blog/heizlast/...` |
| EN | `heating-load` | `/en/knowledge/heating-load/...` |
| FR | `charge-thermique` | `/fr/savoir/charge-thermique/...` |
| IT | `carico-termico` | `/it/sapere/carico-termico/...` |
| ES | `carga-termica` | `/es/conocimiento/carga-termica/...` |

### Workflow für Links in Artikeln

Es sind immer raw:links oder a href mit page.find() und nur mit Raw Routes (Ordnernamen) zu bevorzugen (siehe vorheriger Abschnitt). hierdurch werden falsche verlinkungen durch unterschiedliche sprachen oder spätere slug anpassungen vermieden

Ansonsten wird folgendes Vorgehen empfohlen:


1. **Zielseite ermitteln** (z.B. `grav/user/pages/04.blog/107.heizlast/artikel/`)
2. **Slug aus Frontmatter lesen** (`default.{lang}.md` → `slug:` Feld)
3. **Alle Ebenen prüfen** — Kategorie, Unterkategorie, Artikel
4. **Korrekte URL bauen**:
   ```
   DE: /de/blog/heizlast/artikel
   EN: /en/knowledge/heating-load/article
   IT: /it/sapere/carico-termico/articolo
   ```

### Slug finden

```bash
# Slug in italienischer Version finden
grep "slug:" grav/user/pages/04.blog/107.heizlast/artikel/default.it.md

# Hauptkategorie-Slug für Französisch
grep "slug:" grav/user/pages/04.blog/default.fr.md
```

### Checkliste für GRAV-Artikel

- [ ] rawlink Plugin für sprachübergreifende Links (`raw:/path`) nur mit Raw Routes (Ordnernamen)
- [ ] page.find() nur mit Raw Routes (Ordnernamen)
Alternativ:
- [ ] Markdown-Links OHNE Sprach-Prefix (`/path` statt `/de/path`)
- [ ] HTML-Links MIT Sprach-Prefix (`/de/path`)
- [ ] Alle URL-Ebenen korrekt übersetzt
- [ ] Links in ALLEN Sprachversionen getestet

---

## Sicherheit
- CORS-Policy für Domains, CSRF bei eingebetteten Formularen
- TLS erzwingen, Security Header (CSP, X-Frame-Options beachten)

## Build/Deployment der Inline-Komponenten
- Jedes Frontend liefert zusätzlich ein „Embed"-Bundle (IIFE), das auf den HTML‑Tags mountet (`ge-*`).
- Die gebauten JS‑Dateien werden in den Grav‑Plugin‑Assets abgelegt (`grav-integration/plugins/gecomponents/assets/`).
- Tokens‑CSS wird zentral über das Plugin ausgeliefert.

## Lokale Entwicklung mit GRAV

### Voraussetzungen
- Docker muss installiert sein
- Der GRAV-Container wird über `docker-compose.dev.yml` gestartet

### Build & Deploy Workflow

1. **Embed-Bundles bauen (Development-Modus)**
   ```bash
   npm run build:embeds:dev
   ```
   Dies baut alle drei Frontends als Web Components und kopiert sie automatisch nach:
   - `grav-integration/plugins/gecomponents/assets/` (Template)
   - `grav/user/plugins/gecomponents/assets/` (Lokale Dev-Umgebung)
   - `frontends/*/public/translations/` (Standalone Dev-Server für jedes Frontend)

2. **Nur Übersetzungen und Assets kopieren (ohne Rebuild)**
   ```bash
   npm run copy:embeds
   ```

3. **GRAV-Container starten**
   ```bash
   npm run docker:dev
   ```
   GRAV ist dann erreichbar unter: http://localhost:8080

### Verzeichnisstruktur

```
Gebaeudeenergie/
├── grav-integration/plugins/gecomponents/   # Template für Produktion
│   ├── assets/
│   │   ├── ge-heizlast.embed.js
│   │   ├── ge-solar.embed.js
│   │   ├── ge-waermepumpe.embed.js
│   │   ├── tokens.css
│   │   └── translations/
│   │       ├── de.csv
│   │       ├── en.csv
│   │       ├── fr.csv
│   │       ├── it.csv
│   │       └── es.csv
│   └── gecomponents.php
│
├── grav/user/plugins/gecomponents/          # Lokale Dev-Umgebung (Docker-Mount)
│   └── assets/                              # Gleiche Struktur wie oben
│
├── grav/user/data/datenblaetter/            # Datenblätter (PDFs)
│   └── klimaanlagen/                        # Klimaanlagen-Datenblätter nach Hersteller
│
├── frontends/*/public/translations/         # Kopien für Standalone-Dev (von copy-embeds.js)
│
└── translations/                            # Master-Quelldateien für Übersetzungen
    ├── de.csv
    ├── en.csv
    └── ...
```

### Wichtige Hinweise

- **Development vs Production**: Verwende immer `build:embeds:dev` für lokale Entwicklung. Der Befehl `build:embeds` (ohne `:dev`) baut für Produktion und verwendet die Produktions-API-URL.

- **Übersetzungen**: Neue Übersetzungskeys müssen in `translations/*.csv` hinzugefügt werden. Nach dem Hinzufügen `npm run copy:embeds` ausführen, um sie in alle Zielverzeichnisse zu kopieren.

- **Browser-Cache**: Nach Änderungen an den Embed-Bundles den Browser-Cache leeren (Ctrl+Shift+R).

- **GRAV-Container**: Der Container mountet `grav/user` nach `/config/www/user`. Änderungen in `grav/user/plugins/gecomponents/assets/` sind sofort im Container sichtbar.

## Banner-System

Das Theme unterstützt seitenspezifische Banner im Header-Bereich mit separaten Varianten für Desktop und Mobile.

### Frontmatter-Optionen

Banner werden im YAML-Header (Frontmatter) der jeweiligen Seite konfiguriert:

```yaml
---
title: Seitentitel

# Option 1: Separate HTML-Banner für Desktop und Mobile (empfohlen)
banner_html_desktop: '<div>Großer Desktop-Banner</div>'
banner_html_mobile: '<div>Kompakter Mobile-Banner</div>'

# Option 2: Separate Bild-Banner
banner_image_desktop: 'banner-wide.png'
banner_image_mobile: 'banner-mobile.png'
banner_alt: 'Alternativer Text'
banner_link: 'https://example.com'

# Option 3: Legacy - Einzelner Banner (wird auf Mobile ausgeblendet)
banner_html: '<div>Banner für alle Geräte</div>'
banner_image: 'banner.png'
---
```

### Prioritätsreihenfolge

1. **Responsive HTML-Banner** (`banner_html_desktop` / `banner_html_mobile`)
2. **Legacy HTML-Banner** (`banner_html`) - wird auf Mobile versteckt
3. **Responsive Bild-Banner** (`banner_image_desktop` / `banner_image_mobile`)
4. **Legacy Bild-Banner** (`banner_image`)
5. **Theme-Defaults** aus `config.theme.banners`
6. **Fallback-Bilder** aus `theme://images/banners/`

### Beispiel mit Werbenetzwerk (DAA)

```yaml
---
title: Heizlast-Berechnung
banner_html_desktop: '<daa-wgt data-itg="DESKTOP_KAMPAGNE_ID"></daa-wgt><script async src="https://hub.daa.net/js/hub.js"></script>'
banner_html_mobile: '<daa-wgt data-itg="MOBILE_KAMPAGNE_ID"></daa-wgt><script async src="https://hub.daa.net/js/hub.js"></script>'
---
```

### CSS-Klassen

| Klasse | Beschreibung |
|--------|--------------|
| `.ge-banner` | Basis-Container für alle Banner |
| `.ge-banner-desktop` | Nur auf Desktop sichtbar (>1024px) |
| `.ge-banner-mobile` | Nur auf Mobile sichtbar (≤1024px) |
| `.ge-banner-html` | Marker für HTML/Script-Banner |
| `.ge-banner-img` | Styling für Bild-Banner |

### Responsive Verhalten

- **Desktop (>1024px)**: `.ge-banner-desktop` sichtbar, `.ge-banner-mobile` versteckt
- **Mobile (≤1024px)**: `.ge-banner-mobile` sichtbar, `.ge-banner-desktop` versteckt
- **Legacy-Banner** (ohne `_desktop`/`_mobile` Suffix): Werden auf Mobile komplett ausgeblendet

### Dateipfade für Bild-Banner

Bilder werden in folgender Reihenfolge gesucht:
1. Page Media (im selben Ordner wie die Markdown-Datei)
2. Theme-Verzeichnis: `grav/user/themes/ge-theme/images/banners/`
