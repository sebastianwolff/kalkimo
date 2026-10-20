# Kalkimo Planner – Frontend Style Guide

Verbindliche Gestaltungsregeln für das Kalkimo-Frontend. Ziel ist das Erscheinungsbild einer
seriösen Finanzanwendung: ruhig, präzise, vertrauenswürdig. Leitbild ist ein Investment-Memo
einer Bank, nicht eine Consumer-App.

- Tokens: `src/theme/variables.css`
- Gemeinsame Klassen: `src/theme/global.css`
- Logo: `src/components/layout/KalkLogo.vue`

---

## Grundsätze

1. **Zurückhaltung.** Farbe trägt Bedeutung, nicht Dekoration. Flächen sind weiß oder hellgrau.
2. **Linien statt Schatten.** Panels haben einen 1px-Rahmen und keinen Schatten. Schatten nur für schwebende Ebenen wie Dialoge oder Drawer.
3. **Kleine Radien.** 2 bis 4px. Keine Pillen, keine runden Kacheln.
4. **Zahlen zuerst.** Alle Beträge mit tabellarischen Ziffern, rechtsbündig in Tabellen.
5. **Grün und Rot nur semantisch.** Positiv/negativ, Erfolg/Fehler. Nie als Markenfarbe oder Schmuck.
6. **Keine Emojis, keine verspielten Animationen.** Übergänge höchstens 150ms, ohne Skalierung oder Hüpfen.

---

## Farben

### Marke: Marineblau

| Token | Wert | Verwendung |
|---|---|---|
| `--kalk-navy-950` | `#071526` | Rahmen der Seitenleiste |
| `--kalk-navy-900` | `#0B1F36` | Seitenleiste, Seitentitel, Kennzahlen |
| `--kalk-navy-800` | `#132B4A` | Primär-Button |
| `--kalk-navy-700` | `#1C3A61` | Hover, Links, ausgewählte Controls |
| `--kalk-navy-600` | `#284D7E` | Fokus-Rahmen, Fortschritt |
| `--kalk-navy-400` | `#6C8DB8` | Erledigte Wizard-Schritte |
| `--kalk-navy-100` / `-50` | `#E3EAF3` / `#F1F5FA` | Dezente Hervorhebung |

### Akzent: Messing

Nur für Markenmarker: aktiver Navigationspunkt, Logo-Detail, Schritt-Nummern.

| Token | Wert |
|---|---|
| `--kalk-brass-600` | `#8C6D2F` |
| `--kalk-brass-500` | `#A8873F` |
| `--kalk-brass-300` | `#D6C08E` |

### Neutral

| Token | Wert | Verwendung |
|---|---|---|
| `--kalk-gray-900` | `#17202C` | Fließtext, Werte |
| `--kalk-gray-700` | `#3B4655` | Labels |
| `--kalk-gray-600` | `#556070` | Sekundärtext |
| `--kalk-gray-500` | `#646E7C` | Hinweise, Tabellenköpfe |
| `--kalk-gray-400` | `#98A1AD` | Platzhalter, inaktive Icons |
| `--kalk-gray-300` | `#C6CCD4` | `--kalk-border-strong`: Eingabefelder |
| `--kalk-gray-200` | `#DCE0E5` | `--kalk-border`: Panels, Trennlinien |
| `--kalk-gray-100` | `#EBEEF1` | Zeilentrenner in Tabellen |
| `--kalk-gray-50` | `#F5F6F8` | Tabellenkopf, Panel-Footer |
| `--kalk-canvas` | `#F3F4F6` | Seitenhintergrund |
| `--kalk-surface` | `#FFFFFF` | Panels, Eingabefelder |

### Semantik

Jede Farbe gibt es als Text, Hintergrund (`-bg`) und Rahmen (`-border`).

| Token | Text | Verwendung |
|---|---|---|
| `--kalk-positive` | `#1E6B47` | Positive Werte, Erfolg |
| `--kalk-negative` | `#A12C2C` | Negative Werte, Fehler, Löschen |
| `--kalk-caution` | `#8A5A0B` | Warnungen |
| `--kalk-notice` | `#1F4E85` | Informationen |

**Legacy-Namen.** `--kalk-primary-*`, `--kalk-accent-*`, `--kalk-success`, `--kalk-warning`, `--kalk-error`
und `--kalk-info` bleiben als Aliase bestehen. `--kalk-accent-*` zeigt inzwischen auf Marineblau.
In neuem Code die direkten Namen verwenden.

---

## Typografie

| Rolle | Schrift | Größe / Gewicht |
|---|---|---|
| Oberfläche | IBM Plex Sans | 15px / 400 |
| Seitentitel | Source Serif 4 | 30px / 600, navy-900 |
| Panel-Titel | IBM Plex Sans | 15px / 600 |
| Label | IBM Plex Sans | 14px / 500, gray-700 |
| Eyebrow, Tabellenkopf, KPI-Label | IBM Plex Sans | 11px / 600, Versalien, Laufweite 0.06em |
| Kennzahl | IBM Plex Sans | 24px / 600, navy-900, tabellarische Ziffern |
| Versionsnummern, IDs | IBM Plex Mono | 12px |

Die Serifenschrift ist Seitentiteln, der Login-Headline und dem Logo vorbehalten.

---

