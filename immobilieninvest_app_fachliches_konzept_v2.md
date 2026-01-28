# Fachliches Konzept – App zur realistischen Immobilien-Investitionskalkulation

**Arbeitstitel:** *ImmoInvest Planner*  
**Ziel:** Eine App, die Immobilieninvestitionen über einen Zeitraum (5 / 10–15 / 20 Jahre) **realistisch**, **nachvollziehbar** und **bankstandardnah** kalkuliert – inklusive Cashflow/Liquidität, Finanzierung, Instandhaltung/Sanierung, Miet- und Preis-Szenarien, Multi-Investor-Strukturen sowie **vollständiger Steuerlogik** (DE).

---

## 1. Ausgangslage und Leitprinzipien

### 1.1 Problem
Viele Rechner bilden Immobilieninvestitionen zu stark vereinfacht ab (statische Mieten, pauschale Rücklagen, keine Sanierungszyklen, keine Auswirkung unterlassener Maßnahmen auf den Wiederverkaufswert, keine echte Liquiditätsplanung, unvollständige Finanzierung, fehlende Steuern).

### 1.2 Leitprinzipien (fachlich)
1. **Zeitreihen statt Einmalwerte:** Alle relevanten Größen (Miete, Kosten, Zinsen, Tilgung, Rücklagen, CapEx, Wert, Steuern) werden als **Monats- und Jahresverläufe** modelliert.
2. **Transparente Annahmen:** Jede Berechnung ist auf Annahmen zurückführbar (Audit/Bankgespräch).
3. **Szenarien & Sensitivität:** Basisszenario + Alternativen (negativ, stagnierend, positiv) sowie Stress-Tests.
4. **Trennung von Objekt- und Investorensicht:** Objekt-Cashflows vs. Verteilung auf Investoren (Beteiligung, EK-Anteile, Gesellschafterdarlehen, Ausschüttung).
5. **Realistische Instandhaltung:** Bauteilalter, Zustand, Maßnahmenzyklen, Rücklagenkonten und Konsequenzen bei Aufschub.
6. **Steuern als Renditetreiber:** Steuerwirkungen (laufend und bei Verkauf) sind integraler Bestandteil der Rendite.

---

## 2. Zielgruppen & Rollen

### 2.1 Zielgruppen
- **Gelegenheitsinvestoren:** geführte Eingabe, sinnvolle Defaults, klare Kennzahlen und Warnhinweise.
- **Professionelle Investoren / Bestandshalter:** detaillierte Modellierung (mehr Einheiten, komplexe Mieten, mehrere Darlehen), bankfähige Kennzahlen, Exporte.
- **Berater (Makler, Finanzierer, Verwalter):** strukturierte Datenerfassung, Szenarienvergleich, Reportings.

### 2.2 Rollen in der App
- **Owner/Investor:** legt Projekte an, sieht Ergebnisse.
- **Analyst:** kann Parameter und Szenarien konfigurieren.
- **Viewer:** Leserechte, z.B. Co-Investor oder Bank.

---

## 3. Kern-Use-Cases

1. **Ankaufprüfung:** Kaufpreis, Nebenkosten, Finanzierung, Mieten, Kosten, CapEx, Steuern, Renditekennzahlen.
2. **Sanierung planen:** Maßnahmenpakete inkl. Zeitpunkt, Kosten, Miet- und Wertwirkung, Rücklagen-/Liquiditätswirkung.
3. **Cashflow- & Liquiditätsplanung:** monatliche Liquidität inkl. Rücklagenkonto(n), Sonderkosten, Ausfälle, Steuerzahlungen/-erstattungen.
4. **Szenarien:** Preisverfall/Stagnation/Steigerung, Zinsänderung nach Bindung, Mietentwicklung, Leerstand, CapEx-Schocks (z.B. Mietnomaden).
5. **Multi-Investor:** Beteiligungen, EK-Anteile, Einlagezeitpunkte, Ausschüttungslogik, IRR je Investor.
6. **Bankfähige Auswertung:** DSCR/ICR/LTV, Stress-Tests, nachvollziehbare Herleitungen, exportierbarer Report.

