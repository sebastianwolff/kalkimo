# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Kalkimo Planner** (working title: ImmoInvest Planner) is a cross-platform real estate investment calculator for iOS, Android, and Web. It provides bank-standard investment calculations with full German tax logic (AfA, §82b EStDV, §23 EStG), scenario analysis, cashflow/liquidity planning, maintenance/renovation modeling, multi-investor structures, and version-controlled project data.

**Status:** Currently documentation/planning phase only. No code has been implemented yet.

## Planned Tech Stack

**Frontend:**
- Vue.js + Ionic UI
- Capacitor for iOS/Android native builds
- Pinia for state management
- vue-i18n for internationalization
- Command pattern for Undo/Redo

**Backend:**
- C# / ASP.NET Core REST API
- Deterministic calculation engine as pure domain layer
- QuestPDF for PDF exports
- Optional WebSocket/SSE for real-time collaboration

**Storage:**
- Flatfile JSON (encrypted) - no traditional DBMS
- Per-project structure: `snapshot_*.json.enc` + `events_*.jsonl.enc`
- Envelope encryption: DEK per project, KEK per user, Master KEK for break-glass

**CMS Integration:**
- GRAV CMS embedding via theme/plugin

## Planned Repository Structure

```
/apps/frontend     # Ionic Vue App (Web + Mobile)
/backend/api       # ASP.NET Core API
/backend/domain    # Calculation engine / Domain model
/backend/exports   # Report renderer (PDF/CSV)
/cms/grav          # GRAV theme/plugin
/storage-spec/schemas  # JSON schemas for data formats
/docs              # Concept documentation
```

## Development Commands (Planned)

**Frontend:**
```bash
cd apps/frontend
npm install
npm run dev
```

**Backend:**
```bash
cd backend/api
dotnet restore
dotnet run
```

## Key Architectural Principles

1. **Security-by-Design:** Encryption, authentication, and authorization are foundational, not features. All project data files are encrypted at rest (`*.enc`). Security-related changes require additional review.

2. **Determinism in Calculations:** The calculation engine must be pure (no side effects, injected clock) for testability and reproducibility. Use golden tests with reference cases.

3. **Event Sourcing for Versioning:** All changes append to JSONL eventlog. Snapshots are periodic checkpoints. Undo creates counter-events rather than deleting history.

4. **Server-Side Authorization:** Feature entitlements and ACLs enforced on backend, never UI-only.

5. **Separation of Concerns:** Object-level cashflows vs. investor-level distribution. Tax calculations integrated into IRR/NPV metrics.

## German Tax Logic Requirements

The calculation engine must correctly implement:
- AfA rates by construction date (3%/2%/2.5%)
- Building vs. land value separation (land not depreciable)
- 15% rule for acquisition-related construction costs (anschaffungsnahe HK)
- §82b EStDV distribution of larger maintenance expenses (2-5 years)
- §23 EStG private sale taxation with 10-year holding period
- 1,000€ capital gains exemption threshold (since 2024)

## Configuration

**Frontend env vars:** `VITE_API_BASE_URL`

**Backend env vars:** `APP_ENV`, `DATA_ROOT`, `SECRETS_PROVIDER`, `MASTER_KEK_REF`, `AUTH_ISSUER`

Secrets must never be stored in the repository. Use `.env.local` (gitignored) or secret store for local development.
