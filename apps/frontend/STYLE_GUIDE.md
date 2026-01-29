# Kalkimo Planner - Frontend Style Guide

Dieses Dokument definiert die Design-Standards und CSS-Variablen für das Kalkimo Frontend.
Alle Komponenten sollten diese Richtlinien einhalten, um ein konsistentes Erscheinungsbild zu gewährleisten.

---

## Farbpalette

### Primary (Slate Blue)
Professionelle, vertrauenswürdige Basis für Header und primäre Aktionen.

| Variable | Wert | Verwendung |
|----------|------|------------|
| `--kalk-primary-900` | `#0F172A` | Header, primäre Buttons |
| `--kalk-primary-800` | `#1E293B` | Hover-States auf Primary |
| `--kalk-primary-700` | `#334155` | Progress-Bar Hintergrund |
| `--kalk-primary-600` | `#475569` | Sekundäre Texte auf dunklem Hintergrund |
| `--kalk-primary-500` | `#64748B` | Deaktivierte Elemente |

### Accent (Emerald Green)
Finanz-typische Akzentfarbe für Highlights und CTAs.

| Variable | Wert | Verwendung |
|----------|------|------------|
| `--kalk-accent-700` | `#047857` | Hover auf Accent |
| `--kalk-accent-600` | `#059669` | Sekundärer Accent, Highlights |
| `--kalk-accent-500` | `#10B981` | **Haupt-Akzentfarbe**, Buttons, Links |
| `--kalk-accent-400` | `#34D399` | Helle Accent-Elemente |
| `--kalk-accent-100` | `#D1FAE5` | Erfolgs-Badges Hintergrund |
| `--kalk-accent-50` | `#ECFDF5` | Sehr heller Accent-Hintergrund |

### Grays (Neutral)
Für Text, Hintergründe und Borders.

| Variable | Wert | Verwendung |
|----------|------|------------|
| `--kalk-gray-900` | `#111827` | **Primärer Text** |
| `--kalk-gray-800` | `#1F2937` | Überschriften |
| `--kalk-gray-700` | `#374151` | Labels, sekundäre Überschriften |
| `--kalk-gray-600` | `#4B5563` | Stärkerer sekundärer Text |
| `--kalk-gray-500` | `#6B7280` | **Sekundärer Text**, Hints |
| `--kalk-gray-400` | `#9CA3AF` | Placeholder, Icons |
| `--kalk-gray-300` | `#D1D5DB` | Borders bei Hover |
| `--kalk-gray-200` | `#E5E7EB` | **Standard Borders** |
| `--kalk-gray-100` | `#F3F4F6` | Karten-Borders, leichte Trennlinien |
| `--kalk-gray-50` | `#F9FAFB` | **Helle Hintergründe**, Cards |

### Status-Farben

| Variable | Wert | Verwendung |
|----------|------|------------|
| `--kalk-success` | `#059669` | Erfolg |
| `--kalk-success-bg` | `#ECFDF5` | Erfolg-Hintergrund |
| `--kalk-warning` | `#D97706` | Warnung |
| `--kalk-warning-bg` | `#FFFBEB` | Warnung-Hintergrund |
| `--kalk-error` | `#DC2626` | Fehler |
| `--kalk-error-bg` | `#FEF2F2` | Fehler-Hintergrund |
| `--kalk-info` | `#2563EB` | Information |
| `--kalk-info-bg` | `#EFF6FF` | Info-Hintergrund |

---

## Typografie

### Schriftart
```css
--kalk-font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
--kalk-font-mono: 'SF Mono', 'Monaco', 'Inconsolata', monospace;
```

### Schriftgrößen

| Variable | Wert | Verwendung |
|----------|------|------------|
| `--kalk-text-xs` | `0.75rem` (12px) | Meta-Informationen, Badges |
| `--kalk-text-sm` | `0.875rem` (14px) | Labels, Hints, sekundärer Text |
| `--kalk-text-base` | `1rem` (16px) | **Standard-Text**, Inputs |
| `--kalk-text-lg` | `1.125rem` (18px) | Card-Titles, größere Labels |
| `--kalk-text-xl` | `1.25rem` (20px) | Sektion-Überschriften |
| `--kalk-text-2xl` | `1.5rem` (24px) | Metriken, große Zahlen |
| `--kalk-text-3xl` | `1.875rem` (30px) | Seiten-Titel |