---

## 4. Funktionale Anforderungen

### 4.1 Projektanlage & Zeithorizont
- Auswahl: 5 / 10–15 / 20 Jahre (frei parametrisierbar).
- Startdatum (Monat/Jahr), Zeitauflösung mindestens **monatlich**.
- Währung und Rundungs-/Darstellungsregeln.

### 4.2 Objekttypen & Struktur
- Objekttypen: **Einfamilienhaus**, **Mehrfamilienhaus**, **Eigentumswohnung** (WEG).
- Für MFH/ETW: Einheiten/Flächen, Leerstandslogik pro Einheit möglich.
- Stammdaten: Baujahr, (optional) letzte Modernisierungen je Bauteil, Zustand (gut/mittel/schlecht), Energie-Parameter (optional), WEG-Rücklage (ETW).

### 4.3 Kauf & Kaufnebenkosten (vollständig)
- Kaufpreis, Zahlungszeitpunkt(e) (z.B. mehrere Tranchen).
- Kaufnebenkosten als **Positionsliste** (Notar, Grundbuch, GrESt, Makler, Gutachten, Finanzierungskosten, Sonstiges).
- Zuordnung: aktivierungspflichtig vs. sofortiger Aufwand (steuerlich relevant).

### 4.4 Finanzierung (vollständige Finanzierungsberechnung)
- Eigenkapital: Betrag, Zeitpunkt, Zuordnung zu Investoren.
- Fremdkapital: mehrere Darlehen möglich (Annuität, endfällig, KfW/Nachrang als Typen).
- Parameter je Darlehen: Auszahlung, Zins, Tilgung, Rate, Zinsbindung, Sondertilgung, Tilgungssatzwechsel, Bereitstellungszinsen (optional).
- Anschlussfinanzierung nach Bindungsende als Szenario (Zins/Rate/ Tilgungsvorgaben).
- Ausgabe: Rate, Zins-/Tilgungsanteile, Restschuldverlauf, Gesamtkapitalbedarf.

### 4.5 Mieten & Mietentwicklungen (inkl. Investitionswirkung)
- Mieten: Nettokaltmiete, (optional) Nebenkosten/Bruttomiete, Umlagefähigkeit.
- Mietentwicklung: Rate p.a., Index-Parameter, Staffel (Stufenliste).
- Investitions-/Modernisierungswirkung:
  - Mieterhöhung absolut oder relativ, optional verzögert (Bauzeit).
  - Szenarien „höherer Wohnwert“, „Umstellung Nettomiete statt NK“ (modelliert als Änderung der Zahlungsströme).
  - Begrenzungen/Deckel als Parameter (z.B. markt-/rechtliche Grenzen) – **konfigurierbar**, ohne starre Rechtsannahmen.

### 4.6 Laufende Kosten & nicht absetzbare Kosten
- Betriebskosten getrennt nach:
  - umlagefähig (mieterbezogen),
  - nicht umlagefähig (Cashflow-relevant),
  - Verwaltung, Versicherung, Instandhaltung laufend, Sonstiges.
- Dynamisierung (Inflation p.a. oder frei).
- **Nicht absetzbare Nebenkosten** müssen explizit erfassbar sein und dürfen steuerlich nicht als Werbungskosten wirken (kennzeichnungsbasiert).

### 4.7 Mietausfall, Leerstand, Sonderereignisse
- Leerstand/Mietausfall:
  - Pauschal (% der Nettomiete) und/oder
  - ereignisbasiert (Leerstandsmonate, Bonitätsereignisse).
- Sonderereignisse als Szenario:
  - „Mietnomaden“: zusätzlicher CapEx-Schaden + definierter Mietausfall-Zeitraum.
  - „Versicherung deckt Anteil“ als Parameter (optional).
