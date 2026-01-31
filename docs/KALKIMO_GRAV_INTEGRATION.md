# GRAV-Integration – Kalkimo Planner

## Einbettung der Kalkimo-App

Inline-Einbindung (ohne iFrame) über ein Custom HTML-Element, das die Vue-App an Ort und Stelle mountet:

```html
<km-planner lang="de"></km-planner>
```

Die GRAV-Seiten (Markdown) enthalten diesen Tag direkt im Inhalt. Das Custom Plugin `kalkimo-embed` lädt das JS-Bundle und mountet die Komponente.

### Attribute

| Attribut | Werte | Beschreibung |
|----------|-------|-------------|
| `lang` | `de`, `en` | UI-Sprache der App |
| `api-url` | URL | Backend-API URL (optional, Fallback aus Plugin-Config) |
| `mode` | `full`, `demo` | Vollversion oder Demo ohne Login |

---

## Layout

- Navigation oben (fixed), Inhalte darunter (responsive)
- Rechner-Seite: App nutzt die volle verfügbare Breite
- Gemeinsames Styling über CSS Custom Properties (`--km-*`)
- Mobile-First: Basis-Layout für Mobile, Erweiterung via `min-width` Media Queries

---

## Routing & SEO

### Saubere URLs

Jede Seite hat sprachspezifische Slugs:

| Seite | DE | EN |
|-------|----|----|
| Startseite | `/de/` | `/en/` |
| Rechner | `/de/rechner` | `/en/calculator` |
| Funktionen | `/de/funktionen` | `/en/features` |
| Preise | `/de/preise` | `/en/pricing` |
| Wissen | `/de/wissen` | `/en/knowledge` |
| Ueber uns | `/de/ueber-uns` | `/en/about` |
| Kontakt | `/de/kontakt` | `/en/contact` |

### Meta-Tags & SEO

- Jede Seite definiert `title`, `description`, `keywords` im Frontmatter
- OpenGraph-Tags und Twitter Cards automatisch via Theme-Partial
- `hreflang`-Tags für DE/EN Verknüpfung
- JSON-LD strukturierte Daten (Organization, SoftwareApplication)
- XML-Sitemap automatisch via GRAV Sitemap-Plugin

---

## Mehrsprachigkeit (i18n)

### GRAV-Konfiguration

```yaml
# system.yaml
languages:
  supported:
    - de
    - en
  default_lang: de
  include_default_lang: true
  translations: true
  http_accept_language: true
```

### Sprachübergabe an die eingebettete App

1. GRAV ermittelt die aktive Sprache aus dem URL-Prefix (`/de/`, `/en/`)
2. Das Twig-Template setzt das `lang`-Attribut auf dem Custom Element:
   ```twig
   <km-planner lang="{{ grav.language.getActive }}"></km-planner>
   ```
3. Die Vue-App liest das Attribut und aktiviert die entsprechende Locale
4. HTML `lang`-Attribut wird im `<html>`-Tag synchron gehalten

### Pflichtsprachen

- **Deutsch (de)** – Primärsprache, vollständige Inhalte
- **Englisch (en)** – Sekundärsprache, vollständige Inhalte

### Übersetzungsdateien

Für die eingebettete App werden JSON-Übersetzungsdateien verwendet:

```
plugins/kalkimo-embed/assets/translations/
├── de.json
└── en.json
```

Die App-internen Übersetzungen kommen aus dem bestehenden `vue-i18n`-Setup der Frontend-App und werden beim Embed-Build integriert.

### Fallback-Strategie

- Englische Inhalte, die noch nicht übersetzt sind: Deutsche Fassung als Fallback anzeigen
- App-Übersetzungen: Kein Fallback – alle Keys müssen in beiden Sprachen vorhanden sein

---

## KRITISCH: Verlinkung in GRAV-Artikeln

### Markdown vs HTML Links

GRAV behandelt Markdown-Links und HTML-Links unterschiedlich bezüglich Sprach-Prefixes:

| Link-Typ | Sprach-Prefix | Beispiel |
|----------|---------------|----------|
| `[text](/path)` Markdown | **NEIN** — GRAV fügt automatisch hinzu | `[Mehr](/wissen/artikel)` |
| `<a href="/lang/path">` HTML | **JA** — explizit erforderlich | `<a href="/de/wissen/artikel">` |

