# Technisches Grobkonzept – ImmoInvest Planner (iOS/Android/Web)  
**Stand:** Grobkonzept (Architektur, Sicherheits- und Plattformentscheidungen)  
**Ziel:** Ein einheitlicher Code- und Datenkern für Mobile (Apple/Android) und Web (Browser, eingebettet in Website/CMS), mit Security-by-Default, i18n, AuthN/AuthZ, Kollaboration, Versionierung sowie Undo/Redo.

---

## 1. Zielarchitektur (High Level)

### 1.1 Plattformen
- **Mobile App:** iOS & Android via **Ionic + Capacitor** (Vue.js UI).
- **Web App:** identische SPA/PWA (Vue.js + Ionic Komponenten) im Browser.
- **Website-Integration:** Einbindung in **GRAV CMS** (z.B. als SPA-Route oder eingebettetes App-Frontend via Theme/Plugin).

### 1.2 Kernprinzipien
- **Security ist Grundlage:** durchgängige Verschlüsselung, least privilege, sichere Defaults, auditierbar.
- **Single Source of Truth:** Backend liefert autoritative Daten & Versionen; Client hält lokale Caches.
- **Event- und Versionsorientierung:** Alle Änderungen sind versioniert, nachvollziehbar und (soweit möglich) rückgängig machbar.


---

## 2. Komponenten & Verantwortlichkeiten

### 2.1 Frontend (Vue.js / Ionic)
**Aufgaben**
- UI/UX (Guided/Pro Mode)
- i18n (mehrsprachige UI + Formatierung)
- Offline Cache & Sync-Queue (optional)
- Undo/Redo auf Projekt-/Formular-Ebene
- Szenario-/Report-Visualisierungen (Charts)

**Technische Bausteine**
- Ionic Vue + Capacitor (Mobile) + Web Build
- State Management: Pinia/Vuex (Pinia empfohlen)
- i18n: vue-i18n, CLDR/Intl für Zahlen/Währungen/Datum
- Form Engine: schemabasiert (JSON-Schema) für dynamische Eingabemasken (Vorteil: schnell erweiterbar, validierbar)
- PDF: QuestPDF für PDF Ausgaben
- Diagramme: z.B. ECharts/Chart.js (ausreichend für Zeitreihen)

### 2.2 GRAV CMS Integration
**Ziel:** App als „Produkt“ innerhalb Website nutzen, inkl. Single Sign-On Option.

**Integration-Optionen**
1. **SPA als eigenständige Route** im GRAV Theme (empfohlen):  
   - `https://domain.tld/app` → lädt gebaute SPA Assets (aus `user/themes/.../dist`)
2. **Embedded iFrame / Web Component** für Teil-Integration:  
   - sinnvoll für einzelne Widgets (z.B. Quick-Check), weniger für Voll-App
3. **SSO/Session Bridge (optional):**  
   - GRAV Login kann via OIDC/OAuth Provider an Backend gekoppelt werden (empfohlen: *ein* zentrales Auth-System, nicht doppelt pflegen)

### 2.3 Backend (C#)
**Technologie:** ASP.NET Core (REST) + optional WebSocket/SSE für Kollaboration  
**Aufgaben**
- Authentifizierung (inkl. 2FA)
- Autorisierung (User/Projekt/Rollen, Paid-Entitlements)
- Projekte, Versionierung, Kollaboration (Locking/Conflicts)
- Berechnungs-Engine (Deterministik, Testbarkeit)
- Export-Engine (PDF/CSV), Ausgabe-Versionen
- Audit Logging & Security Telemetry

### 2.4 Datenspeicher: Flatfile JSON (verschlüsselt)
**Randbedingungen**
- Flatfile JSON als Persistenz (kein klassisches DBMS).
- Trotzdem: konsistente Versionen, schnelle Lesezugriffe, sichere Speicherung.

**Empfohlene Struktur**
- Pro Tenant/User eine Root-Struktur:  
  - `/data/{tenantId}/users/{userId}/...`
