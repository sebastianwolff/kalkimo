# Kalkimo GRAV Theme – Design-Spezifikation

Dieses Dokument beschreibt verbindlich das visuelle Erscheinungsbild des Kalkimo GRAV-Themes. Es basiert auf den Layout-Screens (Startseite, Content-Page, News & Infos) und den vorhandenen Assets (Logo, Maskottchen-Illustrationen). Jede GRAV-Template- oder CSS-Arbeit muss sich an diese Spezifikation halten.

---

## 1. Farbpalette

Die Kalkimo-Palette leitet sich direkt aus dem Logo ab: ein dunkles Navy als Primärfarbe und ein mittleres Teal als Akzentfarbe. Das Gesamtbild ist hell, freundlich und professionell.

### Primärfarben

| Name | Hex | Verwendung |
|------|-----|------------|
| **Navy (Primary Dark)** | `#1E3A5F` | Logo "kalk", Überschriften, Fließtext, Outlines, Footer-Hintergrund |
| **Navy Medium** | `#2C4A6E` | Hover-Zustände auf dunklen Flächen, sekundäre Akzente |
| **Navy Light** | `#3D5A80` | Dezente Hintergründe, Rahmen |

### Akzentfarben

| Name | Hex | Verwendung |
|------|-----|------------|
| **Teal (Accent)** | `#2BAAAD` | Logo "imo", CTA-Buttons, Links, Icons, Maskottchen-Krawatte |
| **Teal Hover** | `#239192` | Button-Hover, Link-Hover |
| **Teal Light** | `#E0F5F5` | Highlight-Hintergründe, Tag-Badges, Feature-Card-Icon-BG |

### Neutrale Farben

| Name | Hex | Verwendung |
|------|-----|------------|
| **White** | `#FFFFFF` | Header, Content-Bereiche, Cards |
| **Gray 50** | `#F7F9FB` | Hero-Hintergrund, alternierende Sektionen |
| **Gray 100** | `#EEF1F5` | Card-Hintergründe (inaktiv), Divider |
| **Gray 200** | `#D8DEE6` | Rahmen, Trennlinien |
| **Gray 400** | `#8896A6` | Sekundärer Text, Platzhalter, Icons (inaktiv) |
| **Gray 600** | `#4A5568` | Body-Text (Fließtext) |
| **Gray 900** | `#1A202C` | Primärer Text (alternativ zu Navy) |

### Statusfarben

| Name | Hex | Verwendung |
|------|-----|------------|
| **Success** | `#16A34A` | Erfolg, Häkchen |
| **Warning** | `#D97706` | Warnungen |
| **Error** | `#DC2626` | Fehler |
| **Info** | `#3B82F6` | Hinweise |

### CSS Custom Properties

```css
:root {
  /* Primary – Navy */
  --km-navy: #1E3A5F;
  --km-navy-medium: #2C4A6E;
  --km-navy-light: #3D5A80;

  /* Accent – Teal */
  --km-teal: #2BAAAD;
  --km-teal-hover: #239192;
  --km-teal-light: #E0F5F5;

  /* Neutrals */
  --km-white: #FFFFFF;
  --km-gray-50: #F7F9FB;
  --km-gray-100: #EEF1F5;
  --km-gray-200: #D8DEE6;
  --km-gray-400: #8896A6;
  --km-gray-600: #4A5568;
  --km-gray-900: #1A202C;

  /* Status */
  --km-success: #16A34A;
  --km-warning: #D97706;
  --km-error: #DC2626;
  --km-info: #3B82F6;
}
```

---

## 2. Typografie

### Schriftart

```css
--km-font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
```

Inter wird self-hosted (DSGVO). Fallback auf System-Fonts. Benötigte Gewichte: 400 (Regular), 500 (Medium), 600 (Semibold), 700 (Bold).

### Schriftgrößen (Mobile-First)

Die Basis-Größen gelten für Mobile. Per `min-width`-Media-Query werden sie für größere Screens skaliert.