**Falsch** (doppelter Prefix):
```markdown
[Artikel](/de/wissen/artikel)  <!-- FALSCH: Wird zu /de/de/wissen/... -->
```

**Richtig**:
```markdown
[Artikel](/wissen/artikel)  <!-- RICHTIG: GRAV fügt /de/ automatisch hinzu -->
```

### page.find() mit Raw Routes (Twig)

In Twig-Templates **IMMER** Raw Routes (Ordnernamen) verwenden, **NIEMALS** übersetzte Slugs:

```twig
<!-- RICHTIG: Raw Route basierend auf Ordnerstruktur -->
{% set p = page.find('/wissen/immobilienkauf-leitfaden') %}

<!-- FALSCH: Übersetzter Slug (funktioniert nur in einer Sprache) -->
{% set p = page.find('/knowledge/property-buying-guide') %}
```

**Warum?**
- Raw Routes sind sprachunabhängig — GRAV löst sie zur korrekten Seite auf
- `p.url` gibt automatisch die lokalisierte URL zurück
- Ein Pfad für beide Sprachen

**Raw Route erstellen** (Ordnernamen ohne numerische Prefixe):

| Verzeichnis | Raw Route |
|-------------|-----------|
| `02.rechner/` | `/rechner/` |
| `05.wissen/` | `/wissen/` |
| `05.wissen/01.immobilienkauf-leitfaden/` | `/wissen/immobilienkauf-leitfaden` |

### Multi-Level Slug-Übersetzung

Jede Ebene einer URL kann sprachspezifische Slugs haben:

```
/{SPRACHE}/{KATEGORIE-SLUG}/{ARTIKEL-SLUG}
```

**Hauptseiten-Slugs:**

| Seite | DE-Slug | EN-Slug |
|-------|---------|---------|
| Rechner | `rechner` | `calculator` |
| Funktionen | `funktionen` | `features` |
| Preise | `preise` | `pricing` |
| Wissen | `wissen` | `knowledge` |
| Ueber uns | `ueber-uns` | `about` |
| Kontakt | `kontakt` | `contact` |

### Workflow für Links in Artikeln

1. **Zielseite ermitteln** (z.B. `user/pages/05.wissen/01.immobilienkauf-leitfaden/`)
2. **Slug aus Frontmatter lesen** (`item.{lang}.md` → `slug:` Feld)
3. **Alle Ebenen prüfen** — Kategorie und Artikel
4. **Korrekte URL bauen**:
   ```
   DE: /de/wissen/immobilienkauf-leitfaden
   EN: /en/knowledge/property-buying-guide
   ```

### Checkliste für GRAV-Artikel

- [ ] `page.find()` nur mit Raw Routes (Ordnernamen)
- [ ] Markdown-Links OHNE Sprach-Prefix (`/path` statt `/de/path`)
- [ ] HTML-Links MIT Sprach-Prefix (`/de/path`)
- [ ] Alle URL-Ebenen korrekt übersetzt
- [ ] Links in ALLEN Sprachversionen getestet

---

## Sicherheit

### Allgemein

- CORS-Policy: Nur erlaubte Domains für API-Zugriffe
- CSRF-Schutz bei Formularen (GRAV Forms Plugin integriert)
- TLS/HTTPS erzwungen (301-Redirect HTTP → HTTPS)
- Security Headers:
  - `Content-Security-Policy` (CSP)
  - `X-Frame-Options: SAMEORIGIN`
  - `X-Content-Type-Options: nosniff`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy` (Kamera, Mikrofon etc. deaktiviert)

### Content Security Policy (CSP)

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self';
  style-src 'self' 'unsafe-inline';
  img-src 'self' data:;
  font-src 'self';
  connect-src 'self' https://api.kalkimo.de;
  frame-ancestors 'none';
```

**Hinweis:** `'unsafe-inline'` für Styles nur falls nötig (GRAV Admin). Für die Produktionsseite möglichst vermeiden und durch Nonces ersetzen.

### DSGVO-Konformität

