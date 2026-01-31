# Kalkimo Website – Projektplan

## Zielsetzung

Erstellung einer zweisprachigen (DE/EN), mobile-first Website auf Basis von **GRAV CMS** als Host-Plattform für den Kalkimo Planner (Immobilien-Investitionsrechner). Die Website dient als Marketing-Präsenz, Wissensplattform und Einstiegspunkt für die eingebettete Web-App.

---

## 1. Seitenstruktur & Informationsarchitektur

### Primäre Seiten

| Seite | DE-Slug | EN-Slug | Inhalt |
|-------|---------|---------|--------|
| **Startseite** | `/` | `/en/` | Hero, Kurzvorstellung, Features, CTA |
| **Rechner** | `/rechner` | `/en/calculator` | Eingebettete Kalkimo Vue-App |
| **Funktionen** | `/funktionen` | `/en/features` | Detaillierte Feature-Übersicht |
| **Preise** | `/preise` | `/en/pricing` | Lizenzmodelle & Entitlements |
| **Wissen** | `/wissen` | `/en/knowledge` | Blog/Ratgeber-Artikel (SEO) |
| **Ueber uns** | `/ueber-uns` | `/en/about` | Team, Mission, Hintergrund |
| **Kontakt** | `/kontakt` | `/en/contact` | Kontaktformular |

### Pflichtseiten (Legal, DE)

| Seite | Slug | Hinweis |
|-------|------|---------|
| **Impressum** | `/impressum` | Pflicht nach §5 TMG |
| **Datenschutz** | `/datenschutz` | Pflicht nach DSGVO |
| **AGB** | `/agb` | Falls Bezahldienste angeboten |
| **Cookie-Hinweis** | Banner/Modal | Consent-Management |

### GRAV-Verzeichnisstruktur (Pages)

```
user/pages/
├── 01.home/
│   ├── default.de.md
│   └── default.en.md
├── 02.rechner/
│   ├── default.de.md
│   └── default.en.md
├── 03.funktionen/
│   ├── default.de.md
│   └── default.en.md
├── 04.preise/
│   ├── default.de.md
│   └── default.en.md
├── 05.wissen/
│   ├── blog.de.md
│   ├── blog.en.md
│   ├── 01.immobilienkauf-leitfaden/
│   │   ├── item.de.md
│   │   └── item.en.md
│   └── 02.steuervorteile-immobilien/
│       ├── item.de.md
│       └── item.en.md
├── 06.ueber-uns/
│   ├── default.de.md
│   └── default.en.md
├── 07.kontakt/
│   ├── form.de.md
│   └── form.en.md
├── impressum/
│   └── default.de.md
├── datenschutz/
│   ├── default.de.md
│   └── default.en.md
└── agb/
    ├── default.de.md
    └── default.en.md
```

---

## 2. Design & Branding

### Grundsätze

- **Mobile-First**: Alle Layouts werden zuerst für Smartphones entworfen, dann per `min-width` Media Queries für größere Screens erweitert
- **Design-System**: Basierend auf dem bestehenden Style Guide (`style-guide_web.md`) mit Anpassung der CSS-Variable-Prefixes von `--pv-*` auf `--km-*` (Kalkimo)
- **Farbpalette**: Beibehaltung der Slate/Emerald-Palette (professionell, seriös – passend für Finanz/Immobilien-Kontext)
- **Typografie**: Inter als Primärschrift (bereits im Style Guide definiert)

### Mobile-First Breakpoints

```css
/* Basis: Mobile (< 640px) */
/* sm: >= 640px  – Kleine Tablets */
/* md: >= 768px  – Tablets */
/* lg: >= 1024px – Laptops */
/* xl: >= 1280px – Desktops */
```

### Branding-Anpassungen

| Element | Wert |
|---------|------|
| Produktname | Kalkimo Planner |
| Tagline DE | Immobilien-Investment. Professionell kalkuliert. |
| Tagline EN | Real Estate Investment. Professionally Calculated. |
| Logo | Noch zu erstellen (SVG, horizontal + Icon-Only-Variante) |
| Favicon | Abgeleitet vom Logo |

---

## 3. Seitenkonzepte (Mobile-First)

### 3.1 Startseite

**Mobile-Layout (Top → Bottom):**
1. **Header** – Logo + Hamburger-Menü + Sprachschalter (DE/EN)
2. **Hero** – Headline, Subline, 1 CTA-Button (vertikal gestapelt)
3. **Trust-Leiste** – Icons/Badges (Bankstandard, DSGVO, deutsche Steuerlogik)
4. **Feature-Highlights** – 3–4 Cards (1 Spalte, vertikal gescrollt)
5. **Rechner-Teaser** – Screenshot/Mockup + CTA "Jetzt kalkulieren"
6. **Testimonials/Social Proof** – Horizontal scrollbare Cards
7. **Blog-Teaser** – 2–3 aktuelle Artikel
8. **Footer** – Accordion-Menü, Legal-Links, Sprachschalter