| Element | Mobile | ab 768px | ab 1024px | Gewicht | Zeilenhöhe |
|---------|--------|----------|-----------|---------|------------|
| **Hero H1** | 28px | 36px | 48px | 700 | 1.15 |
| **H2 (Sektions-Titel)** | 24px | 28px | 32px | 700 | 1.25 |
| **H3 (Card-Titel)** | 18px | 20px | 22px | 600 | 1.3 |
| **H4 (Sub-Heading)** | 16px | 18px | 18px | 600 | 1.4 |
| **Body** | 16px | 16px | 16px | 400 | 1.65 |
| **Body Large (Lead)** | 17px | 18px | 20px | 400 | 1.6 |
| **Small / Meta** | 14px | 14px | 14px | 400 | 1.5 |
| **Caption / Badge** | 12px | 12px | 13px | 500 | 1.4 |

### CSS Custom Properties

```css
:root {
  --km-font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;

  /* Mobile-Basis */
  --km-h1: 28px;
  --km-h2: 24px;
  --km-h3: 18px;
  --km-h4: 16px;
  --km-body: 16px;
  --km-body-lg: 17px;
  --km-small: 14px;
  --km-caption: 12px;
}

@media (min-width: 768px) {
  :root {
    --km-h1: 36px;
    --km-h2: 28px;
    --km-h3: 20px;
    --km-h4: 18px;
    --km-body-lg: 18px;
  }
}

@media (min-width: 1024px) {
  :root {
    --km-h1: 48px;
    --km-h2: 32px;
    --km-h3: 22px;
    --km-body-lg: 20px;
    --km-caption: 13px;
  }
}
```

### Text-Farb-Zuordnung

| Kontext | Farbe |
|---------|-------|
| Überschriften | `--km-navy` |
| Fließtext | `--km-gray-600` |
| Sekundärer Text / Meta | `--km-gray-400` |
| Links | `--km-teal` |
| Links Hover | `--km-teal-hover` |
| Text auf dunklem Hintergrund (Footer, Hero optional) | `--km-white` |
| Muted Text auf dunklem Hintergrund | `--km-gray-400` |

---

## 3. Logo

### Aufbau

Das Logo besteht aus:
1. **Icon-Box** – Abgerundetes Quadrat mit Navy-Hintergrund, weiße Buchstaben "ki"
2. **Wortmarke** – "kalk" in Navy, "imo" in Teal
3. **Chartgrafik** (optional, nur auf großen Flächen) – Balkendiagramm mit Trendpfeil, Navy/Teal

### Varianten

| Variante | Verwendung |
|----------|------------|
| **Volllogo** (Icon + Wortmarke) | Header (Desktop), Footer |
| **Icon-Only** (ki-Box) | Favicon, mobile Header wenn Platz knapp, App-Icon |
| **Volllogo + Chartgrafik** | Hero-Bereich, About-Seite, große Flächen |

### Datei-Referenz

- Volllogo: `docs/layout/logo.png` (als SVG für Produktion vektorisieren)
- Hauptmaskottchen: `docs/layout/kalkimo.png`

### Mindestgröße & Freiräume

- Mindesthöhe Icon-Box: 28px
- Mindesthöhe Volllogo: 32px
- Freiraum um Logo: mindestens halbe Icon-Box-Breite auf allen Seiten

---

## 4. Maskottchen-System ("Kalkimo-Männlein")

### Beschreibung

Strichmännchen im Business-Casual-Stil: weißer Körper mit dunklen Navy-Outlines, teal-farbene Krawatte, graue/blaue Hose. Freundlich lächelnd, große runde Augen.

### Verfügbare Posen (Assets)

