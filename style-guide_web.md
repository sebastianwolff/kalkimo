# PV-Calor Web Style Guide

Ein umfassender Design-Leitfaden für die PV-Calor Website basierend auf einer dunklen Slate-Farbpalette.

---

## 1. Farbpalette

### Primärfarben

| Farbe | Hex | RGB | Verwendung |
|-------|-----|-----|------------|
| **Primary Dark** | `#0F172A` | `rgb(15, 23, 42)` | Header, Hero-Bereiche, Footer |
| **Primary** | `#1E293B` | `rgb(30, 41, 59)` | Card-Hintergründe dunkel, Panels |
| **Primary Light** | `#334155` | `rgb(51, 65, 85)` | Hover-Zustände, Rahmen |
| **White** | `#FFFFFF` | `rgb(255, 255, 255)` | Hintergründe, Text auf dunkel |

### Sekundär-/Akzentfarben

| Farbe | Hex | RGB | Verwendung |
|-------|-----|-----|------------|
| **Accent Emerald** | `#10B981` | `rgb(16, 185, 129)` | Highlights, wichtige Werte, CTA |
| **Accent Emerald Dark** | `#059669` | `rgb(5, 150, 105)` | Hover-Zustände für Emerald |
| **Emerald Light** | `#D1FAE5` | `rgb(209, 250, 229)` | Highlight-Box-Hintergründe |
| **Slate 50** | `#F8FAFC` | `rgb(248, 250, 252)` | Helle Hintergründe |
| **Slate 100** | `#F1F5F9` | `rgb(241, 245, 249)` | Card-Hintergründe |
| **Slate 400** | `#94A3B8` | `rgb(148, 163, 184)` | Sekundärer Text, Labels |
| **Slate 300** | `#CBD5E1` | `rgb(203, 213, 225)` | Rahmen, Trennlinien |

### Status- & Akzentfarben

| Farbe | Hex | Verwendung |
|-------|-----|------------|
| **Success Green** | `#16A34A` | Checkmarks, Erfolg |
| **Warning Amber** | `#D97706` | Warnungen |
| **Error Red** | `#DC2626` | Fehlermeldungen |
| **Info Blue** | `#3B82F6` | Informations-Boxen |

### Helle Hintergrundfarben für Status-Boxen

> **Wichtig:** Für Status-Boxen (Erfolg, Warnung, Fehler, Info) sehr helle Hintergrundfarben verwenden, um guten Kontrast zum Text zu gewährleisten. Die Boxen erhalten zusätzlich einen 4px linken Rand in der jeweiligen Statusfarbe.

| Farbe | Hex | RGB | Verwendung |
|-------|-----|-----|------------|
| **Success Background** | `#F0FDF4` | `rgb(240, 253, 244)` | Erfolgs-Boxen, positive Meldungen |
| **Warning Background** | `#FFFBEB` | `rgb(255, 251, 235)` | Warnungen, Hinweise |
| **Error Background** | `#FEF2F2` | `rgb(254, 242, 242)` | Fehlermeldungen |
| **Primary Background** | `#F8FAFC` | `rgb(248, 250, 252)` | Ausgewählte Elemente, Primary-Tint |
| **Info Background** | `#EFF6FF` | `rgb(239, 246, 255)` | Informations-Boxen |

#### CSS-Variablen

```css
:root {
  /* Primärfarben */
  --pv-primary-dark: #0F172A;
  --pv-primary: #1E293B;
  --pv-primary-light: #334155;

  /* Akzentfarben */
  --pv-accent: #10B981;
  --pv-accent-hover: #059669;
  --pv-accent-light: #D1FAE5;

  /* Grautöne (Slate) */
  --pv-slate-50: #F8FAFC;
  --pv-slate-100: #F1F5F9;
  --pv-slate-200: #E2E8F0;
  --pv-slate-300: #CBD5E1;
  --pv-slate-400: #94A3B8;
  --pv-slate-500: #64748B;
  --pv-slate-600: #475569;
  --pv-slate-700: #334155;
  --pv-slate-800: #1E293B;
  --pv-slate-900: #0F172A;

  /* Statusfarben */
  --pv-success: #16A34A;
  --pv-warning: #D97706;
  --pv-error: #DC2626;
  --pv-info: #2563EB;

  /* Helle Hintergrundfarben für Status-Boxen */
  --pv-success-bg: #F0FDF4;
  --pv-warning-bg: #FFFBEB;
  --pv-error-bg: #FEF2F2;
  --pv-primary-bg: #F8FAFC;
  --pv-info-bg: #EFF6FF;
}
```