- Leerstand beeinflusst umlagefähige Kosten (teilweise Eigentümerlast) und Steuerlogik (Einnahmen sinken, Kosten laufen weiter).

### 4.8 Instandhaltung, Rücklagen & Sanierungslogik (inkl. Nicht-Durchführung → Wertabschlag)

#### 4.8.1 Rücklagenkonto (Instandhaltungsrücklage)
- Startstand (z.B. aus WEG-Rücklage vorhanden).
- Periodische Zuführung (fix, €/m², %-basiert).
- Entnahmen durch CapEx/Schäden.
- Mindestschwellen und Warnhinweise (Liquiditäts-/Rücklagenrisiko).

#### 4.8.2 Maßnahmen-/Investitionskostenrechner (tabellarisch)
- Tabellarische Erfassung:
  - Kategorie (Heizung, Dach, Fassade, Fenster, Elektrik, Innenausbau, Energetik, Sonstiges),
  - Zeitpunkt, Kosten, Zahlungsprofil (einmalig/verteilt),
  - Zuordnung: erhaltend vs. wertsteigernd,
  - Mietwirkung (€/Monat oder %), Risiko-Puffer.
- Maßnahmen müssen **in Cashflow**, **Wertentwicklung** und **Steuern** wirken (Erhaltungsaufwand vs. Herstellungskosten/aktivierungspflichtig).

#### 4.8.3 Altersbasierte Maßnahmenvorschläge (Defaults + editierbar)
- Hinterlegte Bauteil-Zyklen und Kostenbänder als Vorschlag (editierbar), z.B.:
  - Heizung 15–25 Jahre, Fenster 20–40, Fassade 25–50, Dach 30–60, Elektrik 30–50.
- Eingaben: Baujahr, letzte Modernisierung je Bauteil, Zustand (gut/mittel/schlecht).
- Output: vorgeschlagene Maßnahmenzeitpunkte + Kostenband + Priorität.

#### 4.8.4 Nicht-Durchführung notwendiger Maßnahmen → Wertabschlag
Wenn eine als notwendig markierte Maßnahme nicht durchgeführt wird:
- Wertabschlag beim Verkauf (und optional im Verlauf) anhand:
  - Überfälligkeit (Jahre über Zyklus),
  - Relevanzklasse (kritisch/hoch/mittel/niedrig),
  - Markteinfluss-Faktor (konfigurierbar).
- Optional: konservativere Mietentwicklung (geringere Steigerung, höhere Ausfallquote).

#### 4.8.5 Sanierungsrisiko-Score (integriert)
- Ampel/Score pro Objekt und Zeitraum basierend auf:
  - Bauteilalter vs. Zyklus,
  - Rücklagenquote und erwartete CapEx-Spitzen,
  - Liquiditätspuffer und DSCR-Stabilität.
- Ziel: Risiken schnell sichtbar machen, ohne Details zu verstecken.

#### 4.8.6 Maßnahmen-Optimierung (integriert)
- Vergleich „jetzt sanieren vs. später sanieren“:
  - Auswirkungen auf NPV/IRR, Liquidität, Vermietbarkeit, Wiederverkaufswert.
- Ergebnis als Empfehlung/Analyse, keine automatische Entscheidung.

### 4.9 Wertentwicklung & Wiederverkauf (Szenariofähig)
- Objektwert als Zeitreihe:
  - Marktpreis-Entwicklung (Szenarien: negativ/stagnierend/positiv, frei definierbar),
  - plus/minus Wertwirkung von Maßnahmen,
  - minus Abschläge bei unterlassenen Maßnahmen.
- Verkauf:
  - Verkaufskosten, Veräußerungszeitpunkt,
  - Restschuldablösung,
  - Netto-Veräußerungsüberschuss,
  - steuerliche Behandlung des Verkaufs (siehe Steuerlogik).

### 4.10 Multi-Investor & Beteiligungen
- Mehrere Investoren:
  - Einlagebeträge (auch zeitversetzt),
  - Beteiligungsquoten (statisch oder kapitalbasiert),
  - optional: Gesellschafterdarlehen (Zins/Tilgung).