| Asset | Datei | Beschriebene Pose | Empfohlener Einsatz |
|-------|-------|-------------------|---------------------|
| **Hauptfigur** | `kalkimo.png` | Hält Haus (links) + Clipboard mit Chart (rechts) | Hero Startseite, Über-uns |
| **Set 1** | `maennlein1.png` | 12 Posen: Winkend, Laptop, Idee, Lupe+Frage, Chart, Kalender, Smartphone, Dokumente, Zählen, Checklist, Laufend, Glühbirne | Content-Seiten, Feature-Bereiche |
| **Set 2** | `maennlein2.png` | 12 Posen: Winkend+Handy, Laptop+Mail, Fragend, Zeigend, Check+Handy, Zeigend, Daumen hoch, Check, Telefonierend, Daumen hoch, Laufend+Aktentasche, Glühbirne | Blog, News, Kontakt |
| **Set 2a** | `maennlein2a.png` | 12 Posen: ähnlich Set 2 mit dunklem Hintergrund, inkl. Präsentation, Sicherheitsschild, Chart-Board | Features, Security, Präsentationen |
| **Set 3** | `maennlein3.png` | 9 Posen: kompakteres Set, Winkend, Laptop, Fragend, Check, Zeigend, Daumen hoch, Telefonierend, Laufend, Glühbirne | Kleinere Illustrationen, Sidebar |

### Einsatzregeln

1. **Maximal 1 Maskottchen pro sichtbarem Viewport** – nicht überlasten
2. **Konsistente Pose wählen**: Pose muss zum Seiteninhalt passen (z.B. Fragezeichen-Pose für FAQ, Chart-Pose für Rechner)
3. **Freistellen**: Maskottchen immer auf transparentem Hintergrund, nie in Boxen eingesperrt
4. **Größe**: Auf Mobile max. 150px Höhe, auf Desktop max. 280px Höhe
5. **Position**: Meist rechts vom Text oder zentriert unter einem Abschnitt
6. **Kein Text im Bild**: Begleittext immer als HTML, nie als Bild-Text

### Empfohlene Zuordnung pro Seitentyp

| Seite | Pose | Begründung |
|-------|------|------------|
| Startseite Hero | Hauptfigur (Haus + Clipboard) | Kernbotschaft: Immobilien + Kalkulation |
| Features | Präsentation / Chart-Board | Zeigt Funktionalität |
| Rechner | Daumen hoch / Check | Positive Bestätigung |
| Blog-Listing | Fragend / Glühbirne | Wissen & Neugier |
| Blog-Artikel | Haus + Clipboard oder Laptop | Inhaltlich passend |
| Kontakt | Winkend / Telefonierend | Einladend |
| Preise | Zeigend / Daumen hoch | Empfehlung |
| FAQ / News | Fragend | Fragen & Antworten |

---

## 5. Seitenlayouts

### 5.1 Header (alle Seiten)

```
┌─────────────────────────────────────────────────────────┐
│  [ki] kalkimo    Features  Immobilienrechner  Preise  [Login]  │
└─────────────────────────────────────────────────────────┘
```

**Spezifikation:**

| Eigenschaft | Wert |
|-------------|------|
| Position | `fixed`, top, volle Breite |
| Höhe | 64px (Mobile), 72px (Desktop) |
| Hintergrund | `--km-white` |
| Unterkante | 1px solid `--km-gray-200` |
| Z-Index | 1000 |
| Max-Width Inhalt | 1200px, zentriert |
| Padding | 0 16px (Mobile), 0 24px (Desktop) |

**Elemente:**

| Element | Position | Styling |
|---------|----------|---------|
| Logo (Volllogo) | Links | Höhe 32px (Mobile), 36px (Desktop) |
| Nav-Links | Mitte/Rechts (Desktop), Hidden (Mobile) | `--km-gray-600`, font-weight 500, 16px |
| Nav-Link Hover | – | `--km-navy`, kein Underline |
| Nav-Link Aktiv | – | `--km-teal`, font-weight 600 |
| Login-Button | Rechts | Teal-Outline-Button (siehe Buttons) |
| Hamburger-Icon | Rechts (nur Mobile) | `--km-navy`, 24x24px |
| Sprachschalter | Vor Login-Button | Kleiner Text-Toggle "DE \| EN", `--km-gray-400`, aktiv: `--km-navy` |