#### Beispiel: Status-Box

```css
.success-box {
  background: var(--pv-success-bg);
  border-left: 4px solid var(--pv-success);
  padding: var(--pv-spacing-md);
  border-radius: var(--pv-radius-sm);
}
```

---

## 2. Typografie

### Schriftart

```css
--font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
```

Die Schriftart **Inter** ist eine moderne, gut lesbare Sans-Serif. Alternativen:
- **Source Sans Pro** (Google Fonts)
- **Nunito Sans** (Google Fonts)
- **Open Sans** (Google Fonts)

### Schriftgrößen & Hierarchie

| Element | Größe | Gewicht | Zeilenhöhe | Verwendung |
|---------|-------|---------|------------|------------|
| **Hero Headline** | 36-48px | 700 (Bold) | 1.2 | Hauptüberschriften im Hero |
| **H2** | 30px | 700 | 1.3 | Sektionsüberschriften |
| **H3** | 24px | 600 | 1.4 | Unterüberschriften |
| **H4** | 20px | 600 | 1.4 | Card-Titel |
| **Body** | 16px | 400 | 1.6 | Fließtext |
| **Body Large** | 18px | 400 | 1.6 | Lead-Text |
| **Button** | 16px | 600 | normal | Buttons |
| **Small** | 14px | 400 | 1.5 | Meta-Informationen |
| **Caption** | 12px | 400 | 1.5 | Bildunterschriften |

### CSS-Variablen

```css
:root {
  --font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --font-size-base: 16px;
  --font-size-sm: 14px;
  --font-size-lg: 18px;
  --font-size-xl: 20px;
  --font-size-2xl: 24px;
  --font-size-3xl: 30px;
  --font-size-4xl: 36px;
  --font-size-5xl: 48px;
  --max-width: 1280px;
}
```

---

## 3. Buttons

### Primary Button (CTA)

```css
.btn-primary {
  background-color: #0F172A;
  color: #FFFFFF;
  border: 2px solid #0F172A;
  border-radius: 6px;
  padding: 12px 24px;
  font-size: 16px;
  font-weight: 600;
  text-decoration: none;
  transition: background-color 0.2s, border-color 0.2s, transform 0.2s;
  cursor: pointer;
}

.btn-primary:hover {
  background-color: #1E293B;
  border-color: #1E293B;
  transform: translateY(-1px);
}

.btn-primary:active {
  transform: translateY(0);
}
```

### Secondary Button (Accent)

```css
.btn-secondary {
  background-color: #10B981;
  color: #FFFFFF;
  border: 2px solid #10B981;
  border-radius: 6px;
  padding: 12px 24px;
  font-size: 16px;
  font-weight: 600;
  transition: background-color 0.2s;
  cursor: pointer;
}

.btn-secondary:hover {
  background-color: #059669;
  border-color: #059669;
}
```

### Ghost/Outline Button

```css
.btn-outline {
  background-color: transparent;
  color: #0F172A;
  border: 2px solid #0F172A;
  border-radius: 6px;
  padding: 12px 24px;
  font-size: 16px;
  font-weight: 600;
  transition: all 0.2s;
}

.btn-outline:hover {
  background-color: #0F172A;
  color: #FFFFFF;
}
```

### Light Button (für dunkle Hintergründe)