### Font-Weights
- `400` - Normal (Body Text)
- `500` - Medium (Labels)
- `600` - Semibold (Überschriften, Buttons)
- `700` - Bold (Haupttitel, Metriken)

---

## Abstände (Spacing)

Konsistentes 4px-Basis-System.

| Variable | Wert | Verwendung |
|----------|------|------------|
| `--kalk-space-1` | `0.25rem` (4px) | Minimale Abstände |
| `--kalk-space-2` | `0.5rem` (8px) | Enge Abstände, Liste-Items |
| `--kalk-space-3` | `0.75rem` (12px) | Input-Padding vertikal |
| `--kalk-space-4` | `1rem` (16px) | **Standard-Abstand**, Cards intern |
| `--kalk-space-5` | `1.25rem` (20px) | Button-Padding |
| `--kalk-space-6` | `1.5rem` (24px) | **Card-Padding**, Sektions-Abstände |
| `--kalk-space-8` | `2rem` (32px) | Große Sektions-Abstände |
| `--kalk-space-10` | `2.5rem` (40px) | Select-Padding rechts |
| `--kalk-space-12` | `3rem` (48px) | Seiten-Abstände |

---

## Borders & Radius

### Border-Radius

| Variable | Wert | Verwendung |
|----------|------|------------|
| `--kalk-radius-sm` | `6px` | Kleine Elemente, Chips |
| `--kalk-radius-md` | `8px` | **Standard**, Inputs, Buttons |
| `--kalk-radius-lg` | `12px` | Cards, große Container |
| `--kalk-radius-xl` | `16px` | Modale, große Cards |
| `--kalk-radius-full` | `9999px` | Pills, runde Badges |

### Border-Styles
```css
/* Standard Border */
border: 1px solid var(--kalk-gray-200);

/* Input Border */
border: 1.5px solid var(--kalk-gray-200);

/* Fokus Border */
border-color: var(--kalk-accent-500);
```

---

## Schatten

| Variable | Verwendung |
|----------|------------|
| `--kalk-shadow-xs` | Subtile Erhebung |
| `--kalk-shadow-sm` | **Standard Cards** |
| `--kalk-shadow-md` | Hover-States, erhöhte Elemente |
| `--kalk-shadow-lg` | Dropdowns, Modals |
| `--kalk-shadow-xl` | Prominente Overlays |

### Focus Ring
```css
--kalk-ring-color: rgba(16, 185, 129, 0.3);
box-shadow: 0 0 0 3px var(--kalk-ring-color);
```

---

## Komponenten-Patterns

### Form Group
```css
.form-group {
  margin-bottom: var(--kalk-space-5);
}
```

### Form Label
```css
.form-label {
  display: flex;
  align-items: center;
  gap: var(--kalk-space-1);
  font-size: var(--kalk-text-sm);
  font-weight: 500;
  color: var(--kalk-gray-700);
  margin-bottom: var(--kalk-space-2);
}
```

### Form Input
```css
.form-input {
  display: block;
  width: 100%;
  padding: var(--kalk-space-3) var(--kalk-space-4);
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-base);
  color: var(--kalk-gray-900);
  background-color: #ffffff;
  border: 1.5px solid var(--kalk-gray-200);
  border-radius: var(--kalk-radius-md);
  min-height: 48px;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.form-input:hover {
  border-color: var(--kalk-gray-300);
}

.form-input:focus {
  outline: none;
  border-color: var(--kalk-accent-500);
  box-shadow: 0 0 0 3px var(--kalk-ring-color);
}

.form-input.has-error {
  border-color: var(--kalk-error);
}

.form-input::placeholder {
  color: var(--kalk-gray-400);
}
```