- Ergebnisdarstellung je Investor:
  - Einzahlungen, Ausschüttungen, Kapitalkonto,
  - IRR/NPV je Investor,
  - Anteil am Verkaufserlös.
- Zurechnung steuerlicher Effekte je Investor (transparentes Modell für Personengesellschaft/Bruchteilsgemeinschaft).

### 4.11 Liquidität / permanenter Cashflow
- **Monatliche Liquiditätsrechnung**:
  - Einnahmen (Miete, Umlagen, sonstige),
  - Ausgaben (nicht umlagefähig, Verwaltung, Versicherung, CapEx, Rücklagenzuführung),
  - Finanzierung (Zins/Tilgung),
  - Steuern (laufend & bei Verkauf, zeitlich zuordenbar),
  - Kontostand/Buffer.
- **Liquiditäts-Puffer-Simulator**:
  - Ermittelt, welcher Start-/Sicherheits-Puffer nötig ist, um definierte Mindestliquidität einzuhalten (z.B. nie < 0 € oder < X €).

---

## 5. Datenmodell (fachlich)

### 5.1 Entitäten (vereinfacht)
- **Projekt** (Zeitraum, Start, Währung, Szenarien)
- **Objekt** (Typ, Baujahr, Zustand, Flächen, Einheiten, WEG-Rücklage)
- **Kauf** (Kaufpreis, Nebenkostenpositionen, Zahlungszeitpunkte)
- **Finanzierung** (Darlehen 1..n, EK, Sondertilgungen, Anschlusszins-Szenarien)
- **Mieten** (Einheiten, Entwicklung, Umlage, Investitionswirkung, Ausfälle)
- **Kosten** (uml./nicht uml., Verwaltung, Versicherung, sonstige; absetzbar-Flag)
- **CapEx/Maßnahmen** (Kategorie, Kosten, Timing, Steuerklassifikation, Wert-/Mietwirkung)
- **Rücklagenkonto** (Stand, Zuführung, Entnahmen)
- **Investoren** (Quoten, Einlagen, Darlehen, Ausschüttung, steuerliche Zurechnung)
- **Steuerprofil** (Haltungsform, Grenzsteuersatz/Parameter, AfA-Parameter, §82b-Option, §23-Parameter)

### 5.2 Zeitreihen-Konzept
- **Zeitauflösung:** Monatlich (Liquidität/Finanzierung) + Aggregation auf Jahr (Reporting/Kennzahlen).
- Jede relevante Größe als Zeitreihe: Mieten, Kosten, Zins/Tilgung, CapEx, Rücklagen, Steuern, Cashflow, Restschuld, Objektwert, Eigenkapital.

---

## 6. Berechnungslogik (fachlich)

### 6.1 Kauf- und Nebenkosten
- Nebenkosten als Positionen, Zuordnung zu Zahlungszeitpunkten.
- Steuerliche Zuordnung (aktivierungspflichtig vs. sofort abziehbar) muss abbildbar sein.

### 6.2 Finanzierung (bankstandardnah)
- Annuitätendarlehen: monatliche Rate, Zins-/Tilgungsanteile, Restschuld.
- Mehrdarlehensfähigkeit, Sondertilgung, Tilgungssatzwechsel, Anschlussfinanzierung.
- Kennzahlen:
  - **DSCR**: NOI / Debt Service
  - **ICR**: NOI / Zins
  - **LTV**: Restschuld / Objektwert
  - Break-Even-Miete, Stress-Tests (Zins/Miete/Leerstand)

### 6.3 Mietmodell inkl. Investitionswirkung
- Miete als Zahlungsstrom, Entwicklung (Rate/Index/Staffel).
- Investitionsbedingte Mieterhöhungen und Strukturwechsel (Nettomiete/NK) als Szenario.