```css
.btn-light {
  background-color: #FFFFFF;
  color: #0F172A;
  border: 2px solid #FFFFFF;
  border-radius: 6px;
  padding: 12px 24px;
  font-size: 16px;
  font-weight: 600;
  transition: all 0.2s;
}

.btn-light:hover {
  background-color: #F1F5F9;
  border-color: #F1F5F9;
}
```

### Link mit Pfeil

```css
.link-arrow {
  color: #10B981;
  font-size: 16px;
  font-weight: 600;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.link-arrow::after {
  content: '→';
  font-size: 18px;
  transition: transform 0.2s;
}

.link-arrow:hover::after {
  transform: translateX(4px);
}

.link-arrow:hover {
  color: #059669;
}
```

---

## 4. Navigation

### Header

```css
.header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 72px;
  background-color: #FFFFFF;
  border-bottom: 1px solid #E2E8F0;
  z-index: 1020;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
}

:root {
  --scroll-margin: 72px;
}
```

### Dark Header (Alternative)

```css
.header-dark {
  background-color: #0F172A;
  border-bottom: 1px solid #1E293B;
}

.header-dark .nav-link {
  color: #F1F5F9;
}

.header-dark .nav-link:hover {
  color: #10B981;
}
```

### Navigation Links

```css
.nav-link {
  color: #334155;
  font-size: 16px;
  font-weight: 500;
  text-decoration: none;
  padding: 8px 16px;
  transition: color 0.2s;
}

.nav-link:hover {
  color: #0F172A;
}

.nav-link.active {
  color: #10B981;
}
```

---

## 5. Cards & Komponenten

### Service Card (Icon + Text)

```css
.service-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 24px;
  background-color: #FFFFFF;
  border: 1px solid #E2E8F0;
  border-radius: 8px;
  transition: box-shadow 0.3s, transform 0.2s;
}

.service-card:hover {
  box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.1);
  transform: translateY(-2px);
}

.service-card-icon {
  width: 48px;
  height: 48px;
  color: #10B981;
}

.service-card-title {
  color: #0F172A;
  font-size: 18px;
  font-weight: 600;
}
```

### Content Card (mit Bild)

```css
.content-card {
  background-color: #FFFFFF;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #E2E8F0;
  transition: box-shadow 0.3s;
}

.content-card:hover {
  box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.1);
}

.content-card-image {
  width: 100%;
  aspect-ratio: 16/9;
  object-fit: cover;
}

.content-card-body {
  padding: 24px;
}

.content-card-title {
  font-size: 20px;
  font-weight: 600;
  color: #0F172A;
  margin-bottom: 12px;
}

.content-card-text {
  font-size: 16px;
  color: #64748B;
  line-height: 1.6;
}
```

### Radio Card (Wizard)

```css
.radio-card {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 16px 24px;
  border: 2px solid #E2E8F0;
  border-radius: 8px;
  background-color: #FFFFFF;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.radio-card:hover {
  border-color: #94A3B8;
}

.radio-card.selected {
  border-color: #0F172A;
  box-shadow: inset 0 0 0 1px #0F172A;
  background-color: #F8FAFC;
}

.radio-card-icon {
  width: 48px;
  height: 48px;
  color: #10B981;
}

.radio-card-label {
  font-size: 16px;
  font-weight: 500;
  color: #0F172A;
}
```

### Feature Card (zentriert)

```css
.feature-card {
  text-align: center;
  padding: 32px 24px;
  background-color: #FFFFFF;
  border-radius: 8px;
  border: 1px solid #E2E8F0;
}

.feature-card-icon {
  width: 64px;
  height: 64px;
  color: #10B981;
  margin: 0 auto 24px;
}

.feature-card-title {
  font-size: 20px;
  font-weight: 600;
  color: #0F172A;
  margin-bottom: 12px;
}

.feature-card-text {
  font-size: 16px;
  color: #64748B;
  line-height: 1.6;
}
```

### Result Card (Dashboard)

> Für Ergebnisanzeigen wie im Heizlast-Rechner

