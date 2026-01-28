# Technisches Grobkonzept – ImmoInvest Planner (Update)

Dieses Update ergänzt das bestehende technische Grobkonzept um:
- **Mieterliste (Rent Roll)** als strukturierte Domänenentität inkl. Historisierung
- **Teilungserklärung/WEG-Modul** inkl. Dokumentenhandling und Metadaten
- **Help/Glossar/FAQ-System** (i18n, versioniert, exportierbar)
- Einordnung, **wo KI sicher & sinnvoll unterstützen kann** (ohne Security/Transparenz zu gefährden)

---

## 1. Datenmodell-Erweiterungen

### 1.1 Mieterliste (Rent Roll)
**Entitäten**
- `Unit` (Einheit): id, typ, fläche, zimmer, lage, nutzung, meta
- `Tenancy` (Mietverhältnis): id, unitId, tenantLabel (pseudonym), startDate, endDate?, rentNet, nkVZ, indexOrStepModel?, deposit?, status
- `Tenant` optional (wenn getrennt): tenantId, label, tags (z.B. „Gewerbe“), datenschutzfreundlich

**Historisierung**
- Mietverhältnisse sind **zeitlich begrenzt** (Start/Ende) und werden nicht überschrieben.
- Änderungen (Mieterhöhung, Index-Anpassung) laufen als **Events** (JSONL) und erzeugen neue `projectVersion`.

**Auswertung**
- Backend-Engine erzeugt aggregierte Kennzahlen aus Rent Roll (Miete/m², Leerstand, Turnover).
- In Reports werden Einheiten optional anonymisiert ausgegeben.

### 1.2 Teilungserklärung/WEG (ETW)
**Entitäten**
- `CondominiumDeclaration` (Teilungserklärung-Meta):
  - `mea`, `meaBase`, `costKeys` (Hausgeld/Rücklage/sonstige), `specialUseRights[]`, `restrictionsFlags`, `notes`
- `Documents`:
  - Upload `pdf` (verschlüsselt), Metadaten (hash, uploadAt, version)

**Dokumenten-Speicherung**
- Dokumente werden wie Projektdaten verschlüsselt gespeichert (DEK pro Projekt).
- Optional: separate `documentDEK` (bei sehr großen Dateien), weiterhin per KEKs eingehüllt.

---

## 2. Help/Glossar/FAQ-System (Pflicht)

### 2.1 Anforderungen (technisch)
- **Jedes UI-Feld und jede Kennzahl** referenziert einen `helpKey`.
- Help Content ist **i18n-fähig** und **versioniert** (damit Exporte reproduzierbar bleiben).
- Help Content kann:
  - als Tooltip (Kurztext)
  - als Detailseite (Langtext, Beispiele, ggf. Formel)
  - als „Related Topics“ (Verlinkung) bereitgestellt werden.

### 2.2 Content-Storage
- Help Content als separate, versionierte Ressourcen, z.B.:
  - `/help/{locale}/help_v{n}.json.enc`
  - Struktur:
    - `helpKey`
    - `short`, `long`
    - `examples[]`
    - `formula?`
    - `tags[]`
    - `relatedHelpKeys[]`

> Wichtig: Help Content wird ebenfalls verschlüsselt gespeichert (insb. falls proprietäre Berechnungslogik erklärt wird) und über Entitlements gesteuert, wenn nötig.

### 2.3 API (Beispiele)
- `GET /help/catalog?locale=de-DE&version=vN`
- `GET /help/item/{helpKey}?locale=de-DE&version=vN`
- `POST /exports/faq` (Markdown/PDF)
  - Parameter: `mode=all|usedInProject`, `projectId?`, `locale`, `helpVersion`

### 2.4 “FAQ Gesamtausgabe” (Export)
- Export-Engine erzeugt:
  - Markdown (für GitHub/Website)
  - PDF (für Bank/Investor)
- Export-Metadaten enthalten: `helpVersion`, `engineVersion`, `projectVersion` (falls projektbezogen).

---

## 3. Kollaboration, Versionierung & Undo/Redo – Auswirkungen durch neue Module

### 3.1 Versionierung
- Rent Roll und Teilungserklärung sind Teil des Snapshot + Eventlog.
- Änderungen an Rent Roll (z.B. Mieterhöhung) sind **domain-spezifische Commands** (besser als freier JSON Patch), um Validierung und Undo zu erleichtern.

### 3.2 Undo/Redo
- Client Command Pattern bleibt; neue Commands:
  - `AddUnit`, `UpdateUnit`, `AddTenancy`, `EndTenancy`, `ApplyRentIncrease`, `AttachDocument`, `UpdateDeclarationMeta`
- Server-Undo in Kollaboration: Gegen-Events (revisionssicher).

---

## 4. KI-Unterstützung (technische Einordnung)

### 4.1 Grundsätze
- KI ist **assistierend**, niemals alleiniger Entscheider.
- Outputs werden als **Vorschläge** geliefert, mit:
  - Confidence/Unsicherheit
  - Quellen/Belegstellen (bei Dokument-Extraktion: Seiten/Abschnitt)
  - “Apply”-Mechanismus (User bestätigt vor Persistenz)
- Security/Privacy:
  - standardmäßig **kein** Versand sensibler Daten an Drittanbieter ohne ausdrückliche Konfiguration/Einwilligung.
  - bevorzugt: on-prem/privat gehostete Modelle oder datenschutzkonforme Anbieter.

### 4.2 KI-Use-Cases (technisch)
1. **Document Parsing Pipeline**
   - Upload → Virenscan (optional) → OCR/Parsing (falls nötig) → Extraktion in strukturierte Vorschläge
   - UI: Side-by-side Review (Quelle links, Felder rechts)
2. **Plausibilitäts- & Anomalie-Checks**
   - Regelbasierte Checks + KI für Textinterpretation (z.B. Hausgeldbestandteile aus Abrechnung)
3. **Maßnahmen-/CapEx-Vorschläge**
   - Hybrid: deterministische Regeln (Zyklen) + KI für fehlende Infos („Fenster vermutlich alt“ aus Exposé-Text)
4. **Erklär-Assistent**
   - RAG über internes Help/Glossar, um konsistente Erklärungen zu liefern
5. **FAQ/Help-Autoring**
   - KI generiert/übersetzt Help-Texte, aber Veröffentlichung nur nach Review (Versionierung!)

---

*Ende des technischen Updates.*
