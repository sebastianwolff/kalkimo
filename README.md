# Kalkimo Planner

Realistische, bankstandardnahe Immobilien-Investitionskalkulation für **iOS / Android / Web** – mit **vollständiger Steuerlogik (DE)**, Szenarien, Cashflow/Liquidität, Maßnahmen-/Sanierungslogik, Multi-Investor-Splits und revisionssicherer Versionierung.

> **Security ist Grundlage.** Verschlüsselung, Authentifizierung und Autorisierung sind von Beginn an integraler Teil des Systems.

---

## Highlights

- **Cross-Platform App**: iOS & Android (Ionic + Capacitor) + Web (Browser)  
- **CMS Integration**: Einbettung/Hosting in **GRAV CMS**  
- **Backend**: **C# / ASP.NET Core**  
- **Datenspeicher**: **Flatfile JSON** (verschlüsselt, versioniert)  
- **Security-by-Design**: User-spezifische Verschlüsselung + Break-Glass Master-Key (Audit, 2FA, Rotation)  
- **i18n**: Mehrsprachige UI & lokalisierte Reports  
- **AuthN/AuthZ**: Rollen, Projektfreigaben, **Paid-Entitlements** (Feature-Gates)  
- **Kollaboration**: simultanes Bearbeiten (optimistic concurrency + Live Updates)  
- **Versionierung**: Snapshot + Eventlog (revisionssicher)  
- **Undo/Redo**: Command-basiert (Client) + Gegen-Events (Server)

---

## Produktumfang (fachlich)

- Kauf & Kaufnebenkosten (vollständig)
- Finanzierung (mehrere Darlehen, Zinsbindung, Anschlusszins-Szenarien)
- Mieten & Mietentwicklungen (inkl. Maßnahmenwirkung, Leerstand/Mietausfälle)
- Instandhaltung/Rücklagen, CapEx (tabellarisch) + Sanierungslogik  
- **Nicht-Durchführung notwendiger Maßnahmen → Wertabschläge**
- Szenarien (negativ / stagnierend / positiv) + Stress-Tests
- **Steuerlogik (DE)**: AfA, Werbungskosten, 15%-Regel, §82b EStDV, Verkauf / §23 EStG etc.
- Reports/Exporte (PDF/CSV) mit Versionsbezug

---

## Architektur (Kurz)

**Frontend**
- Vue.js + Ionic UI
- Capacitor für iOS/Android
- i18n via `vue-i18n`
- State Management: Pinia
- Undo/Redo via Command Pattern
- Optional: Offline Cache + Sync Queue

**Backend**
- ASP.NET Core REST API
- Optional WebSocket/SSE für Live Updates
- Rechen-Engine als deterministischer Domain-Layer
- Export-Engine (PDF/CSV)
- Audit Logging & Security Telemetry

**Storage**
- Flatfile JSON (verschlüsselt) pro Projekt:
  - `snapshot_*.json.enc` (Snapshots)
  - `events_*.jsonl.enc` (append-only Eventlog)
  - `exports/*` (Artefakte + Metadaten, Hashes)

**Encryption**
- Envelope Encryption: DEK (pro Projekt) + KEKs (pro User) + Master KEK (Break-Glass)  
- Key Rotation (Master/User/DEK), MFA, Audit, Least Privilege

---

## Repository Struktur (Vorschlag)

```
/apps
  /frontend            # Ionic Vue App (Web + Mobile)
/backend
  /api                 # ASP.NET Core API
  /domain              # Rechen-Engine / Domain Model
  /exports             # Report Renderer (PDF/CSV)
/cms
  /grav                # GRAV Theme/Plugin für Integration
/storage-spec
  /schemas             # JSON Schemas (Snapshots, Events, Reports)
/docs
  fachliches_konzept.md
  technisches_grobkonzept.md
README.md
```

> Hinweis: Struktur kann je nach Teampräferenz abweichen – wichtig ist die klare Trennung von UI, API, Domain und Storage-Spezifikation.

---

## Getting Started (lokal)

### Voraussetzungen
- Node.js (LTS)
- .NET SDK (passend zur Backend-Version)
- (Optional) Docker für lokale Infrastruktur
- iOS/Android Tooling (nur für Mobile Builds)

### Frontend
```bash
cd apps/frontend
npm install
npm run dev
```

### Backend
```bash
cd backend/api
dotnet restore
dotnet run
```

> Standardmäßig sollte das Backend lokal unter `https://localhost:xxxx` laufen; das Frontend nutzt eine konfigurierte API-Base-URL.

---

## Konfiguration

### Umgebungsvariablen (Beispiele)
**Frontend**
- `VITE_API_BASE_URL=...`

**Backend**
- `APP_ENV=Development|Staging|Production`
- `DATA_ROOT=/path/to/encrypted-flatfiles`
- `SECRETS_PROVIDER=Vault|KMS|LocalDev`
- `MASTER_KEK_REF=...` (nur Referenz/Handle, kein Klartext-Key)
- `AUTH_ISSUER=...` (OIDC/OAuth)
- `FEATURE_FLAGS=...` (optional)

> **Wichtig:** Secrets niemals im Repo speichern. Lokale Dev-Secrets über `.env.local` (gitignored) oder Secret Store.

---

## Security

- **Security ist nicht optional.** Änderungen an Auth, Encryption, ACL, Logging erfordern Review.
- Break-Glass / Master-Key Zugriff nur über definierte Prozesse (2FA, Audit, JIT Access).
- Flatfiles sind **immer** verschlüsselt (`*.enc`), Eventlogs append-only.
- CI sollte SAST/Dependency Scans ausführen (z.B. GitHub Advanced Security oder Alternativen).

---

## Paid-Entitlements (Feature Gates)

Einige Funktionen können plan-/lizenzabhängig sein (z.B. Portfolio, Kollaboration, erweiterte Reports).  
**Regel:** Durchsetzung immer serverseitig (AuthZ), UI zeigt nur an/aus.

---

## Dokumentation

- `docs/fachliches_konzept.md`
- `docs/technisches_grobkonzept.md`
- `storage-spec/schemas/*` (Datenformate, JSON-Schema, Versionen)

---

## Contributing

1. Branch von `main` erstellen
2. Kleine, klar abgegrenzte PRs
3. Tests/Checks müssen grün sein
4. Security-relevante Änderungen: zusätzliches Review

---

## Lizenz

TBD – wird im Projekt festgelegt (z.B. proprietär oder Open Source).  
Falls Open Source: `LICENSE` Datei ergänzen.

---

## Disclaimer

Dieses Tool bildet steuerliche und finanzielle Logik parametrisierbar ab, ersetzt jedoch keine individuelle Steuer- oder Rechtsberatung.