### 6.4 Kostenmodell (inkl. absetzbar/nicht absetzbar)
- Jede Kostenposition hat Flags:
  - umlagefähig ja/nein,
  - steuerlich absetzbar ja/nein,
  - Dynamisierung.
- Leerstand beeinflusst Kostenlast.

### 6.5 Mietausfall/Leerstand
- Pauschal- und Ereignismodelle.
- Ereignisse beeinflussen Cashflow, Steuern (Einnahmen↓, Werbungskosten meist weiter), Rücklagen und ggf. CapEx.

### 6.6 Instandhaltung & CapEx
- Bauteil-Zyklen, Vorschläge, Maßnahmen-Tabelle.
- Verknüpfung: Cashflow ↔ Rücklage ↔ Wertentwicklung ↔ Steuern.
- Nicht-Durchführung → Wertabschlag.

### 6.7 Rücklagenkonto
- Periodische Zuführung, Entnahmen, Kontostand, Mindestschwellen.

### 6.8 Wertentwicklung & Verkauf
- Szenarien für Marktpreise, Wertwirkung von Maßnahmen, Abschläge.
- Verkaufskosten, Restschuldablösung, Nettoerlös, steuerliche Behandlung.

### 6.9 Steuerlogik (Pflicht, DE) – integraler Bestandteil der Rendite

> Ziel: Das Tool berechnet Renditekennzahlen **nach Steuern** und zeigt die Treiber (AfA, Zinsen, Erhaltungsaufwand, anschaffungsnahe HK, Verkauf) transparent.

#### 6.9.1 Steuerliche Grundlogik & Profile
- Auswahl **Steuerprofil/Haltungsform**:
  - Privatperson (Einkünfte aus Vermietung und Verpachtung),
  - Personengesellschaft/Bruchteilsgemeinschaft (transparent, Zurechnung je Investor),
  - Kapitalgesellschaft (parameterbasiert; Steuersätze konfigurierbar).
- Eingaben: persönlicher Grenzsteuersatz/Steuerquote (oder vereinfachtes Steuerprofil), Solidarität/KiSt als Parameter (optional), Verlustverrechnung als Parameter (vereinfachter Modus).

#### 6.9.2 Laufende Besteuerung bei Vermietung
- Einnahmen: Nettokaltmiete (und ggf. umlagefähige NK als Durchlaufposten modellierbar).
- Werbungskosten (Beispiele als vordefinierte Kategorien):
  - Schuldzinsen und Finanzierungskosten,
  - nicht umlagefähige Betriebskosten,
  - Verwaltung, Versicherungen,
  - Erhaltungsaufwand,
  - AfA,
  - ggf. Vorsteuer nur bei umsatzsteuerpflichtiger Vermietung (optional).  
  (Werbungskosten-Logik und Anlage-V-Struktur sind als Referenz abbildbar.) citeturn1search1turn0search11turn0search7

#### 6.9.3 AfA (Gebäudeabschreibung) – korrekt zeitlich und sachlich
- Trennung **Bodenwert vs. Gebäudewert** (Boden nicht abschreibbar; Eingabe über Kaufpreisaufteilung oder Gutachten/Schätzer).
- Standard-AfA nach Fertigstellungszeitraum:  
  - nach 31.12.2022 fertiggestellt: **3% p.a.**  
  - vor 01.01.2023 und nach 31.12.1924: **2% p.a.**  
  - vor 01.01.1925: **2,5% p.a.** citeturn0search8
- Option „kürzere tatsächliche Nutzungsdauer“ (Gutachten): Anwender kann alternativ eine kürzere Nutzungsdauer hinterlegen, die AfA erhöht. citeturn1search2turn0search16
- AfA-Start (Anschaffung/Fertigstellung) muss zeitlich korrekt abgebildet werden (Monatsscheibe).