```css
.result-card {
  background-color: #0F172A;
  border-radius: 12px;
  overflow: hidden;
}

.result-card-header {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 24px;
}

.result-card-icon {
  width: 48px;
  height: 48px;
  color: #FFFFFF;
}

.result-card-value {
  font-size: 36px;
  font-weight: 700;
  color: #FFFFFF;
}

.result-card-label {
  font-size: 14px;
  color: #94A3B8;
}

.result-card-body {
  padding: 24px;
  background-color: #FFFFFF;
}
```

### Highlight Box (Akzent-Info)

> Für hervorgehobene Informationen wie Stromverbrauch-Überschlag

```css
.highlight-box {
  background-color: #D1FAE5;
  border-radius: 8px;
  padding: 20px 24px;
  margin-bottom: 24px;
}

.highlight-box-label {
  font-size: 12px;
  font-weight: 600;
  color: #065F46;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 8px;
}

.highlight-box-value {
  font-size: 24px;
  font-weight: 700;
  color: #059669;
  margin-bottom: 8px;
}

.highlight-box-text {
  font-size: 14px;
  color: #047857;
  line-height: 1.5;
}
```

### Stats Grid (Kennzahlen)

> Für Statistik-Anzeigen wie Tagesbedarf, Heizleistung etc.

```css
.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
  padding: 24px;
}

.stat-item {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.stat-icon {
  width: 20px;
  height: 20px;
  color: #64748B;
  flex-shrink: 0;
}

.stat-label {
  font-size: 12px;
  color: #64748B;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 20px;
  font-weight: 700;
  color: #0F172A;
}

.stat-subtext {
  font-size: 12px;
  color: #94A3B8;
  margin-top: 2px;
}
```

### Info Box (Hinweis)

> Für erklärende Hinweise mit blauem Rand

```css
.info-box {
  background-color: #EFF6FF;
  border-left: 4px solid #3B82F6;
  border-radius: 0 8px 8px 0;
  padding: 16px 20px;
  display: flex;
  gap: 12px;
}

.info-box-icon {
  width: 20px;
  height: 20px;
  color: #3B82F6;
  flex-shrink: 0;
  margin-top: 2px;
}

.info-box-title {
  font-size: 16px;
  font-weight: 600;
  color: #1E40AF;
  margin-bottom: 4px;
}

.info-box-text {
  font-size: 14px;
  color: #1E3A8A;
  line-height: 1.5;
}
```

---

## 6. Formulare

### Text Input

```css
.input {
  width: 100%;
  height: 48px;
  padding: 12px 16px;
  font-size: 16px;
  font-family: inherit;
  color: #0F172A;
  background-color: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.input:focus {
  outline: none;
  border-color: #10B981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.input::placeholder {
  color: #94A3B8;
}

.input:disabled {
  background-color: #F1F5F9;
  cursor: not-allowed;
}
```

### Textarea

```css
.textarea {
  width: 100%;
  min-height: 120px;
  padding: 12px 16px;
  font-size: 16px;
  font-family: inherit;
  color: #0F172A;
  background-color: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  resize: vertical;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.textarea:focus {
  outline: none;
  border-color: #10B981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}
```

### Select

```css
.select {
  width: 100%;
  height: 48px;
  padding: 12px 16px;
  font-size: 16px;
  font-family: inherit;
  color: #0F172A;
  background-color: #FFFFFF;
  border: 1px solid #CBD5E1;
  border-radius: 6px;
  cursor: pointer;
  appearance: none;
  background-image: url("data:image/svg+xml,..."); /* Chevron Icon */
  background-repeat: no-repeat;
  background-position: right 16px center;
}

.select:focus {
  outline: none;
  border-color: #10B981;
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}
```

### Range Slider