### Form Select
```css
.form-select {
  /* Wie form-input, zusätzlich: */
  padding-right: var(--kalk-space-10);
  appearance: none;
  cursor: pointer;
}

/* Custom Chevron */
.select-chevron {
  position: absolute;
  right: var(--kalk-space-4);
  top: 50%;
  transform: translateY(-50%);
  width: 20px;
  height: 20px;
  color: var(--kalk-gray-400);
  pointer-events: none;
}
```

### Form Error & Hint
```css
.form-error {
  margin: var(--kalk-space-2) 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-error);
}

.form-hint {
  margin: var(--kalk-space-2) 0 0;
  font-size: var(--kalk-text-xs);
  color: var(--kalk-gray-500);
}
```

### Required Mark
```css
.required-mark {
  color: var(--kalk-error);
}
```

---

## Card-Patterns

### Standard Card
```css
.kalk-card {
  background: #ffffff;
  border-radius: var(--kalk-radius-lg);
  border: 1px solid var(--kalk-gray-100);
  box-shadow: var(--kalk-shadow-sm);
}

.card-header {
  padding: var(--kalk-space-5) var(--kalk-space-6);
  border-bottom: 1px solid var(--kalk-gray-100);
}

.card-title {
  font-size: var(--kalk-text-lg);
  font-weight: 600;
  color: var(--kalk-gray-900);
}

.card-content {
  padding: var(--kalk-space-6);
}

.card-footer {
  padding: var(--kalk-space-4) var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-100);
  background: var(--kalk-gray-50);
}
```

### Info/Summary Box
```css
.info-box {
  padding: var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
}
```

### Highlight Summary (Primary)
```css
.summary-highlight {
  padding: var(--kalk-space-6);
  background: var(--kalk-primary-900);
  color: #ffffff;
  border-radius: var(--kalk-radius-md);
}
```

### Accent Summary
```css
.summary-accent {
  padding: var(--kalk-space-6);
  background: var(--kalk-accent-600);
  color: #ffffff;
  border-radius: var(--kalk-radius-md);
}
```

---

## Button-Patterns

### Primary Button
```css
.btn-primary {
  background: var(--kalk-primary-900);
  color: #ffffff;
  border: none;
  border-radius: var(--kalk-radius-md);
  padding: var(--kalk-space-3) var(--kalk-space-5);
  font-weight: 600;
  font-size: var(--kalk-text-sm);
  min-height: 44px;
  box-shadow: var(--kalk-shadow-sm);
}

.btn-primary:hover:not(:disabled) {
  background: var(--kalk-primary-800);
}
```

### Outline Button
```css
.btn-outline {
  background: transparent;
  border: 1.5px solid var(--kalk-gray-300);
  color: var(--kalk-gray-700);
  border-radius: var(--kalk-radius-md);
  padding: var(--kalk-space-3) var(--kalk-space-5);
  font-weight: 600;
  font-size: var(--kalk-text-sm);
  min-height: 44px;
}

.btn-outline:hover:not(:disabled) {
  background: var(--kalk-gray-50);
  border-color: var(--kalk-gray-400);
}
```

### Success Button
```css
.btn-success {
  background: var(--kalk-accent-600);
  color: #ffffff;
  /* Rest wie btn-primary */
}

.btn-success:hover:not(:disabled) {
  background: var(--kalk-accent-700);
}
```

### Disabled State
```css
.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
```

---

## Section-Patterns

### Section Header
```css
.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--kalk-space-4);
}

.section-label {
  font-size: var(--kalk-text-base);
  font-weight: 600;
  color: var(--kalk-gray-900);
  margin: 0;
}
```

### Section Divider
```css
.section {
  margin-top: var(--kalk-space-8);
  padding-top: var(--kalk-space-6);
  border-top: 1px solid var(--kalk-gray-200);
}
```

### Empty State
```css
.empty-state {
  text-align: center;
  padding: var(--kalk-space-6);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  color: var(--kalk-gray-500);
}
```

---

## List/Item-Patterns

### Item Card (für Listen)
```css
.item-card {
  padding: var(--kalk-space-4);
  background: var(--kalk-gray-50);
  border-radius: var(--kalk-radius-md);
  border: 1px solid var(--kalk-gray-200);
}
```