#### 6.9.4 Anschaffungsnahe Herstellungskosten (15%-Regel) & Maßnahmenklassifikation
- Regel muss abgebildet werden: Aufwendungen für Instandsetzung/Modernisierung **innerhalb von 3 Jahren** nach Anschaffung, die netto **> 15%** der Gebäude-Anschaffungskosten betragen, gelten als anschaffungsnahe Herstellungskosten (aktivierungspflichtig, AfA statt Sofortabzug). citeturn0search2turn0search6turn0search14
- Maßnahmenpositionen müssen steuerlich klassifizierbar sein:
  - Erhaltungsaufwand (sofort/verteilt),
  - Herstellungskosten/Erweiterungen (aktivierungspflichtig),
  - anschaffungsnahe HK (regelbasiert).
- Die App muss Warnungen ausgeben, wenn die 15%-Schwelle gerissen wird (inkl. Erklärung der Wirkung auf Steuer und Cashflow).

#### 6.9.5 Verteilung größerer Erhaltungsaufwendungen (§82b EStDV)
- Option, größeren Erhaltungsaufwand gleichmäßig auf **2 bis 5 Jahre** zu verteilen (wahlweise), mit Auswirkungen auf Steuer, Cashflow nach Steuern und Kennzahlen. citeturn1search16turn1search0

#### 6.9.6 Verkauf / Spekulationssteuer (§23 EStG) & Freigrenze
- Abbildung des privaten Veräußerungsgeschäfts:
  - **10-Jahres-Frist** zwischen Anschaffung und Veräußerung (steuerfrei nach Ablauf; Ausnahmen z.B. Eigennutzung parametrisierbar). citeturn0search1turn0search9
  - Freigrenze für Gewinne aus privaten Veräußerungsgeschäften: **1.000 €** (seit 01.01.2024), als Freigrenze (nicht Freibetrag). citeturn1search3turn1search11
- Verkaufsergebnis muss steuerlich hergeleitet werden:
  - Verkaufspreis minus (fortgeschriebene) Anschaffungs-/Herstellungskosten minus Verkaufskosten,
  - AfA-Effekte in der Buchwertlogik (vereinfachbar, aber transparent).
- Für andere Haltungsformen (z.B. gewerblich/kapitalgesellschaftlich) muss die App einen separaten Modus bieten (parameterbasiert), da §23 EStG dann ggf. nicht greift.

#### 6.9.7 Ergebnisdarstellung Steuern
- **Vor-Steuer** und **Nach-Steuer** Kennzahlen:
  - IRR, NPV, Cash-on-Cash, Equity Multiple, Rendite p.a.
- Steuer-Bridge:
  - Darstellung, welche Positionen die Steuerlast treiben (AfA, Zinsen, Erhaltungsaufwand, 15%-Regel, §82b-Verteilung, Verkauf).
- Ausgabe als Report-Teil “Steuerliche Sicht” (prüfbar, mit Parametern).

---

## 7. Szenarien & Visualisierung (zeitlich klar erkennbar)

### 7.1 Szenarien (vordefiniert + frei)
- Immobilienpreise: negativ/stagnierend/positiv (frei definierbar).
- Mieten: stagnierend/moderat/optimistisch, inkl. Maßnahmenwirkung.
- Zins: Anschlusszins-Szenarien nach Bindung.
- Leerstand/Mietausfall: niedrig/mittel/hoch, plus Ereignisse.
- CapEx-Schocks: z.B. Mietnomaden-Schaden + Mietausfall.

### 7.2 Visualisierungen
- Cashflow (monatlich) + kumulierter Cashflow.
- Liquidität & Rücklagenkonto.
- Restschuld vs. Objektwert (LTV).
- NOI / Kosten / Debt Service / Steuern (Stack oder Linien).
- Maßnahmen-Timeline (Gantt-ähnlich) inkl. steuerlicher Einordnung.
- Szenariovergleich als Overlay (mehrere Kurven) + Bandbreiten.
- Portfolio-Stresstest (wenn mehrere Objekte): kombinierte Kurven und Worst-Case-Analyse.

---

## 8. Kennzahlen & Ergebnisse (bankstandardnah + investorentauglich)