```css
.range-slider {
  -webkit-appearance: none;
  width: 100%;
  height: 8px;
  background: linear-gradient(to right, #0F172A 0%, #0F172A var(--value), #E2E8F0 var(--value));
  border-radius: 4px;
}

.range-slider::-webkit-slider-thumb {
  -webkit-appearance: none;
  width: 24px;
  height: 24px;
  background: #0F172A;
  border-radius: 50%;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(15, 23, 42, 0.3);
  transition: transform 0.2s;
}

.range-slider::-webkit-slider-thumb:hover {
  transform: scale(1.1);
}

.range-value {
  background-color: #0F172A;
  color: #FFFFFF;
  padding: 8px 16px;
  font-weight: 600;
  border-radius: 4px;
  display: inline-block;
}
```

### Checkbox

```css
.checkbox-label {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  font-size: 16px;
  color: #334155;
  cursor: pointer;
}

.checkbox-input {
  width: 20px;
  height: 20px;
  accent-color: #0F172A;
  cursor: pointer;
}
```

### Radio Button

```css
.radio-label {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 16px;
  color: #334155;
  cursor: pointer;
}

.radio-input {
  width: 20px;
  height: 20px;
  accent-color: #0F172A;
  cursor: pointer;
}
```

---

## 7. Icons

### Icon-System

```css
.icon {
  width: 24px;
  height: 24px;
  fill: currentColor;
}

.icon-primary {
  color: #0F172A;
}

.icon-accent {
  color: #10B981;
}

.icon-muted {
  color: #94A3B8;
}

.icon-sm {
  width: 16px;
  height: 16px;
}

.icon-lg {
  width: 32px;
  height: 32px;
}

.icon-xl {
  width: 48px;
  height: 48px;
}
```

### Checkmark/Success Icon

```css
.checkmark {
  color: #16A34A;
  width: 24px;
  height: 24px;
}

.list-check::before {
  content: '✓';
  color: #0F172A;
  font-weight: bold;
  margin-right: 12px;
}
```

---

## 8. Layout & Spacing

### Container

```css
.container {
  max-width: 1280px;
  margin: 0 auto;
  padding: 0 24px;
}

.container-sm {
  max-width: 768px;
}

.container-lg {
  max-width: 1440px;
}
```

### Spacing-System

```css
:root {
  --pv-spacing-xs: 4px;
  --pv-spacing-sm: 8px;
  --pv-spacing-md: 16px;
  --pv-spacing-lg: 24px;
  --pv-spacing-xl: 32px;
  --pv-spacing-2xl: 48px;
  --pv-spacing-3xl: 64px;
  --pv-spacing-4xl: 96px;
}
```

### Sections

```css
.section {
  padding: 64px 0;
}

.section-sm {
  padding: 48px 0;
}

.section-lg {
  padding: 96px 0;
}

.section-alt {
  background-color: #F8FAFC;
}

.section-dark {
  background-color: #0F172A;
  color: #FFFFFF;
}

.section-dark .text-muted {
  color: #94A3B8;
}
```

### Grid

```css
.grid {
  display: grid;
  gap: 24px;
}

.grid-2 {
  grid-template-columns: repeat(2, 1fr);
}

.grid-3 {
  grid-template-columns: repeat(3, 1fr);
}

.grid-4 {
  grid-template-columns: repeat(4, 1fr);
}

@media (max-width: 768px) {
  .grid-2,
  .grid-3,
  .grid-4 {
    grid-template-columns: 1fr;
  }
}
```

---

## 9. Shadows & Border Radius

### Shadows

```css
:root {
  --pv-shadow-sm: 0 1px 2px 0 rgba(15, 23, 42, 0.05);
  --pv-shadow-md: 0 4px 6px -1px rgba(15, 23, 42, 0.1);
  --pv-shadow-lg: 0 10px 15px -3px rgba(15, 23, 42, 0.1);
  --pv-shadow-xl: 0 20px 25px -5px rgba(15, 23, 42, 0.1);
}
```

### Border Radius

```css
:root {
  --pv-radius-sm: 4px;
  --pv-radius-md: 6px;
  --pv-radius-lg: 8px;
  --pv-radius-xl: 12px;
  --pv-radius-2xl: 16px;
  --pv-radius-full: 9999px;
}
```