- Pro Projekt ein Ordner:  
  - `/projects/{projectId}/`
    - `snapshot_{n}.json.enc` (verschlüsselte Snapshots)
    - `events_{yyyymm}.jsonl.enc` (append-only Änderungslog; JSON Lines)
    - `exports/{exportId}/...` (PDF/CSV + Meta)
    - `meta.json.enc` (Metadaten, Schlüssel-Info, ACL, Versionstempel)

**Warum Snapshot + Eventlog?**
- Versionierung & Audit werden “natürlich”.
- Undo/Redo kann auf Event-Ebene arbeiten.
- Konflikte beim simultanen Bearbeiten sind beherrschbarer (siehe Kollaboration).

---

## 3. Security-by-Design (kein Feature, sondern Basis)

### 3.1 Bedrohungsmodell (Kurz)
- Schutz vor Datenabfluss (at rest/in transit)
- Schutz vor Account-Übernahme
- Schutz vor unautorisierten Projektzugriffen (inkl. Paid Features)
- Manipulationsschutz (Integrität der Versionen/Exports)
- Notfallzugriff (Master Key) mit strengen Controls & Audit

### 3.2 Verschlüsselungsmodell (Envelope Encryption)
**Ziel:** User-spezifische Verschlüsselung, aber über Master Key auflösbar.

**Empfohlenes Modell**
- **Data Encryption Key (DEK) pro Projekt** (symmetrisch, z.B. AES-256-GCM)
- **Key Encryption Keys (KEK)**:
  - pro User ein KEK (ableitbar aus serverseitig verwaltetem Secret + user-specific Material)
  - zusätzlich ein **Master KEK** (HSM/KMS/Secret Vault, getrennt verwaltet)
- Speicherung:
  - Projektdateien sind mit DEK verschlüsselt (`*.enc`)
  - DEK wird mehrfach “eingehüllt”:  
    - `DEK_user_{userId}` (für berechtigte User)  
    - `DEK_master` (für Notfallzugriff)
- Integrität:
  - AEAD (GCM) + zusätzliche Hash/Signatur pro Artefakt (optional)  
  - Export-Artefakte mit Checksums und ggf. Signatur

**Master Key Zugriff**
- Nur über “Break-Glass”-Prozess:  
  - 2FA + getrennte Rollen (4-Augen-Prinzip)  
  - Audit-Log, Alarmierung, zeitlich begrenzte Freigabe (JIT Access)
- Master Key liegt **nicht** im App-Code, sondern im Secret Management.

### 3.3 Key Rotation
- **Master KEK Rotation:** neue Version im Vault; alte bleibt zum Entschlüsseln bis Re-Encrypt erledigt ist.
- **User KEK Rotation:** bei Passwortänderung/Compromise; Rewrap aller DEKs für User.
- **DEK Rotation:** optional pro Projektversion oder periodisch; neues DEK, Re-Encrypt der Snapshots (Events können schrittweise migriert werden).

### 3.4 2FA / MFA
- TOTP (Authenticator App) als Minimum; optional WebAuthn/Passkeys.
- Recovery Codes (verschlüsselt, einmalig sichtbar).
- Risk-based Checks (neues Gerät, neue IP) → MFA erzwingen.

### 3.5 Transport & Plattform-Security
- TLS 1.2+ (besser 1.3), HSTS, Secure Cookies
- Mobile: Secure Storage (Keychain/Keystore) für Refresh Tokens/Keys
- Browser: HttpOnly Cookies + CSRF Schutz (wenn Cookie-basierte Sessions); oder OAuth PKCE mit Token-Speicher im Memory

### 3.6 Audit & Monitoring (Pflicht)
- Audit-Events: Login, MFA, Projektfreigaben, Export-Generierung, Master-Key Zugriff, Rechteänderungen
- Security Logs getrennt von Business Logs
- Alarmierung (z.B. auffällige Logins, viele fehlgeschlagene MFA)

---

## 4. Authentifizierung & Autorisierung

### 4.1 Auth System
**Empfohlen:** OAuth2/OIDC Pattern (intern implementiert), mit:
- Access Token (kurzlebig)
- Refresh Token (rotierend, widerrufbar)
- Session Management (Logout everywhere, device list)