**Desktop-Erweiterung (>= 1024px):**
- Hero mit Side-by-Side Layout (Text links, Mockup rechts)
- Feature-Cards in 2–3 Spalten Grid
- Footer als Multi-Column Grid

### 3.2 Rechner-Seite

**Mobile-Layout:**
1. **Header** (minimiert, mehr Platz für App)
2. **Kurze Einleitung** (1–2 Sätze)
3. **Eingebettete Kalkimo-App** – Vollbreite, responsive
4. **Hilfe/FAQ** – Accordion unterhalb

**Desktop-Erweiterung:**
- App zentriert mit max-width
- Optional: Sidebar mit Kontexthilfe

**Technisch:** Die Vue-App wird via Custom Element `<km-planner lang="de"></km-planner>` eingebettet (siehe GRAV-Integration).

### 3.3 Funktionen-Seite

**Mobile-Layout:**
- Alternierend: Feature-Beschreibung + Illustration/Screenshot
- Jede Feature-Sektion als eigener Block mit Anchor-Link
- Feature-Kategorien:
  - Kaufkalkulation & Nebenkosten
  - Finanzierungsvergleich
  - Deutsche Steuerlogik (AfA, §82b, §23 EStG)
  - Cashflow & Liquiditätsplanung
  - Sanierung & Instandhaltung
  - Szenarien & Stress-Tests
  - Multi-Investor
  - PDF/CSV Export

### 3.4 Preise-Seite

**Mobile-Layout:**
- Pricing-Cards vertikal gestapelt
- Empfohlener Plan hervorgehoben
- Feature-Vergleichstabelle (horizontal scrollbar auf Mobile)
- FAQ zu Abrechnung/Kündigung

### 3.5 Wissen/Blog

**Mobile-Layout:**
- Artikelliste mit Thumbnail, Titel, Auszug
- Kategorie-Filter (horizontal scrollbare Tags)
- Einzelartikel: Fließtext mit Inhaltsverzeichnis (Sticky auf Desktop)

**Geplante Anfangsartikel:**
1. Leitfaden: Erste Immobilie als Kapitalanlage
2. AfA bei Immobilien: Was Investoren wissen müssen
3. Cashflow-Analyse: Positive vs. negative Rendite
4. Sanierungskosten richtig kalkulieren
5. Steuervorteile bei Immobilien-Investments

### 3.6 Kontakt-Seite

- GRAV Forms Plugin
- Felder: Name, E-Mail, Nachricht, Betreff (Dropdown)
- Honeypot + CSRF-Schutz
- Datenschutz-Checkbox (DSGVO-Pflicht)

---

## 4. Technische Architektur

### GRAV CMS Konfiguration

**GRAV-Version:** Stabil (1.7.x)

**Benötigte Plugins:**
| Plugin | Zweck |
|--------|-------|
| `admin` | Admin-Panel zur Inhaltspflege |
| `login` | Admin-Authentifizierung |
| `form` | Kontaktformular |
| `email` | E-Mail-Versand |
| `sitemap` | XML-Sitemap (SEO) |
| `seo` | Meta-Tags, OpenGraph |
| `feed` | RSS/Atom Feed für Blog |
| `pagination` | Blog-Paginierung |
| `breadcrumbs` | Breadcrumb-Navigation |
| `langswitcher` | Sprachumschalter DE/EN |
| `shortcode-core` | Shortcodes in Markdown |
| **`kalkimo-embed`** | Custom Plugin: Lädt Kalkimo Vue-App Bundles |

### Multi-Language Setup

```yaml
# system.yaml (GRAV Konfiguration)
languages:
  supported:
    - de
    - en
  default_lang: de
  include_default_lang: true   # /de/... auch für Standardsprache
  translations: true
  session_store_active: false
  http_accept_language: true    # Browser-Sprache erkennen
```

**Routing:**
- DE: `https://kalkimo.de/de/rechner`
- EN: `https://kalkimo.de/en/calculator`

### Kalkimo-Embed Plugin (Custom)

```
user/plugins/kalkimo-embed/
├── kalkimo-embed.php          # Plugin-Hauptdatei
├── kalkimo-embed.yaml         # Plugin-Konfiguration
├── blueprints.yaml            # Admin-UI Blueprint
├── assets/
│   ├── km-planner.embed.js    # Kompiliertes Vue-App Bundle (IIFE)
│   ├── km-planner.embed.css   # App-Styles
│   └── translations/
│       ├── de.json
│       └── en.json
└── templates/
    └── partials/
        └── kalkimo-embed.html.twig
```