- Fonts self-hosted (kein Google Fonts CDN)
- Analytics datenschutzkonform (Plausible/Umami self-hosted)
- Cookie-Consent vor Tracking-Cookies
- Impressum + Datenschutzerklärung vorhanden
- Auftragsverarbeitung (AVV) mit Hoster

---

## Build & Deployment der Inline-Komponenten

### Embed-Bundle

Das Frontend-Projekt (`apps/frontend`) liefert zusätzlich zum normalen SPA-Build ein Embed-Bundle (IIFE), das auf dem Custom Element `km-planner` mountet.

```bash
# Embed-Bundle bauen (Development)
cd apps/frontend
npm run build:embed:dev

# Embed-Bundle bauen (Production)
npm run build:embed
```

### Deployment-Pfade

Die gebauten Dateien werden in das GRAV Plugin kopiert:

```
cms/grav/user/plugins/kalkimo-embed/assets/
├── km-planner.embed.js        # Vue-App IIFE Bundle
├── km-planner.embed.css       # App-Styles (falls extern)
└── translations/
    ├── de.json
    └── en.json
```

### Verzeichnisstruktur

```
kalkimo/
├── cms/grav/
│   └── user/
│       ├── config/
│       │   ├── system.yaml           # GRAV System-Konfiguration
│       │   ├── site.yaml             # Site-Metadaten
│       │   └── plugins/
│       │       ├── langswitcher.yaml
│       │       ├── sitemap.yaml
│       │       └── kalkimo-embed.yaml
│       │
│       ├── plugins/kalkimo-embed/    # Custom Plugin
│       │   ├── kalkimo-embed.php
│       │   ├── kalkimo-embed.yaml
│       │   ├── blueprints.yaml
│       │   ├── assets/
│       │   │   ├── km-planner.embed.js
│       │   │   ├── km-planner.embed.css
│       │   │   └── translations/
│       │   │       ├── de.json
│       │   │       └── en.json
│       │   └── templates/partials/
│       │       └── kalkimo-embed.html.twig
│       │
│       ├── themes/kalkimo/           # Custom Theme
│       │   ├── kalkimo.php
│       │   ├── kalkimo.yaml
│       │   ├── blueprints.yaml
│       │   ├── templates/            # Twig-Templates
│       │   ├── css/                  # Stylesheets
│       │   ├── js/                   # Theme-JavaScript
│       │   ├── images/               # Statische Bilder
│       │   └── fonts/                # Self-hosted Fonts (Inter)
│       │
│       └── pages/                    # Inhalt (Markdown)
│           ├── 01.home/
│           ├── 02.rechner/
│           ├── 03.funktionen/
│           ├── 04.preise/
│           ├── 05.wissen/
│           ├── 06.ueber-uns/
│           ├── 07.kontakt/
│           ├── impressum/
│           ├── datenschutz/
│           └── agb/
│
├── apps/frontend/                    # Vue.js App (SPA + Embed)
├── backend/                          # ASP.NET Core API
└── docker-compose.dev.yml            # Dev-Infrastruktur
```

---

## Lokale Entwicklung mit GRAV

### Voraussetzungen

- Docker muss installiert sein
- Der GRAV-Container wird über `docker-compose.dev.yml` gestartet (zu erweitern)

### GRAV-Container starten

```bash
# docker-compose.dev.yml um GRAV-Service erweitern (siehe WEBSITE_PLAN.md)
docker compose -f docker-compose.dev.yml up grav
```

GRAV ist dann erreichbar unter: http://localhost:8080

### Build & Deploy Workflow (Entwicklung)

1. **Embed-Bundle bauen**
   ```bash
   cd apps/frontend
   npm run build:embed:dev
   ```
   Dies baut die Vue-App als Web Component und kopiert das Bundle nach:
   - `cms/grav/user/plugins/kalkimo-embed/assets/`

2. **Nur Assets kopieren (ohne Rebuild)**
   ```bash
   npm run copy:embed
   ```

3. **GRAV-Container starten**
   ```bash
   docker compose -f docker-compose.dev.yml up grav
   ```
   GRAV ist dann erreichbar unter: http://localhost:8080

### Wichtige Hinweise

