# Fachliches Konzept – ImmoInvest Planner (Update)

**Ziel:** Realistische, bankstandardnahe Immobilien-Investitionskalkulation für 5 / 10–15 / 20 Jahre mit vollständiger Steuerlogik (DE), Szenarien, Cashflow/Liquidität, Maßnahmen-/Sanierungslogik, Multi-Investor-Strukturen – **inkl. erweiterter Datenerfassung für bessere Auswertung** und **integriertem Hilfe-/Glossarsystem**.

---

## 1. Erweiterungen in der Datenerfassung (für bessere Auswertung)

### 1.1 Mieterliste (insb. für Mehrparteienobjekte)
Für Mehrfamilienhäuser (und optional ETW mit mehreren Mietverhältnissen) wird eine **Mieterliste (Rent Roll)** als Pflicht-/Kernmodul eingeführt.

**Anforderungen (fachlich)**
- **Mietobjekte/Einheiten:** Jede Einheit hat eine eindeutige ID (z.B. WE01), Fläche, Zimmer, Lage (EG/OG/DG), Nutzung (Wohnen/Gewerbe/PKW/Stellplatz).
- **Mieterprofil (pro Mietverhältnis):**
  - Mietername/Bezeichnung (auch „anonymisiert“ möglich, z.B. Mieter A)
  - Vertragsbeginn, Vertragsende (optional), Kündigungsfrist (optional)
  - Nettokaltmiete, Nebenkosten-Vorauszahlung, Warmmiete (ableitbar)
  - Mietmodell: unbefristet/befristet, Staffel/Index (Parameter)
  - Kaution (optional), Zahlungsart/Turnus (monatlich)
  - Status: aktiv / gekündigt / leerstehend / im Umbau
- **Historisierung:** Mieterwechsel müssen als Ereignisse (Start/Ende) abbildbar sein (für Cashflow, Leerstand, Kosten, Steuer).
- **Auswertungen aus Mieterliste:**
  - Ist-/Soll-Mieten, Mietniveau pro m², Leerstandsquote, Fluktuation (Turnover), Mietsteigerungspotenzial
  - Laufzeit-/Kündigungsrisiken (z.B. viele befristete Verträge in kurzer Zeit)
  - Szenarien: „Mieterhöhung bei Neuvermietung“, „Leerstand nach Kündigung“, „Mietausfall pro Mieterklasse“
- **Datenschutz:** Mieternamen optional/pseudonymisierbar; fachlich muss die App ohne Klarnamen auskommen.

### 1.2 Teilungserklärung (für ETW/WEG-relevante Bewertung)
Für Eigentumswohnungen (und ggf. Teileigentum) wird ein Modul **Teilungserklärung & Gemeinschaftsordnung** als strukturierte Erfassung eingeführt, weil es direkt Kosten-/Rücklagen-/Rechte-Logik und Risiko beeinflusst.

**Anforderungen (fachlich)**
- Erfassung zentraler Parameter:
  - **Miteigentumsanteil (MEA)** und Bezugsgröße
  - **Sondereigentum / Gemeinschaftseigentum** (relevante Abgrenzungen als Text/Upload + strukturierte Auszüge)
  - **Sondernutzungsrechte** (Stellplatz, Garten, Keller etc.)
  - **Kostenverteilungsschlüssel** (Hausgeld, Instandhaltung, Rücklage, ggf. abweichende Schlüssel)
  - **Regelungen** mit Einfluss auf Vermietbarkeit/Investition (z.B. Kurzzeitvermietung, Tierhaltung, bauliche Veränderungen – als Flags/Notizen)
- Wirkung in der Kalkulation:
  - Hausgeldbestandteile (umlagefähig/nicht umlagefähig) konsistent ableitbar/prüfbar
  - Rücklagenstand (WEG) und Zuführung nach Schlüssel
  - Risiko-/Hinweislogik („Teilungserklärung unklar“, „abweichender Umlageschlüssel“, „Sondernutzungsrechte fehlen“)
- Dokumentenhandling:
  - Upload/Verknüpfung der Teilungserklärung (PDF) + strukturierte Metadaten (für Auswertung/Report).

---

## 2. Hilfe-/Glossarsystem als Pflichtbestandteil (Erfassung & Ergebnis)

### 2.1 Anforderungen an Hilfefunktionen
- **Jeder Begriff** in Eingabe und Ergebnis (Kennzahlen, Steuerbegriffe, Finanzierungsbegriffe, CapEx-Kategorien etc.) muss:
  - per Tooltip/Info-Icon **kurz erklärt** werden (1–3 Sätze)
  - eine **Detailansicht** haben (mehr Kontext, Beispiele, ggf. Formel)
- Hilfetexte sind:
  - **versioniert** (damit Reports nachvollziehbar bleiben)
  - **i18n-fähig** (DE/EN…)
  - **konsistent** über UI, Ergebnis-Dashboard und PDF-Report

### 2.2 FAQ/Gesamtausgabe
- Die App muss eine **FAQ/Gesamtausgabe** erzeugen können:
  - Export als **Markdown/PDF** (konfigurierbar)
  - Inhalte: Glossar + FAQ + ggf. „Wie wird berechnet?“ (High-Level)
  - Filter: nur im Projekt verwendete Begriffe **oder** kompletter Katalog
- Die FAQ-Ausgabe ist **Teil des Revisionskonzepts**:
  - Export enthält `helpVersion` und ggf. `engineVersion`.

---

## 3. Ergänzende Auswertungen (aus den neuen Daten)
- **MFH Rent Roll Auswertung:** Miete pro m², Leerstand, Turnover, Risikoindikatoren
- **ETW/WEG Auswertung:** Hausgeldstruktur, nicht umlagefähige Anteile, Rücklage/MEA, Sondernutzungsrechte in Report
- **Hinweise/Warnungen:** fehlende Teilungserklärung, unvollständige Mieterstruktur, inkonsistente Schlüssel

---

## 4. KI-Unterstützung (fachlich, optional und kontrolliert)

KI ist kein „Blackbox-Rechner“, sondern unterstützt **Datenerfassung, Plausibilisierung und Erklärung**. Fachliche Mindestanforderung: **alle KI-Vorschläge sind transparent, editierbar und haben Quellen/Herleitung**.

Mögliche KI-Unterstützung:
1. **Exposé-/Dokumenten-Extraktion:** Werte aus Exposé, Mietaufstellung, Teilungserklärung, Hausgeldabrechnung in strukturierte Felder übertragen (mit Confidence & Review).
2. **Plausibilitätschecks:** Auffälligkeiten erkennen (z.B. Hausgeld ungewöhnlich hoch, Miete/m² außerhalb Bandbreite, CapEx fehlt trotz hohem Bauteilalter).
3. **Maßnahmenvorschläge:** aus Baujahr/Modernisierungen/Zustand CapEx-Zeitpunkte und Kostenbänder vorschlagen (immer editierbar).
4. **Szenario-Generator:** automatische Downside/Upside-Parameter-Sets erzeugen (z.B. Zinsstress + Leerstand + Preisrückgang).
5. **Erklär-Assistent:** Kennzahlen und Steuerwirkungen in einfacher Sprache erklären („Warum ist CF negativ?“; „Wieso sinkt die Steuerlast?“).
6. **FAQ/Help-Autor:** Hilfetexte konsistent formulieren, Beispiele erzeugen, Übersetzungen vorschlagen (i18n).

---

*Dieses Update ergänzt das bestehende Fachkonzept um Mieterliste, Teilungserklärung und das verpflichtende Hilfe-/FAQ-System.*