**Mobile Off-Canvas Menü:**
- Slide-in von rechts, volle Höhe
- Weißer Hintergrund, Overlay dunkel (rgba(30,58,95,0.5))
- Menüpunkte vertikal, 48px Zeilenhöhe, volle Breite
- Sprachschalter + Login-Button unten im Menü

### 5.2 Startseite

#### Hero-Sektion

```
┌───────────────────────────────────────────────────┐
│  bg: --km-gray-50                                 │
│                                                   │
│  ┌──────────────────┐  ┌──────────────────────┐   │
│  │ Immobilien       │  │                      │   │
│  │ kalkulieren      │  │   [Maskottchen]      │   │
│  │ leicht gemacht   │  │   + App-Screenshot   │   │
│  │                  │  │   (halbtransparent)   │   │
│  │ Subtext...       │  │                      │   │
│  │                  │  │                      │   │
│  │ [CTA Button →]   │  │                      │   │
│  └──────────────────┘  └──────────────────────┘   │
└───────────────────────────────────────────────────┘
```

| Eigenschaft | Wert |
|-------------|------|
| Hintergrund | `--km-gray-50` |
| Padding | 80px 16px (Mobile), 100px 24px (Desktop) |
| Layout | 1 Spalte (Mobile), 2 Spalten 55/45 (Desktop ab 1024px) |
| Headline | `--km-h1`, `--km-navy`, max-width 560px |
| Subtext | `--km-body-lg`, `--km-gray-600`, max-width 480px |
| CTA-Button | Teal Primary Button (siehe Buttons), mit Pfeil → |
| Rechte Seite | Maskottchen-Illustration (max 320px Höhe), dahinter leicht transparenter App-Screenshot |

**Mobile:**
- Alles einpaltig, zentriert
- Maskottchen unter dem Text, kleiner (max 200px)
- CTA-Button volle Breite

#### Feature-Streifen (unterhalb Hero)

```
┌───────────────────────────────────────────────────┐
│  [Icon] Cashflow    [Icon] Steuer &    [Icon] Zins &    │
│         & Rendite          Nebenkosten        Finanzierung │
└───────────────────────────────────────────────────┘
```

| Eigenschaft | Wert |
|-------------|------|
| Hintergrund | `--km-white` |
| Layout | 1 Spalte (Mobile), 3 Spalten (Desktop) |
| Card-Stil | Kein Rahmen, Icon oben/links, Titel darunter |
| Icon | 40x40px, `--km-teal` oder Teal-Light-Hintergrund-Kreis |
| Titel | `--km-h4`, `--km-navy` |
| Beschreibung (optional) | `--km-small`, `--km-gray-600` |
| Gap | 24px (Mobile), 32px (Desktop) |
| Padding | 48px 16px (Mobile), 64px 24px (Desktop) |

### 5.3 Content-Page (Blog-Artikel)

```
┌───────────────────────────────────────────────────┐
│  [Header]                                         │
├───────────────────────────────────────────────────┤
│                                                   │
│  10 Dinge, die du bei Immobilien-                 │
│  Kalkulationen beachten solltest                  │
│                                              [4 min] │
│                                                   │
│  1. Kaufpreis & Erwerbsnebenkosten               │
│     realistisch einschätzen                       │
│                                                   │
│  Fließtext lorem ipsum...          [Maskottchen]  │
│  Fließtext lorem ipsum...                         │
│                                                   │
│  [Häuser-Illustration]                            │
│                                                   │
├───────────────────────────────────────────────────┤
│  [Footer]                                         │
└───────────────────────────────────────────────────┘
```

| Eigenschaft | Wert |
|-------------|------|
| Max-Width Content | 760px, zentriert |
| Titel | `--km-h1` (kleiner: 24px Mobile, 36px Desktop), `--km-navy` |
| Lesezeit-Badge | `--km-caption`, `--km-gray-400`, optional mit Uhr-Icon |
| Zwischenüberschriften | `--km-h3`, `--km-navy`, margin-top 40px |
| Nummerierung | Bold, `--km-teal` als Farbe für die Nummer |
| Fließtext | `--km-body`, `--km-gray-600`, Zeilenhöhe 1.65 |
| Maskottchen | Float right (Desktop), max 200px, unter dem Titel-Bereich |
| Maskottchen Mobile | Zentriert zwischen Abschnitten, max 150px |
| Illustrations | Volle Breite, zentriert, max 480px |