### 4.2 Autorisierung
**Ebenen**
1. **User-Level Access:** Projekteigentümer, Rollen (Owner/Editor/Viewer)
2. **Paid-Functions / Entitlements:** Feature Flags pro User/Plan  
   - z.B. „Multi-Investor“, „Portfolio“, „Bankreport-Export“, „Kollaboration“, „Erweiterte Steuerlogik-Details“
3. **Object-Level ACL:** pro Projekt und optional pro Export/Report

**Durchsetzung**
- Server-side Enforcement (nie nur UI)
- Claims/Entitlements im Token (kurzlebig), Quelle bleibt Backend

---

## 5. Kollaboration (simultan bearbeiten)

### 5.1 Freigabe- und Rollenmodell
- Projektfreigabe: Einladen per E-Mail/Account
- Rollen:
  - **Owner:** Rechteverwaltung, Master-Einstellungen, Löschen
  - **Editor:** Eingaben ändern, Szenarien bearbeiten, Exporte erzeugen
  - **Viewer:** Lesen, Exporte ansehen (optional eingeschränkt)

### 5.2 Technische Kollaboration (Konzept)

1. **Optimistic Concurrency + Live Presence (empfohlen fürs Grobkonzept)**  
   - Client lädt `baseVersion`
   - Änderungen werden als **Patch-Events** (JSON Patch / eigene Command-Struktur) an Server gesendet
   - Server prüft: passt `baseVersion`?  
     - ja → Event wird angehängt, neue `projectVersion` entsteht  
     - nein → Conflict → Client bekommt Rebase/Conflict-Info
   - Optional: WebSocket/SSE für “live updates” + “wer ist drin”-Anzeige

### 5.3 Konfliktstrategie (Minimum)
- Feldbasierte Konflikte mit UI-Auflösung:
  - “Server-Wert behalten” / “Mein Wert überschreiben” / “Manuell”
- Automatisches Merge für nicht überlappende Änderungen

---

## 6. Versionierung & Revisionssicherheit

### 6.1 Versionierung der erfassten Daten
- Jede Änderung erzeug Undo-fähige Events im **append-only Eventlog**.
- Regelmäßige Snapshots (z.B. alle N Events oder zeitbasiert) zur Performance.
- Jede Version hat:
  - `projectVersion` (monoton)
  - Timestamp, UserId, ChangeSetId
  - Hash des resultierenden Snapshots (Integritätskette optional)

### 6.2 Versionierung der Dateiausgaben (Exports)
- Exporte (PDF/CSV) werden als Artefakte gespeichert:
  - Metadaten: verwendete `projectVersion`, Szenario-ID, Template-Version, Render-Parameter, Hash
- Reproduzierbarkeit:
  - Export muss im Idealfall aus Snapshot+Parameter reproduzierbar sein (Determinismus).

---

## 7. Undo/Redo (Einzel- und Multiuser)

### 7.1 Clientseitig (UX)
- Command Pattern: jede UI-Änderung = Command (do/undo).
- Undo Stack pro Projekt-Sitzung.
- Für komplexe Forms: Debounce & “Change Grouping” (z.B. Tippen in Textfeld als eine Aktion).

### 7.2 Server-/Kollaborationsebene
- In Kollaboration wird Undo/Redo anspruchsvoller:
  - **Option A (pragmatisch):** Undo wirkt nur auf eigene letzte Änderungen (per user ChangeSetId) und erzeugt Gegen-Event.
  - **Option B (global):** “Time travel” auf Projektversion (nur Owner) – erzeugt neue Version, die zu einem früheren Snapshot zurückkehrt (ohne History zu löschen).
- Wichtig: Undo darf Historie nicht löschen (Revisionssicherheit), sondern Gegenänderungen schreiben.

---

## 8. i18n & Lokalisierung

- Alle UI-Strings in Translation-Files (z.B. JSON).
- Formatierung:
  - Zahlen/Währungen (€, CHF, etc.), Datum, Prozent, Tausendertrennungen.