---

## 10. Hero Sections

### Standard Hero

```css
.hero {
  background-color: #0F172A;
  color: #FFFFFF;
  padding: 120px 0 80px;
  text-align: center;
}

.hero-title {
  font-size: 48px;
  font-weight: 700;
  line-height: 1.2;
  margin-bottom: 24px;
}

.hero-subtitle {
  font-size: 20px;
  color: #94A3B8;
  max-width: 640px;
  margin: 0 auto 32px;
}

.hero-buttons {
  display: flex;
  gap: 16px;
  justify-content: center;
}

@media (max-width: 768px) {
  .hero-title {
    font-size: 32px;
  }

  .hero-subtitle {
    font-size: 18px;
  }

  .hero-buttons {
    flex-direction: column;
    align-items: center;
  }
}
```

### Hero mit Gradient

```css
.hero-gradient {
  background: linear-gradient(135deg, #0F172A 0%, #1E293B 50%, #334155 100%);
}
```

---

## 11. Footer

### Standard Footer

```css
.footer {
  background-color: #0F172A;
  color: #F1F5F9;
  padding: 64px 0 32px;
}

.footer-grid {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr 1fr;
  gap: 48px;
  margin-bottom: 48px;
}

.footer-title {
  font-size: 18px;
  font-weight: 600;
  color: #FFFFFF;
  margin-bottom: 24px;
}

.footer-link {
  display: block;
  color: #94A3B8;
  text-decoration: none;
  padding: 8px 0;
  transition: color 0.2s;
}

.footer-link:hover {
  color: #FFFFFF;
}

.footer-bottom {
  border-top: 1px solid #1E293B;
  padding-top: 32px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  color: #64748B;
  font-size: 14px;
}

@media (max-width: 768px) {
  .footer-grid {
    grid-template-columns: 1fr;
    gap: 32px;
  }

  .footer-bottom {
    flex-direction: column;
    gap: 16px;
    text-align: center;
  }
}
```

---

## 12. Utilities

### Text Colors

```css
.text-primary { color: #0F172A; }
.text-secondary { color: #64748B; }
.text-muted { color: #94A3B8; }
.text-accent { color: #10B981; }
.text-success { color: #16A34A; }
.text-warning { color: #D97706; }
.text-error { color: #DC2626; }
.text-white { color: #FFFFFF; }
```

### Background Colors

```css
.bg-primary { background-color: #0F172A; }
.bg-secondary { background-color: #1E293B; }
.bg-accent { background-color: #10B981; }
.bg-white { background-color: #FFFFFF; }
.bg-slate-50 { background-color: #F8FAFC; }
.bg-slate-100 { background-color: #F1F5F9; }
```

### Text Alignment

```css
.text-left { text-align: left; }
.text-center { text-align: center; }
.text-right { text-align: right; }
```

### Display

```css
.hidden { display: none; }
.block { display: block; }
.inline-block { display: inline-block; }
.flex { display: flex; }
.grid { display: grid; }
```

---

## 13. Responsive Breakpoints

```css
/* Mobile first */
@media (min-width: 640px) {
  /* sm: Small tablets */
}

@media (min-width: 768px) {
  /* md: Tablets */
}

@media (min-width: 1024px) {
  /* lg: Laptops */
}

@media (min-width: 1280px) {
  /* xl: Desktops */
}

@media (min-width: 1536px) {
  /* 2xl: Large screens */
}
```

---

## 14. Animationen & Transitions

### Standard Transitions

```css
:root {
  --pv-transition-fast: 0.15s ease;
  --pv-transition-base: 0.2s ease;
  --pv-transition-slow: 0.3s ease;
}
```

### Hover Animations

```css
.hover-lift {
  transition: transform var(--pv-transition-base), box-shadow var(--pv-transition-base);
}

.hover-lift:hover {
  transform: translateY(-4px);
  box-shadow: var(--pv-shadow-lg);
}

.hover-scale {
  transition: transform var(--pv-transition-base);
}

.hover-scale:hover {
  transform: scale(1.02);
}
```