**Funktionsweise:**
1. Markdown-Seite enthält `<km-planner lang="de"></km-planner>`
2. Plugin erkennt den Tag und injiziert JS/CSS-Assets
3. Vue-App mountet sich auf dem Custom Element
4. Sprache wird aus GRAV-Kontext an die App übergeben

### Theme-Struktur

```
user/themes/kalkimo/
├── kalkimo.php
├── kalkimo.yaml
├── blueprints.yaml
├── templates/
│   ├── default.html.twig
│   ├── blog.html.twig
│   ├── item.html.twig          # Blog-Einzelartikel
│   ├── form.html.twig          # Kontaktformular
│   ├── modular.html.twig
│   ├── error.html.twig
│   ├── partials/
│   │   ├── base.html.twig      # HTML-Grundgerüst
│   │   ├── header.html.twig    # Header + Mobile Nav
│   │   ├── footer.html.twig
│   │   ├── navigation.html.twig
│   │   ├── langswitcher.html.twig
│   │   ├── hero.html.twig
│   │   ├── breadcrumbs.html.twig
│   │   └── metadata.html.twig  # SEO Meta-Tags
│   └── modular/
│       ├── hero.html.twig
│       ├── features.html.twig
│       ├── cta.html.twig
│       ├── testimonials.html.twig
│       └── blog-teaser.html.twig
├── css/
│   ├── theme.css               # Haupt-Stylesheet (mobile-first)
│   ├── variables.css            # CSS Custom Properties (--km-*)
│   └── components/
│       ├── header.css
│       ├── footer.css
│       ├── hero.css
│       ├── cards.css
│       ├── forms.css
│       └── utilities.css
├── js/
│   ├── main.js                 # Hamburger-Menü, Scroll-Effekte
│   └── cookie-consent.js
├── images/
│   ├── logo.svg
│   ├── logo-icon.svg
│   ├── favicon.ico
│   └── og-default.jpg
├── fonts/                       # Inter (self-hosted für DSGVO)
│   ├── inter-v13-latin-regular.woff2
│   ├── inter-v13-latin-500.woff2
│   ├── inter-v13-latin-600.woff2
│   └── inter-v13-latin-700.woff2
└── CHANGELOG.md
```

---

## 5. Mobile-First Responsive Strategie

### Navigation

**Mobile (< 1024px):**
- Fixed Header: Logo (links), Sprachschalter (Mitte/rechts), Hamburger (rechts)
- Off-Canvas Menü (Slide-in von rechts)
- Menüpunkte vertikal, volle Breite
- Sprachschalter auch im Off-Canvas Menü

**Desktop (>= 1024px):**
- Horizontale Navigationsleiste
- Logo links, Nav-Links mittig, CTA-Button + Sprachschalter rechts

### Typografie-Skalierung

```css
/* Mobile-Basis */
:root {
  --km-h1: 28px;
  --km-h2: 24px;
  --km-h3: 20px;
  --km-body: 16px;
}

/* Tablet (>= 768px) */
@media (min-width: 768px) {
  :root {
    --km-h1: 36px;
    --km-h2: 28px;
    --km-h3: 22px;
  }
}

/* Desktop (>= 1024px) */
@media (min-width: 1024px) {
  :root {
    --km-h1: 48px;
    --km-h2: 30px;
    --km-h3: 24px;
  }
}
```

### Touch-Optimierung

- Minimale Touch-Target-Größe: 44x44px (Apple HIG)
- Buttons: Mindestens `padding: 12px 24px`
- Formulare: Input-Höhe mindestens 48px
- Ausreichend Abstand zwischen interaktiven Elementen

---

## 6. SEO-Strategie

### Technisches SEO

- Saubere URL-Struktur (GRAV-native Slugs)
- XML-Sitemap (auto-generiert via Plugin)
- `robots.txt` konfiguriert
- Strukturierte Daten (JSON-LD) für:
  - Organization
  - SoftwareApplication
  - FAQ
  - BlogPosting
- `hreflang`-Tags für DE/EN
- Canonical URLs
- OpenGraph + Twitter Cards

### Content-SEO (Ziel-Keywords)

**DE:**
- Immobilien Investitionsrechner
- Immobilien Rendite berechnen
- AfA Immobilien Rechner
- Cashflow Immobilie berechnen
- Immobilie als Kapitalanlage Rechner

**EN:**
- Real estate investment calculator
- Property ROI calculator
- Rental property cashflow calculator
- Real estate tax calculator Germany

### Performance

- Lighthouse Score-Ziel: > 90 (Mobile)
- Fonts self-hosted (kein Google Fonts – DSGVO)
- Bilder: WebP/AVIF mit Fallback, lazy loading
- CSS: Kritischer Pfad inline, Rest deferred
- JS: Nur bei Bedarf laden (Calculator-Bundle nur auf Rechner-Seite)

---

## 7. DSGVO / Datenschutz

### Anforderungen