### 5.4 News & Ratgeber (Blog-Listing)

```
┌───────────────────────────────────────────────────┐
│  [Header]                                         │
├───────────────────────────────────────────────────┤
│                                                   │
│  Neuigkeiten & Ratgeber                           │
│  Antworten auf alle deine Fragen                  │
│                                                   │
│  ┌─────────────────────────────────┐ [🔍]         │
│  │ z.B. Anfangszahlung, Steuererklärung... │      │
│  └─────────────────────────────────┘              │
│                                                   │
│  ┌─ Top-Themen ──────────────────────────┐        │
│  │  Immobilien kosten Berechnen →        │        │
│  │  Garant sprechen →                    │        │
│  │  Tipps für mögliche Optionen →        │        │
│  │  So kostenspar die Ersparn Optionen → │        │
│  │  Monatlichen Cashflow berechnen →     │        │
│  └───────────────────────────────────────┘        │
│                                                   │
│              [Maskottchen]                         │
│                                                   │
│  Top-Themen                                       │
│  ┌───────────────────────────────────┐            │
│  │ Immobilienrendite berechnen     > │            │
│  ├───────────────────────────────────┤            │
│  │ Wie berechne ich Nebenkosten?   > │            │
│  ├───────────────────────────────────┤            │
│  │ Tipps für Rendite Steigerung?   > │            │
│  ├───────────────────────────────────┤            │
│  │ So reduzieren Sie die Kosten    > │            │
│  ├───────────────────────────────────┤            │
│  │ Monatlichen Cashflow berechnen? > │            │
│  └───────────────────────────────────┘            │
│                                                   │
├───────────────────────────────────────────────────┤
│  [Footer]                                         │
└───────────────────────────────────────────────────┘
```

**Suchleiste:**

| Eigenschaft | Wert |
|-------------|------|
| Breite | 100% (Mobile), max 600px (Desktop) |
| Höhe | 48px |
| Rahmen | 1px solid `--km-gray-200`, radius 8px |
| Placeholder | `--km-gray-400` |
| Such-Button | Quadratisch, `--km-teal` Hintergrund, weißes Lupen-Icon, radius 0 8px 8px 0 |

**Top-Themen Box (Link-Liste):**

| Eigenschaft | Wert |
|-------------|------|
| Hintergrund | `--km-gray-50` |
| Rahmen | 1px solid `--km-gray-200`, radius 12px |
| Padding | 24px |
| Überschrift | `--km-h3`, `--km-navy`, innerhalb der Box |
| Links | `--km-body`, `--km-teal`, mit Pfeil → am Ende |
| Link-Abstand | 12px vertikal |

**Accordion-Liste (FAQ-Stil):**

| Eigenschaft | Wert |
|-------------|------|
| Hintergrund | `--km-white` |
| Jeder Eintrag | Padding 16px 20px, Rahmen unten 1px solid `--km-gray-200` |
| Text | `--km-body`, `--km-navy` |
| Chevron | Rechts, `--km-gray-400`, 20x20px, Pfeil nach rechts (→ oder >) |
| Hover | Hintergrund `--km-gray-50` |
| Erster Eintrag | Border-radius oben 12px |
| Letzter Eintrag | Border-radius unten 12px, kein Border unten |

---

## 6. Komponenten

### 6.1 Buttons

#### Primary Button (CTA – Teal)

```css
.btn-primary {
  background-color: var(--km-teal);
  color: var(--km-white);
  border: 2px solid var(--km-teal);
  border-radius: 8px;
  padding: 12px 28px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s, transform 0.2s;
}
.btn-primary:hover {
  background-color: var(--km-teal-hover);
  border-color: var(--km-teal-hover);
  transform: translateY(-1px);
}
```