- Renditekennzahlen: IRR (Objekt & je Investor), NPV, ROI, Equity Multiple, Cash-on-Cash.
- Bankkennzahlen: DSCR, ICR, LTV, Break-Even-Miete.
- Risikoindikatoren: Sanierungsrisiko-Score, Liquiditätsunterdeckung, DSCR-Stress.
- Ergebnisse immer als Zeitreihen + aggregierte Summary (Jahr/gesamt).

---

## 9. Reporting & Exporte

### 9.1 Bank-Report (PDF)
- Kauf & Nebenkosten (vollständig), Gesamtkapitalbedarf.
- Finanzierung: Darlehenspläne, Restschuld, Zinsbindung, Szenarien.
- NOI/CF-Rechnung (jährlich) + monatliche Liquidität (Zusammenfassung).
- DSCR/ICR/LTV über Zeit + Stress-Tests.
- Maßnahmen- und Rücklagenlogik + “Nicht-Durchführung”-Konsequenzen.
- Steuerteil: AfA, Werbungskosten, 15%-Regel, §82b, Verkaufsteuerlogik.

### 9.2 Exportformate
- PDF (Bank/Investor), CSV (Zeitreihen), optional Excel-Template-Export.
- “Bank-Mapping”: Exportlayout konfigurierbar, um bankinterne Templates zu bedienen.

---

## 10. Eingabe-UX (professionell + einfach)

### 10.1 Guided Mode (Gelegenheitsinvestor)
- Schritt-für-Schritt: Objekt → Kauf → Finanzierung → Miete/Kosten → Maßnahmen → Steuern → Szenarien → Ergebnis.
- Gute Defaults, klare Tooltips, Warnhinweise (z.B. Rücklage negativ, 15%-Regel triggert, DSCR kritisch).

### 10.2 Pro Mode (Profi)
- Tabellen- und Detailansicht, Mehrdarlehen, Einheitenverwaltung, Szenariomatrix, Im-/Export.

### 10.3 Validierungen & Plausibilität
- Konsistenzprüfungen (Tilgung/Rate, Cashflow-Logik, Steuerklassifikation).
- Hinweislogik (nicht blockierend), inkl. “Warum ist das wichtig?”-Erklärungen.

---

## 11. Nicht-funktionale Anforderungen

- **Nachvollziehbarkeit:** Jede Kennzahl klickbar → Herleitung, verwendete Annahmen.
- **Revisionssicherheit:** Versionierung von Szenarien, Änderungsprotokoll (wer/was/wann).
- **Performance:** Projekte bis z.B. 200 Einheiten / 20 Jahre monatlich.
- **Datenschutz:** lokale Projekte + Cloud optional; DSGVO, Verschlüsselung.
- **Exportfähigkeit:** PDF/CSV/optional Excel; konsistente Rundung.
- **Hinweis zur Einordnung:** Das Tool liefert Berechnungen auf Basis eingegebener Parameter und gesetzlicher Modelllogik, ersetzt jedoch keine individuelle Steuer-/Rechtsberatung.

---

## 12. Definition der wichtigsten Outputs (was “gut” heißt)

Die App gilt fachlich als erfolgreich, wenn sie pro Projekt liefert:
- **Monatliche Liquiditätsrechnung** inkl. Rücklagenkonto und steuerlicher Zahlungswirkung.
- **Maßnahmenplan** mit Auswirkungen auf Miete, Wert, Steuern und Risiko.
- **Downside/Upside-Szenarien** (inkl. negativer Preisentwicklung) im direkten Vergleich.
- **Wiederverkaufswert** inkl. Abschlägen bei unterlassenen Sanierungen + steuerliche Verkaufsauswertung.
- **Bankkennzahlen** (DSCR/ICR/LTV) über Zeit inkl. Stress-Tests.
- **Investorensicht** mit IRR/NPV und Cashflow-/Steuer-Zurechnung.

---

*Ende des fachlichen Konzepts (Markdown).*