- **Development vs Production**: `build:embed:dev` für lokale Entwicklung, `build:embed` für Produktion (nutzt Produktions-API-URL).
- **Übersetzungen**: Frontend-Übersetzungen aus `apps/frontend/src/i18n/locales/` werden beim Embed-Build in JSON konvertiert und nach `plugins/kalkimo-embed/assets/translations/` kopiert.
- **Browser-Cache**: Nach Änderungen am Embed-Bundle den Browser-Cache leeren (Ctrl+Shift+R).
- **GRAV-Container**: Mountet `cms/grav/user` nach `/config/www/user`. Änderungen in `cms/grav/user/` sind sofort im Container sichtbar.
- **GRAV Admin**: Erreichbar unter http://localhost:8080/admin – beim ersten Start Admin-Account anlegen.

---

## Theme-Konfiguration

### kalkimo.yaml (Theme-Defaults)

```yaml
enabled: true
streams:
  schemes:
    theme:
      type: ReadOnlyStream
      prefixes:
        '': [user/themes/kalkimo]

# Site-Branding
brand:
  name: "Kalkimo Planner"
  tagline_de: "Immobilien-Investment. Professionell kalkuliert."
  tagline_en: "Real Estate Investment. Professionally Calculated."

# Navigation
nav:
  items:
    - label_de: "Startseite"
      label_en: "Home"
      route: "/"
    - label_de: "Rechner"
      label_en: "Calculator"
      route: "/rechner"
    - label_de: "Funktionen"
      label_en: "Features"
      route: "/funktionen"
    - label_de: "Preise"
      label_en: "Pricing"
      route: "/preise"
    - label_de: "Wissen"
      label_en: "Knowledge"
      route: "/wissen"
    - label_de: "Kontakt"
      label_en: "Contact"
      route: "/kontakt"

# Footer
footer:
  copyright: "Kalkimo"
  social:
    # linkedin: "https://linkedin.com/company/kalkimo"
    # twitter: "https://twitter.com/kalkimo"

# SEO Defaults
seo:
  og_image: "theme://images/og-default.jpg"
  twitter_site: "@kalkimo"
```

### Frontmatter-Beispiel (Rechner-Seite)

```yaml
---
title: Immobilien-Investitionsrechner
slug: rechner
metadata:
  description: "Berechnen Sie Rendite, Cashflow und Steuervorteile Ihrer Immobilien-Investition mit dem Kalkimo Planner."
  keywords: "Immobilien Rechner, Investitionsrechner, Rendite berechnen, AfA Rechner"
  og:
    title: "Kalkimo – Immobilien-Investitionsrechner"
    type: website
    image: "og-rechner.jpg"
---

# Immobilien-Investitionsrechner

Kalkulieren Sie Ihre Immobilien-Investition mit bankstandardnaher Genauigkeit.

<km-planner lang="de"></km-planner>
```

---

## Cookie-Consent Integration

### Empfehlung: Klaro (Open Source)

```html
<!-- Im base.html.twig vor </body> -->
<script defer src="{{ url('theme://js/klaro.min.js') }}"></script>
<script>
var klaroConfig = {
  lang: '{{ grav.language.getActive }}',
  privacyPolicy: {
    de: '/de/datenschutz',
    en: '/en/privacy'
  },
  services: [
    {
      name: 'analytics',
      purposes: ['analytics'],
      cookies: [/^_pk_.*/],
      required: false
    }
  ]
};
</script>
```

---

## Performance-Optimierung

### Lazy Loading

- Bilder: `loading="lazy"` für alle Bilder unterhalb des Folds
- Embed-Bundle: Nur auf der Rechner-Seite laden (nicht global)
- Fonts: `font-display: swap` für schnelles erstes Rendering

### Caching

```yaml
# .htaccess oder Nginx-Config
# Statische Assets: 1 Jahr Cache
# HTML: Kein Cache (oder kurz: 5 Min)
# Embed-Bundle: Hash im Dateinamen für Cache-Busting
```

### Bundle-Größe

- Embed-Bundle: Ziel < 200 KB (gzip)
- Theme CSS: Ziel < 30 KB (gzip)
- Theme JS: Ziel < 10 KB (gzip)