Verwendung: CTA "Jetzt kostenlos kalkulieren →", Formular-Submit, Suchbutton

#### Outline Button (Login, Sekundär)

```css
.btn-outline {
  background-color: transparent;
  color: var(--km-teal);
  border: 2px solid var(--km-teal);
  border-radius: 8px;
  padding: 8px 20px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-outline:hover {
  background-color: var(--km-teal);
  color: var(--km-white);
}
```

Verwendung: Login-Button im Header, sekundäre Aktionen

#### Navy Button (Dark)

```css
.btn-dark {
  background-color: var(--km-navy);
  color: var(--km-white);
  border: 2px solid var(--km-navy);
  border-radius: 8px;
  padding: 12px 28px;
  font-size: 16px;
  font-weight: 600;
}
.btn-dark:hover {
  background-color: var(--km-navy-medium);
}
```

Verwendung: Alternative CTA auf hellen Flächen

#### Arrow-Link

```css
.link-arrow {
  color: var(--km-teal);
  font-weight: 600;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.link-arrow::after {
  content: '→';
  transition: transform 0.2s;
}
.link-arrow:hover::after {
  transform: translateX(4px);
}
.link-arrow:hover {
  color: var(--km-teal-hover);
}
```

Verwendung: "Mehr erfahren →", Top-Themen-Links

### 6.2 Cards

#### Feature Card (Startseite)

```css
.feature-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 24px 16px;
  gap: 12px;
}
.feature-card-icon {
  width: 48px;
  height: 48px;
  background: var(--km-teal-light);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--km-teal);
}
.feature-card-title {
  font-size: var(--km-h4);
  font-weight: 600;
  color: var(--km-navy);
}
```

#### Topic Card (Accordion-Eintrag)

```css
.topic-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid var(--km-gray-200);
  cursor: pointer;
  transition: background-color 0.15s;
}
.topic-item:hover {
  background-color: var(--km-gray-50);
}
.topic-item-title {
  font-size: var(--km-body);
  color: var(--km-navy);
  font-weight: 500;
}
.topic-item-chevron {
  color: var(--km-gray-400);
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}
```

### 6.3 Suchleiste

```css
.search-bar {
  display: flex;
  max-width: 600px;
  width: 100%;
}
.search-input {
  flex: 1;
  height: 48px;
  padding: 0 16px;
  font-size: 16px;
  border: 1px solid var(--km-gray-200);
  border-right: none;
  border-radius: 8px 0 0 8px;
  color: var(--km-navy);
  background: var(--km-white);
}
.search-input::placeholder {
  color: var(--km-gray-400);
}
.search-input:focus {
  outline: none;
  border-color: var(--km-teal);
  box-shadow: 0 0 0 3px rgba(43, 170, 173, 0.1);
}
.search-button {
  width: 48px;
  height: 48px;
  background: var(--km-teal);
  border: 1px solid var(--km-teal);
  border-radius: 0 8px 8px 0;
  color: var(--km-white);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
.search-button:hover {
  background: var(--km-teal-hover);
}
```

### 6.4 Lesezeit-Badge

```css
.reading-time {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: var(--km-caption);
  color: var(--km-gray-400);
  font-weight: 500;
}
```

---

## 7. Footer

```
┌───────────────────────────────────────────────────┐
│  bg: --km-navy                                    │
│                                                   │
│  [ki] kalkimo                                     │
│                                                   │
│  Features   Immobilienrechner   Preise   Blog   FAQs │
│                                                   │
│  ─────────────────────────────────────            │
│  © 2025 Kalkimo  ·  Impressum  ·  Datenschutz    │
└───────────────────────────────────────────────────┘
```

| Eigenschaft | Wert |
|-------------|------|
| Hintergrund | `--km-navy` |
| Text | `--km-white` (Logo, Hauptlinks), `--km-gray-400` (Copyright, Legal) |
| Padding | 48px 16px (Mobile), 64px 24px (Desktop) |
| Logo | Weiße Variante oder invertiert, max-height 32px |
| Nav-Links | Einzeilig (Desktop), vertikal (Mobile), font-weight 500 |
| Link-Hover | `--km-teal-light` |
| Trennlinie | 1px solid `--km-navy-light` |
| Copyright-Zeile | `--km-small`, `--km-gray-400` |
| Legal-Links | `--km-small`, `--km-gray-400`, hover: `--km-white` |