### Item List
```css
.item-list {
  display: flex;
  flex-direction: column;
  gap: var(--kalk-space-4);
}
```

### Summary Row
```css
.summary-row {
  display: flex;
  justify-content: space-between;
  padding: var(--kalk-space-2) 0;
  border-bottom: 1px solid var(--kalk-gray-200);
}

.summary-row:last-child {
  border-bottom: none;
}

.summary-row.highlight {
  margin-top: var(--kalk-space-2);
  padding-top: var(--kalk-space-4);
  border-top: 2px solid var(--kalk-accent-500);
  border-bottom: none;
  font-size: var(--kalk-text-lg);
}
```

---

## Grid-Patterns

### 2-Column Grid
```css
.grid-2 {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--kalk-space-4);
}

@media (max-width: 600px) {
  .grid-2 {
    grid-template-columns: 1fr;
  }
}
```

### 3-Column Grid
```css
.grid-3 {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--kalk-space-4);
}

@media (max-width: 768px) {
  .grid-3 {
    grid-template-columns: 1fr;
  }
}
```

### Auto-fit Grid
```css
.grid-auto {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: var(--kalk-space-6);
}
```

---

## Page Layout

### Page Container
```css
.page-container {
  max-width: 800px;
  margin: 0 auto;
  padding: var(--kalk-space-6);
}
```

### Page Title
```css
.page-title {
  font-family: var(--kalk-font-family);
  font-size: var(--kalk-text-3xl);
  font-weight: 700;
  color: var(--kalk-gray-900);
  margin: 0;
}

.page-subtitle {
  color: var(--kalk-gray-500);
  font-size: var(--kalk-text-lg);
  margin-top: var(--kalk-space-1);
}
```

---

## Transitions

Standard-Transitions für konsistente Animationen:

```css
/* Standard Transition */
transition: all 0.15s;

/* Für Borders und Shadows */
transition: border-color 0.15s, box-shadow 0.15s;

/* Für Hintergründe */
transition: background-color 0.2s;

/* Für Transform-Effekte */
transition: transform 0.2s, box-shadow 0.2s;
```

---

## Ionic Overrides

Wichtige Ionic-Variablen die gesetzt werden müssen:

```css
:root {
  /* Hintergrund auf Weiß */
  --ion-background-color: #ffffff;
  --ion-background-color-rgb: 255, 255, 255;

  /* Text auf Dunkel */
  --ion-text-color: var(--kalk-gray-900);

  /* Toolbar dunkel */
  --ion-toolbar-background: var(--kalk-primary-900);
  --ion-toolbar-color: #ffffff;

  /* Items transparent */
  --ion-item-background: transparent;
  --ion-item-border-color: transparent;

  /* Cards weiß */
  --ion-card-background: #ffffff;

  /* Schriftart */
  --ion-font-family: var(--kalk-font-family);
}

/* Dark Mode deaktivieren */
@media (prefers-color-scheme: dark) {
  :root {
    --ion-background-color: #ffffff;
    --ion-text-color: var(--kalk-gray-900);
    --ion-item-background: transparent;
    --ion-card-background: #ffffff;
  }
}
```

---

## Best Practices

1. **Farben**: Immer CSS-Variablen verwenden, nie Hardcoded-Werte
2. **Abstände**: Nur die definierten `--kalk-space-*` Variablen nutzen
3. **Inputs**: Native HTML-Elemente mit Custom-Styling bevorzugen
4. **Konsistenz**: Gleiche Patterns für gleiche UI-Elemente
5. **Mobile-First**: Responsive Breakpoints bei 600px und 768px
6. **Accessibility**: Focus-States immer mit Ring-Shadow
7. **Hintergründe**: Weiß (`#ffffff`) für Cards, Gray-50 für Sektionen

---

## Datei-Referenzen

- Theme-Variablen: `src/theme/variables.css`
- Globale Styles: `src/theme/global.css`
- Komponenten: `src/components/common/`