- Inhalte in Reports:
  - Templates sprachabhängig (PDF), inkl. Legenden/Begriffe.
- Backend:
  - Locale-Parameter in Requests (z.B. `Accept-Language`)
  - Berechnungen unabhängig von Locale (nur Darstellung lokalisieren)

---

## 9. Berechnungs-Engine & Domänenlogik

### 9.1 Aufbau
- C# “Domain”-Layer als reine Business-Logik (ohne IO), deterministisch.
- Input: Snapshot + Szenario-Parameter
- Output: Zeitreihen + Kennzahlen + Diagnosen (Warnings, Plausibilitäten)

### 9.2 Determinismus & Testbarkeit
- Keine “current time” ohne injizierten Clock-Service.
- Rechenkerne mit Golden-Tests (Referenzfälle).
- Versionierung der Berechnungslogik (Engine-Version) zur Reproduzierbarkeit älterer Exporte.

---

## 10. API (Grob)

### 10.1 Auth
- `POST /auth/login`
- `POST /auth/mfa/verify`
- `POST /auth/refresh`
- `POST /auth/logout`

### 10.2 Projekte
- `GET /projects`
- `POST /projects` (create)
- `GET /projects/{id}` (snapshot + meta)
- `POST /projects/{id}/events` (append changeset)
- `GET /projects/{id}/events?since=...` (sync)
- `POST /projects/{id}/share` (invite, role)
- `POST /projects/{id}/locks` (optional soft-lock)

### 10.3 Exporte
- `POST /projects/{id}/exports` (generate)
- `GET /projects/{id}/exports`
- `GET /projects/{id}/exports/{exportId}`

### 10.4 Entitlements / Billing Hooks
- `GET /me/entitlements`
- optional Webhook/Job, um Payed Features zu aktualisieren

---

## 11. Deployment, Betrieb, Wartung

### 11.1 Deployment-Option
- Backend: Container (Docker), Reverse Proxy (Nginx/Traefik).
- Flatfile Store:
  - lokales Volume oder S3-kompatibler Object Store (trotz “Flatfile” möglich)  
  - Vorteil: Backups, Versioning, Lifecycle Policies
- Secrets: Vault/KMS (z.B. HashiCorp Vault, Azure Key Vault, AWS KMS) – je nach Infrastruktur.

### 11.2 Backups & Restore (Pflicht)
- Verschlüsselte Backups mit getrennten Keys.
- Restore-Prozesse getestet (Disaster Recovery).
- “Break-glass” dokumentiert und geprüft.

### 11.3 CI/CD
- Frontend: Build Web + Mobile Bundles
- Backend: Tests (Unit/Integration/Security), SAST/Dependency Scans
- Signierung: iOS/Android Release Pipeline
- Versionierung: SemVer für API/Frontend/Engine

---

## 12. Datenformate & Änderungsmodell (für Flatfile geeignet)

### 12.1 Snapshot
- `snapshot.json` enthält vollständigen Zustand eines Projektes (stark typisiert, aber JSON).
- Separater Schema-Header:
  - `schemaVersion`, `engineVersion`, `createdAt`, `updatedAt`

### 12.2 Eventlog (JSONL)
Jede Zeile ein Event/Changeset, z.B.:
- `changeSetId`, `userId`, `timestamp`, `baseVersion`, `ops[]`
- `ops[]` als JSON Patch oder domain-spezifische Commands

**Vorteile**
- schnelle Append-Operationen
- gut für Audit/Undo/Redo/Sync

---

## 13. Mindest-Sicherheitsanforderungen (Checkliste)

- MFA verfügbar und für Risk Events erzwingbar
- sichere Passwortrichtlinien + Rate Limiting + Account Lockouts
- Token Rotation, Revocation, Device Sessions
- serverseitige Autorisierung pro Request (Projekt/Entitlements)
- AEAD-Verschlüsselung pro Datei + Schlüsselverwaltung mit Rotation
- Audit Logs unveränderlich (append-only), Export-Hashes
- regelmäßige Security Updates, Dependency Scanning, Pen-Test-ready

---

*Ende des technischen Grobkonzepts (Markdown).*