**Mobile:** Links vertikal, zentriert, größerer Abstand.

---

## 8. Spacing & Layout

### Container

```css
.container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 16px;
}
@media (min-width: 768px) {
  .container { padding: 0 24px; }
}
@media (min-width: 1280px) {
  .container { padding: 0 32px; }
}
```

### Spacing-System

```css
:root {
  --km-space-xs: 4px;
  --km-space-sm: 8px;
  --km-space-md: 16px;
  --km-space-lg: 24px;
  --km-space-xl: 32px;
  --km-space-2xl: 48px;
  --km-space-3xl: 64px;
  --km-space-4xl: 96px;
}
```

### Sektions-Abstände

| Sektion | Padding Mobile | Padding Desktop |
|---------|----------------|-----------------|
| Hero | 80px 0 48px | 100px 0 64px |
| Standard-Sektion | 48px 0 | 64px 0 |
| Große Sektion | 64px 0 | 96px 0 |

### Border Radius

```css
:root {
  --km-radius-sm: 4px;
  --km-radius-md: 8px;
  --km-radius-lg: 12px;
  --km-radius-xl: 16px;
  --km-radius-full: 9999px;
}
```

### Shadows

```css
:root {
  --km-shadow-sm: 0 1px 3px rgba(30, 58, 95, 0.06);
  --km-shadow-md: 0 4px 12px rgba(30, 58, 95, 0.08);
  --km-shadow-lg: 0 12px 24px rgba(30, 58, 95, 0.1);
}
```

---

## 9. Responsive Breakpoints

```css
/* Mobile-First: Basis-Styles ohne Media Query */

@media (min-width: 640px)  { /* sm – Kleine Tablets */ }
@media (min-width: 768px)  { /* md – Tablets */ }
@media (min-width: 1024px) { /* lg – Laptops */ }
@media (min-width: 1280px) { /* xl – Desktops */ }
```

### Layout-Entscheidungen pro Breakpoint

| Komponente | < 640px | 640–767px | 768–1023px | >= 1024px |
|------------|---------|-----------|------------|-----------|
| Header-Nav | Hamburger | Hamburger | Hamburger | Inline |
| Hero | 1 Spalte | 1 Spalte | 1 Spalte | 2 Spalten |
| Feature-Cards | 1 Spalte | 2 Spalten | 3 Spalten | 3 Spalten |
| Blog-Artikel | Vollbreite | Vollbreite | Zentriert 720px | Zentriert 760px |
| Footer-Links | Vertikal | Vertikal | Horizontal | Horizontal |
| Maskottchen (Hero) | 150px, unter Text | 180px | 220px | 280px, neben Text |

---

## 10. Touch & Accessibility

### Touch Targets

- Minimale Tappable Area: **44x44px** (Apple HIG)
- Buttons: min-height 44px, padding mind. 12px vertikal
- Nav-Links: mind. 44px Zeilenhöhe
- Accordion-Einträge: mind. 48px Zeilenhöhe
- Ausreichend Abstand (8px+) zwischen tappbaren Elementen

### Accessibility

- Fokus-Ring: `outline: 2px solid var(--km-teal); outline-offset: 2px;` – nie entfernen, nur restylen
- Farbkontraste: Alle Text/Hintergrund-Kombinationen müssen WCAG AA erfüllen (4.5:1 für Body, 3:1 für Large Text)
- Skip-to-Content Link als erstes Element im DOM
- Alle Bilder/Illustrationen mit `alt`-Text
- Semantisches HTML: `<nav>`, `<main>`, `<article>`, `<aside>`, `<footer>`
- Aria-Labels für Hamburger-Menü, Sprachschalter, Suchleiste

### Farb-Kontrast-Check