| Maßnahme | Umsetzung |
|----------|-----------|
| **Cookie-Consent** | Cookie-Banner vor Tracking/Analytics |
| **Datenschutzerklärung** | Eigene Seite, DE + EN |
| **Impressum** | Pflichtangaben nach §5 TMG |
| **Fonts self-hosted** | Kein Google Fonts CDN (LG München Urteil) |
| **Kontaktformular** | Datenschutz-Checkbox + Hinweis |
| **Analytics** | Datenschutzkonformes Tool (z.B. Plausible, Umami, Matomo) |
| **Verschlüsselung** | TLS/HTTPS erzwungen |
| **Auftragsverarbeitung** | AVV mit Hoster/Dienstleistern |

### Empfehlung: Analytics

**Plausible Analytics** oder **Umami** – selbst-gehostet, kein Cookie-Banner nötig (kein Tracking), DSGVO-konform.

---

## 8. Hosting & Deployment

### Optionen

| Option | Vorteile | Nachteile |
|--------|----------|-----------|
| **Shared Hosting + GRAV** | Günstig, einfach | Begrenzte Performance |
| **VPS (Hetzner/Netcup)** | Volle Kontrolle, gut für DE-Hosting | Mehr Wartung |
| **Docker (empfohlen)** | Reproduzierbar, CI/CD-fähig | Initialer Aufwand |

### Empfohlene Konfiguration

```yaml
# docker-compose.grav.yml (Ergänzung zu bestehender docker-compose.dev.yml)
services:
  grav:
    image: linuxserver/grav:latest
    ports:
      - "8080:80"
    volumes:
      - ./cms/grav/user:/config/www/user
    environment:
      - PUID=1000
      - PGID=1000
    depends_on:
      - backend
    networks:
      - kalkimo-network
```

### Deployment-Workflow

1. Content-Änderungen in `cms/grav/user/pages/` committen
2. Theme-Änderungen in `cms/grav/user/themes/kalkimo/` committen
3. Embed-Bundle bauen: `npm run build:embed` im Frontend-Projekt
4. Bundle nach `cms/grav/user/plugins/kalkimo-embed/assets/` kopieren
5. Deployment via Git Push → CI/CD → Docker Rebuild

---

## 9. Umsetzungs-Reihenfolge

### Phase 1: Grundgerüst
- [ ] GRAV CMS Installation (Docker-Setup)
- [ ] Theme-Grundstruktur (kalkimo-Theme) mit base-Template
- [ ] CSS-Variablen-System (--km-*) basierend auf Style Guide
- [ ] Mobile-first Header + Navigation (inkl. Hamburger-Menü)
- [ ] Footer-Template
- [ ] Sprachumschaltung DE/EN konfigurieren
- [ ] Basis-Seiten anlegen (Home, Impressum, Datenschutz)

### Phase 2: Inhaltsseiten
- [ ] Startseite (Hero, Features, CTA-Module)
- [ ] Funktionen-Seite mit Feature-Descriptions
- [ ] Ueber-uns-Seite
- [ ] Kontaktseite mit GRAV Forms
- [ ] Preise-Seite (Platzhalter-Inhalte)

### Phase 3: Rechner-Integration
- [ ] Custom Plugin `kalkimo-embed` erstellen
- [ ] Vue-App Embed-Bundle (IIFE) Build-Pipeline
- [ ] Rechner-Seite mit eingebetteter App
- [ ] Sprach-Synchronisation GRAV ↔ Vue-App
- [ ] API-Anbindung (Backend-URL-Konfiguration)

### Phase 4: Blog / Wissen
- [ ] Blog-Template (Listing + Einzelartikel)
- [ ] Erste 3–5 Artikel schreiben (DE + EN)
- [ ] Kategorie-System
- [ ] RSS-Feed

### Phase 5: SEO & Launch
- [ ] Meta-Tags / OpenGraph für alle Seiten
- [ ] Strukturierte Daten (JSON-LD)
- [ ] XML-Sitemap verifizieren
- [ ] Lighthouse-Audit + Performance-Optimierung
- [ ] Cookie-Consent implementieren
- [ ] Analytics einrichten
- [ ] Produktionsdeployment

---

## 10. Offene Entscheidungen

| Thema | Optionen | Status |
|-------|----------|--------|
| Domain | kalkimo.de / kalkimo.app / kalkimo.io | Offen |
| Logo / Branding | Eigenentwicklung vs. Designer | Offen |
| Hosting-Provider | Hetzner / Netcup / andere | Offen |
| Analytics-Tool | Plausible / Umami / Matomo | Offen |
| Cookie-Consent-Lösung | Eigenbau / Klaro / Osano | Offen |
| Preismodell | Free+Premium / nur Premium / Freemium | Offen |
| Content-Erstellung | Eigenarbeit / Texter / KI-assistiert | Offen |