## Abstände, Radien, Schatten

- Abstände im 4px-Raster: `--kalk-space-1` bis `--kalk-space-12`.
- Radien: `--kalk-radius-sm` 2px (Badges), `-md` 3px (Buttons, Inputs), `-lg` 4px (Panels).
- Schatten: `--kalk-shadow-sm` ist bewusst `none`. Für Overlays `--kalk-shadow-lg` / `-xl`.

---

## Layout

- **Desktop ab 992px:** feste Seitenleiste links (`--kalk-sidebar-width` 248px), weißer Header mit 1px-Linie.
- **Mobil:** Seitenleiste als Overlay-Menü, Header mit Menü-Button.
- **Inhalt:** `.page-content` begrenzt auf `--kalk-content-max` 1040px, `.page-content.narrow` auf 760px.
- **Seitenkopf:** `.page-head` mit `.eyebrow`, `.page-title`, `.page-subtitle` und rechts `.page-actions`.
- **Zweispaltig:** Hauptspalte 2fr, Seitenspalte 1fr. Unter 960px einspaltig.

```html
<div class="page-content">
  <header class="page-head">
    <div>
      <span class="eyebrow">Portfolio</span>
      <h1 class="page-title">Projekte</h1>
      <p class="page-subtitle">Alle gespeicherten Investitionsprojekte.</p>
    </div>
    <div class="page-actions">
      <button class="btn btn-primary">Neues Projekt</button>
    </div>
  </header>
  …
</div>
```

---

## Bausteine (global.css)

### Panel

```html
<section class="panel">
  <div class="panel-header">
    <h2 class="panel-title">Titel</h2>
    <button class="btn btn-ghost btn-sm">Aktion</button>
  </div>
  <div class="panel-body">…</div>
  <div class="panel-footer">…</div>
</section>
```

`KalkCard` rendert denselben Stil und bleibt für bestehende Views erhalten.

### Kennzahlen

```html
<div class="kpi-grid">
  <div class="kpi">
    <div class="kpi-label">IRR nach Steuern</div>
    <div class="kpi-value">5,8 %</div>
    <div class="kpi-sub">über 15 Jahre</div>
  </div>
</div>
```

Die Kacheln teilen sich Rahmenlinien und wirken wie eine Kennzahlenleiste.

### Tabellen

`.data-table` mit Versal-Kopfzeile auf grauem Grund, Zeilentrennern und Hover.
Zahlenspalten bekommen `.num` für Rechtsbündigkeit. Breite Tabellen in einen
Container mit `overflow-x: auto` setzen.

### Aufstellungen

`.data-row` mit `.data-label` und `.data-value` für Label-Wert-Listen.
`.data-row.total` setzt eine Summenzeile mit dunkler Linie darüber, wie in einer Bilanz.

### Hinweise

`.callout` mit `.info`, `.success`, `.warning` oder `.error`. Linker 3px-Balken, gedämpfter Hintergrund, Icon links.

### Buttons

| Klasse | Verwendung |
|---|---|
| `.btn-primary` | Hauptaktion einer Seite, maximal eine pro Bereich |
| `.btn-outline` | Sekundäre Aktion |
| `.btn-ghost` | Tertiäre Aktion in Panel-Köpfen |
| `.btn-danger` | Löschen, Abmelden |
| `.btn-sm`, `.btn-block` | Größe, volle Breite |

Ionic-Buttons (`ion-button`) sind über `variables.css` an denselben Stil angeglichen.

### Badges

`.badge` mit `.badge-neutral`, `.badge-info`, `.badge-success`, `.badge-warning`, `.badge-error`.
Rechteckig, 11px, mit feinem Rahmen.

### Werte

`.value-positive` und `.value-negative` färben Beträge nach Vorzeichen.

### Leerzustand

`.empty-state` mit gestricheltem Rahmen, Icon, Titel, Text und einer Aktion.

---

## Formulare

- Eingabefelder 42px hoch, 1px `--kalk-border-strong`, Radius 3px, weißer Grund.
- Fokus: Rahmen `--kalk-navy-600` plus 3px-Ring `--kalk-ring-color`.
- Fehler: Rahmen `--kalk-negative`, Meldung 12px darunter.
- Einheiten wie €, %, m² stehen rechts im Feld in gray-500.
- Native Selects und Inputs in Tabellen: 32px hoch, sonst gleiche Regeln.

---

## Wizard

- **Stepper:** nummerierte Prozessleiste mit Unterstrich. Aktiv: navy-900, 3px. Erledigt: navy-400 mit Häkchen. Offen: gray-200.
- **Mobil:** Stepper als schmale Segmentleiste mit „Schritt X von Y“ und Schrittnamen.
- **Fußleiste:** Zurück als `.btn-outline`, Weiter als Primär-Button, Berechnen in navy-900.
- **Header:** Eyebrow „Projekt-Assistent“, darunter der Projektname.

---

## Erreichbarkeit

- Kontrast für Text mindestens 4.5:1. Die Grauwerte ab gray-500 auf Weiß erfüllen das.
- Sichtbarer Fokus über `:focus-visible` mit 2px navy-500.
- Klickbare Tabellenzeilen sind per Tab erreichbar und mit Enter auslösbar.
- `prefers-reduced-motion` schaltet Übergänge ab.