| Kombination | Kontrast | Status |
|-------------|----------|--------|
| Navy (#1E3A5F) auf Weiß (#FFF) | ~8.5:1 | OK |
| Teal (#2BAAAD) auf Weiß (#FFF) | ~3.2:1 | Nur für Large Text / Icons |
| Gray-600 (#4A5568) auf Weiß (#FFF) | ~6.4:1 | OK |
| Gray-400 (#8896A6) auf Weiß (#FFF) | ~3.4:1 | Nur für Large Text / Deko |
| Weiß auf Navy (#1E3A5F) | ~8.5:1 | OK |
| Teal-Hover (#239192) auf Weiß (#FFF) | ~3.5:1 | Nur für Large Text / Buttons (min 16px bold) |

**Hinweis:** Teal als alleinige Textfarbe auf weißem Grund ist grenzwertig für WCAG AA bei Normalgröße. Für Fließtext Navy verwenden, Teal nur für Buttons (groß + bold), Icons und dekorative Links mit zusätzlichem Underline/Pfeil als visuellem Indikator.

---

## 11. GRAV Template-Mapping

| GRAV-Template | Seitentyp | Layout |
|---------------|-----------|--------|
| `default.html.twig` | Standard-Inhaltsseite | Content zentriert, optionaler Maskottchen-Float |
| `home.html.twig` | Startseite | Hero + Feature-Streifen + modulare Sektionen |
| `blog.html.twig` | Blog-/News-Listing | Such-Leiste + Top-Themen-Box + Accordion-Liste |
| `item.html.twig` | Blog-Einzelartikel | Content-Seite mit Lesezeit, nummerierte Abschnitte |
| `form.html.twig` | Kontaktformular | Formular zentriert, max-width 600px |
| `modular.html.twig` | Modulare Seite | Zusammengesetzt aus Modular-Partials |
| `error.html.twig` | 404 / Fehler | Zentriert, Maskottchen (fragend), CTA zurück |

### Modular-Templates

| Template | Inhalt |
|----------|--------|
| `modular/hero.html.twig` | Hero-Sektion mit Headline, Subtext, CTA, Illustration |
| `modular/features.html.twig` | Feature-Cards Grid (3 Spalten Desktop) |
| `modular/cta.html.twig` | Call-to-Action Streifen (Navy oder Teal BG) |
| `modular/testimonials.html.twig` | Zitat-Cards horizontal scrollbar |
| `modular/blog-teaser.html.twig` | 2–3 aktuelle Blog-Artikel |
| `modular/faq.html.twig` | Accordion FAQ-Liste |

---

## 12. Asset-Referenz

### Verzeichnis: `docs/layout/`

| Datei | Inhalt | Format | Produktionsformat |
|-------|--------|--------|-------------------|
| `logo.png` | Volllogo (Icon + Wortmarke + Chart) | PNG | SVG (vektorisieren) |
| `kalkimo.png` | Hauptmaskottchen (Haus + Clipboard) | PNG | PNG/SVG, freigestellt |
| `maennlein1.png` | Posen-Set 1 (12 Posen, dunkler BG) | PNG | Einzeln freistellen |
| `maennlein2.png` | Posen-Set 2 (12 Posen, heller BG) | PNG | Einzeln freistellen |
| `maennlein2a.png` | Posen-Set 2a (12 Posen, dunkler BG) | PNG | Einzeln freistellen |
| `maennlein3.png` | Posen-Set 3 (9 Posen, heller BG) | PNG | Einzeln freistellen |

### Produktions-Aufbereitung (TODO)

1. Logo als SVG vektorisieren (Icon-Only + Volllogo + Weiße Variante für Footer)
2. Maskottchen-Posen einzeln freistellen (transparenter Hintergrund)
3. Häuser-Illustration aus Content-Page extrahieren
4. App-Screenshot / Mockup für Hero erstellen
5. Alle Bilder als WebP mit PNG-Fallback bereitstellen
6. Favicon-Set aus ki-Icon generieren (16, 32, 180, 192, 512px)