### Fade In Animation

```css
@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.animate-fadeIn {
  animation: fadeIn 0.3s ease forwards;
}
```

---

## 15. Vollständige CSS-Variablen-Referenz

```css
:root {
  /* Farben - Primär */
  --pv-primary-dark: #0F172A;
  --pv-primary: #1E293B;
  --pv-primary-light: #334155;

  /* Farben - Akzent (Emerald) */
  --pv-accent: #10B981;
  --pv-accent-hover: #059669;
  --pv-accent-light: #D1FAE5;

  /* Farben - Slate */
  --pv-slate-50: #F8FAFC;
  --pv-slate-100: #F1F5F9;
  --pv-slate-200: #E2E8F0;
  --pv-slate-300: #CBD5E1;
  --pv-slate-400: #94A3B8;
  --pv-slate-500: #64748B;
  --pv-slate-600: #475569;
  --pv-slate-700: #334155;
  --pv-slate-800: #1E293B;
  --pv-slate-900: #0F172A;

  /* Farben - Status */
  --pv-success: #16A34A;
  --pv-warning: #D97706;
  --pv-error: #DC2626;
  --pv-info: #2563EB;

  /* Farben - Status Hintergründe */
  --pv-success-bg: #F0FDF4;
  --pv-warning-bg: #FFFBEB;
  --pv-error-bg: #FEF2F2;
  --pv-info-bg: #EFF6FF;

  /* Typografie */
  --pv-font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  --pv-font-size-xs: 12px;
  --pv-font-size-sm: 14px;
  --pv-font-size-base: 16px;
  --pv-font-size-lg: 18px;
  --pv-font-size-xl: 20px;
  --pv-font-size-2xl: 24px;
  --pv-font-size-3xl: 30px;
  --pv-font-size-4xl: 36px;
  --pv-font-size-5xl: 48px;

  /* Spacing */
  --pv-spacing-xs: 4px;
  --pv-spacing-sm: 8px;
  --pv-spacing-md: 16px;
  --pv-spacing-lg: 24px;
  --pv-spacing-xl: 32px;
  --pv-spacing-2xl: 48px;
  --pv-spacing-3xl: 64px;
  --pv-spacing-4xl: 96px;

  /* Border Radius */
  --pv-radius-sm: 4px;
  --pv-radius-md: 6px;
  --pv-radius-lg: 8px;
  --pv-radius-xl: 12px;
  --pv-radius-2xl: 16px;
  --pv-radius-full: 9999px;

  /* Shadows */
  --pv-shadow-sm: 0 1px 2px 0 rgba(15, 23, 42, 0.05);
  --pv-shadow-md: 0 4px 6px -1px rgba(15, 23, 42, 0.1);
  --pv-shadow-lg: 0 10px 15px -3px rgba(15, 23, 42, 0.1);
  --pv-shadow-xl: 0 20px 25px -5px rgba(15, 23, 42, 0.1);

  /* Transitions */
  --pv-transition-fast: 0.15s ease;
  --pv-transition-base: 0.2s ease;
  --pv-transition-slow: 0.3s ease;

  /* Layout */
  --pv-max-width: 1280px;
  --pv-header-height: 72px;
}
```

---

## Unterschiede zum App Style Guide

| Aspekt | App (Interhyp) | Web (PV-Calor) |
|--------|----------------|----------------|
| Primärfarbe | Orange `#EE7900` | Dark Slate `#0F172A` |
| Akzentfarbe | Teal `#579EB0` | Emerald `#10B981` |
| Button-Radius | 0px (eckig) | 6px (leicht gerundet) |
| Schriftart | TT Norms | Inter |
| Design-Stil | Warm, einladend | Modern, professionell, dunkel |
| Hover-Effekte | Subtle | Mit Lift-Effekt |
