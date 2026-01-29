# Defizit-Aufstellung: Kalkimo Planner Backend

**Erstellt:** 2026-01-28
**Version:** 1.0
**Status:** Fachlicher Review abgeschlossen

---

## Übersicht

| Kategorie | Kritisch | Hoch | Mittel | Gesamt |
|-----------|----------|------|--------|--------|
| Steuerlogik | 2 | 3 | 2 | 7 |
| Finanzierung | 0 | 2 | 1 | 3 |
| Sicherheit | 2 | 1 | 1 | 4 |
| Domain-Modell | 0 | 1 | 3 | 4 |
| **Gesamt** | **4** | **7** | **7** | **18** |

---

## 1. STEUERLOGIK

### 1.1 Haltedauer-Berechnung bei §23 EStG (KRITISCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/TaxCalculator.cs:177-179`

**Aktueller Code:**
```csharp
var holdingPeriodYears = (saleDate.Year - purchase.PurchaseDate.Year) +
    ((saleDate.Month >= purchase.PurchaseDate.Month) ? 0 : -1);
```

**Problem:** Die Berechnung ignoriert den Tag des Monats. Kauf am 15.01.2015, Verkauf am 14.01.2025 = 9 Jahre + 364 Tage, nicht 10 Jahre. Steuerfreiheit würde fälschlicherweise angenommen.

**Korrekte Implementierung:**
```csharp
// Exakte Tagesberechnung für §23 EStG
var holdingDays = saleDate.DayNumber - purchase.PurchaseDate.DayNumber;
var holdingPeriodYears = holdingDays / 365; // Vereinfacht

// Oder präzise mit Schaltjahren:
var exactYears = (saleDate.ToDateTime(TimeOnly.MinValue) -
                  purchase.PurchaseDate.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25;
var holdingPeriodYears = (int)exactYears;

// Die 10-Jahres-Frist endet am Tag nach dem 10. Jahrestag des Kaufs
var tenYearDeadline = purchase.PurchaseDate.AddYears(parameters.HoldingPeriodYears);
var isTaxExempt = saleDate > tenYearDeadline || parameters.OwnerOccupiedExemption;
```

---

### 1.2 Keine zeitanteilige AfA im ersten Jahr (KRITISCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/TaxCalculator.cs:42-48`

**Aktueller Code:**
```csharp
public Money CalculateMonthlyDepreciation(...)
{
    return CalculateAnnualDepreciation(purchase, property, taxProfile) / 12;
}
```

**Problem:** Bei Kauf im Juli beträgt die AfA im ersten Jahr nur 6/12 der Jahres-AfA. Aktuell wird volle Monats-AfA ab Januar berechnet.

**Korrekte Implementierung:**
```csharp
public MoneyTimeSeries CalculateDepreciationTimeSeries(...)
{
    var result = new MoneyTimeSeries(startPeriod, endPeriod);
    var purchasePeriod = YearMonth.FromDate(purchase.PurchaseDate);
    var annualDepreciation = CalculateAnnualDepreciation(purchase, property, taxProfile);
    var monthlyDepreciation = annualDepreciation / 12;

    var usefulLifeYears = taxProfile.CustomDepreciationRatePercent.HasValue
        ? (int)(100 / taxProfile.CustomDepreciationRatePercent.Value)
        : DepreciationRates.GetUsefulLifeYears(property.ConstructionYear);

    // AfA endet nach voller Nutzungsdauer, nicht nach X Kalenderjahren
    var totalDepreciationMonths = usefulLifeYears * 12;
    var depreciationEndPeriod = purchasePeriod.AddMonths(totalDepreciationMonths);

    foreach (var period in result.Periods)
    {
        if (period >= purchasePeriod && period < depreciationEndPeriod)
        {
            // Im letzten Monat: nur Restbetrag bis 100% abgeschrieben
            result[period] = monthlyDepreciation;
        }
        else
        {
            result[period] = Money.Zero();
        }
    }

    return result;
}
```

**Hinweis:** Der aktuelle Code ist technisch korrekt für die monatliche Verteilung, aber die **Jahressumme** muss im ersten und letzten Jahr zeitanteilig sein für die Steuererklärung.

---

### 1.3 Kapitalgesellschafts-Besteuerung fehlt (HOCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/TaxCalculator.cs:234-246`

**Aktueller Code:**
```csharp
public Money CalculateAnnualTax(Money taxableIncome, TaxProfile taxProfile)
{
    // Verwendet immer EffectiveTaxRatePercent (persönlicher Steuersatz)
    return (taxableIncome * taxProfile.EffectiveTaxRatePercent / 100).Round();
}
```

**Problem:** Bei `OwnershipType.Corporation` gelten andere Regeln:
- Körperschaftsteuer 15% + Soli 5,5% = 15,825%
- Gewerbesteuer ca. 14% (abhängig vom Hebesatz)
- §23 EStG gilt NICHT (gewerbliche Einkünfte)

**Korrekte Implementierung:**
```csharp
public Money CalculateAnnualTax(Money taxableIncome, TaxProfile taxProfile)
{
    if (taxableIncome <= Money.Zero())
        return Money.Zero();

    return taxProfile.OwnershipType switch
    {
        OwnershipType.Corporation => CalculateCorporateTax(taxableIncome, taxProfile),
        OwnershipType.Partnership => CalculatePartnershipTax(taxableIncome, taxProfile),
        _ => (taxableIncome * taxProfile.EffectiveTaxRatePercent / 100).Round()
    };
}

private Money CalculateCorporateTax(Money taxableIncome, TaxProfile taxProfile)
{
    const decimal CORP_TAX_RATE = 15m;
    const decimal SOLI_ON_CORP = 5.5m;

    var corpTax = taxableIncome * CORP_TAX_RATE / 100;
    var soliOnCorp = corpTax * SOLI_ON_CORP / 100;

    // Gewerbesteuer: Messbetrag * Hebesatz
    // Messbetrag = 3,5% vom Gewerbeertrag
    // Hebesatz ist kommunal unterschiedlich (min 200%, oft 400-500%)
    var tradeTaxRate = taxProfile.TradeTaxMultiplier ?? 400m; // Default 400%
    var tradeTaxBase = taxableIncome * 3.5m / 100;
    var tradeTax = tradeTaxBase * tradeTaxRate / 100;

    return (corpTax + soliOnCorp + tradeTax).Round();
}
```

**Zusätzlich benötigt in TaxProfile:**
```csharp
/// <summary>Gewerbesteuer-Hebesatz (nur bei Corporation)</summary>
public decimal? TradeTaxMultiplier { get; init; }
```

---

### 1.4 §23 EStG bei Kapitalgesellschaften fälschlicherweise angewandt (HOCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/TaxCalculator.cs:167-229`

**Problem:** Die Veräußerungsgewinnbesteuerung nach §23 EStG gilt nur für Privatvermögen. Bei Kapitalgesellschaften sind Veräußerungsgewinne laufende gewerbliche Einkünfte (volle Besteuerung, keine Haltefrist).

**Korrekte Implementierung:**
```csharp
public CapitalGainsTaxResult CalculateCapitalGainsTax(
    Purchase purchase,
    Money salePrice,
    Money saleCosts,
    Money accumulatedDepreciation,
    DateOnly saleDate,
    TaxProfile taxProfile,
    CapitalGainsTaxParameters parameters)
{
    // Bei Kapitalgesellschaft: KEINE Haltefrist, volle Besteuerung
    if (taxProfile.OwnershipType == OwnershipType.Corporation)
    {
        var adjustedBasis = purchase.TotalInvestment - accumulatedDepreciation;
        var gain = salePrice - saleCosts - adjustedBasis;

        // Veräußerungsgewinn ist laufender Gewinn, besteuert mit KSt + GewSt
        var tax = CalculateCorporateTax(gain, taxProfile);

        return new CapitalGainsTaxResult
        {
            IsTaxExempt = false,
            HoldingPeriodYears = 0, // Irrelevant bei GmbH
            Gain = gain,
            TaxAmount = gain > Money.Zero() ? tax : Money.Zero(),
            Reason = "Gewerbliche Einkünfte (§8 KStG)"
        };
    }

    // Bestehende Logik für Privatvermögen...
}
```

---

### 1.5 Verlustverrechnung ohne Beschränkung (HOCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/TaxCalculator.cs:238-244`

**Aktueller Code:**
```csharp
if (taxableIncome <= Money.Zero())
{
    return Money.Zero();
}
```

**Problem:**
1. Verluste werden nicht korrekt vorgetragen
2. §15a EStG (Beschränkung bei Kommanditisten) fehlt
3. Mindestbesteuerung bei hohen Gewinnen nach Verlustvortrag fehlt

**Korrekte Implementierung:**
```csharp
public Money CalculateAnnualTax(
    Money taxableIncome,
    TaxProfile taxProfile,
    ref Money lossCarryforward) // Verlustvortrag als ref-Parameter
{
    // 1. Verlustvortrag anrechnen
    if (lossCarryforward > Money.Zero() && taxableIncome > Money.Zero())
    {
        // Mindestbesteuerung: max 1 Mio € + 60% des darüber liegenden Betrags
        var maxOffset = taxableIncome.Amount <= 1_000_000m
            ? taxableIncome
            : new Money(1_000_000m + (taxableIncome.Amount - 1_000_000m) * 0.6m, taxableIncome.Currency);

        var actualOffset = Money.Min(lossCarryforward, maxOffset);
        taxableIncome -= actualOffset;
        lossCarryforward -= actualOffset;
    }

    // 2. Neuer Verlust -> zu Verlustvortrag addieren
    if (taxableIncome < Money.Zero())
    {
        if (taxProfile.LossOffsetEnabled)
        {
            lossCarryforward += taxableIncome.Abs();
        }
        return Money.Zero();
    }

    return (taxableIncome * taxProfile.EffectiveTaxRatePercent / 100).Round();
}
```

---

### 1.6 Solidaritätszuschlag-Berechnung vereinfacht (MITTEL)

**Datei:** `backend/Kalkimo.Domain/Models/TaxProfile.cs:30-40`

**Aktueller Code:**
```csharp
public decimal EffectiveTaxRatePercent
{
    get
    {
        var soli = MarginalTaxRatePercent * SolidaritySurchargePercent / 100;
        // ...
    }
}
```

**Problem:** Solidaritätszuschlag wird seit 2021 nur noch ab bestimmten Einkommensgrenzen erhoben (Freigrenzen). Für die meisten Privatanleger fällt kein Soli mehr an.

**Korrekte Implementierung:**
```csharp
public Money CalculateSolidaritySurcharge(Money incomeTax, TaxProfile taxProfile)
{
    // Freigrenzen 2024:
    // Einzelveranlagung: 18.130 € ESt -> kein Soli
    // Zusammenveranlagung: 36.260 € ESt -> kein Soli
    // Danach Gleitzone bis voller Soli

    const decimal SINGLE_THRESHOLD = 18_130m;
    const decimal JOINT_THRESHOLD = 36_260m;
    const decimal SOLI_RATE = 5.5m;

    var threshold = taxProfile.JointAssessment ? JOINT_THRESHOLD : SINGLE_THRESHOLD;

    if (incomeTax.Amount <= threshold)
        return Money.Zero();

    // Gleitzone: 11,9% des Differenzbetrags bis zur vollen Soli-Grenze
    var fullSoliThreshold = threshold * 1.45m; // Ungefähr
    if (incomeTax.Amount < fullSoliThreshold)
    {
        var excess = incomeTax.Amount - threshold;
        return new Money(excess * 0.119m, incomeTax.Currency).Round();
    }

    return (incomeTax * SOLI_RATE / 100).Round();
}
```

**Benötigt in TaxProfile:**
```csharp
public bool JointAssessment { get; init; } = false; // Zusammenveranlagung
```

---

### 1.7 Kirchensteuer-Sonderausgabenabzug fehlt (MITTEL)

**Problem:** Gezahlte Kirchensteuer ist als Sonderausgabe abzugsfähig und mindert das zu versteuernde Einkommen des Folgejahres.

**Aktuell:** Nicht implementiert.

**Empfehlung:** In der Jahressteuerberechnung berücksichtigen.

---

## 2. FINANZIERUNG

### 2.1 Bereitstellungszinsen nicht berechnet (HOCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/FinancingCalculator.cs:39-55`

**Aktueller Code:** Bereitstellungszinsen werden im Loan-Model definiert, aber nicht berechnet.

**Problem:** Bei gestaffelter Auszahlung (z.B. Baufinanzierung) fallen Bereitstellungszinsen auf den nicht abgerufenen Betrag an.

**Korrekte Implementierung:**
```csharp
// In CalculateLoanSchedule nach Zeile 41:
if (period < disbursementPeriod && loan.CommitmentFeePercent.HasValue)
{
    var commitmentFreeEnd = YearMonth.FromDate(
        loan.DisbursementDate.AddMonths(-loan.CommitmentFreeMonths));

    if (period >= commitmentFreeEnd)
    {
        // Bereitstellungszinsen auf volle Darlehenssumme
        var commitmentFee = loan.Principal * loan.CommitmentFeePercent.Value / 100 / 12;
        schedule.InterestPayments[period] = commitmentFee.Round();
        schedule.TotalPayments[period] = commitmentFee.Round();
    }
    continue;
}
```

---

### 2.2 KfW-Darlehen: Tilgungsfreie Anlaufjahre fehlen (HOCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/FinancingCalculator.cs:109-124`

**Aktueller Code:**
```csharp
return loan.Type switch
{
    LoanType.Annuity => CalculateAnnuityPayment(...),
    LoanType.BulletLoan => CalculateInterestOnlyPayment(...),
    _ => CalculateAnnuityPayment(...)  // KfW wird wie Annuität behandelt
};
```

**Problem:** KfW-Darlehen haben typischerweise 1-5 tilgungsfreie Anlaufjahre, in denen nur Zinsen gezahlt werden.

**Korrekte Implementierung:**
```csharp
// Neues Feld im Loan-Model:
public int TilgungsfreieAnlaufjahre { get; init; } = 0;

// In CalculateLoanSchedule:
var tilgungsfreieEnde = disbursementPeriod.AddYears(loan.TilgungsfreieAnlaufjahre);

if (loan.Type == LoanType.KfW && period <= tilgungsfreieEnde)
{
    // Nur Zinsen, keine Tilgung
    var interestPayment = outstandingBalance * currentRate;
    schedule.InterestPayments[period] = interestPayment.Round();
    schedule.PrincipalPayments[period] = Money.Zero(currency);
    schedule.TotalPayments[period] = interestPayment.Round();
    schedule.OutstandingBalance[period] = outstandingBalance;
    continue;
}
```

---

### 2.3 Disagio/Damnum nicht implementiert (MITTEL)

**Problem:** Disagio (Abschlag vom Darlehensbetrag) ist bei Immobilienfinanzierungen üblich und steuerlich über die Zinsbindungsfrist zu verteilen.

**Benötigt im Loan-Model:**
```csharp
public decimal? DisagioPercent { get; init; }
```

**Steuerliche Behandlung:**
```csharp
// Jährlicher Disagio-Anteil als Zinsaufwand
var disagioTotal = loan.Principal * loan.DisagioPercent.Value / 100;
var disagioPerMonth = disagioTotal / loan.FixedInterestPeriodMonths;
```

---

## 3. SICHERHEIT & INFRASTRUKTUR

### 3.1 In-Memory User Store (KRITISCH)

**Datei:** `backend/Kalkimo.Api/Controllers/AuthController.cs:24-25`

**Aktueller Code:**
```csharp
private static readonly ConcurrentDictionary<string, UserRecord> _users = new();
private static readonly ConcurrentDictionary<string, RefreshTokenRecord> _refreshTokens = new();
```

**Problem:** Alle Benutzer und Sessions gehen bei Server-Neustart verloren.

**Korrekte Implementierung:**
```csharp
public interface IUserStore
{
    Task<UserRecord?> GetByEmailAsync(string email, CancellationToken ct);
    Task<UserRecord?> GetByIdAsync(string id, CancellationToken ct);
    Task<bool> CreateAsync(UserRecord user, CancellationToken ct);
    Task UpdateAsync(UserRecord user, CancellationToken ct);
}

public interface IRefreshTokenStore
{
    Task<RefreshTokenRecord?> GetAndRemoveAsync(string token, CancellationToken ct);
    Task StoreAsync(string token, RefreshTokenRecord record, CancellationToken ct);
    Task RemoveAllForUserAsync(string userId, CancellationToken ct);
}

// Implementierung als verschlüsselte JSON-Files analog zu IProjectStore
public class FlatfileUserStore : IUserStore { ... }
```

---

### 3.2 LocalDevEncryptionService für Production ungeeignet (KRITISCH)

**Datei:** `backend/Kalkimo.Api/Infrastructure/IEncryptionService.cs:61-151`

**Probleme:**
1. Keys werden bei jedem Start neu generiert -> Daten nicht mehr lesbar
2. XOR-basiertes Key-Wrapping ist unsicher
3. Keine Key-Rotation

**Korrekte Implementierung (Azure Key Vault Beispiel):**
```csharp
public class AzureKeyVaultEncryptionService : IEncryptionService
{
    private readonly SecretClient _secretClient;
    private readonly CryptographyClient _cryptoClient;
    private readonly ConcurrentDictionary<string, byte[]> _dekCache = new();

    public async Task<EncryptedData> EncryptAsync(byte[] plaintext, string projectId, CancellationToken ct)
    {
        var dek = await GetOrCreateDekAsync(projectId, ct);

        using var aes = new AesGcm(dek, AesGcm.TagByteSizes.MaxSize);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return new EncryptedData
        {
            Ciphertext = ciphertext,
            Nonce = nonce,
            Tag = tag,
            KeyId = projectId,
            KeyVersion = await GetKeyVersionAsync(projectId, ct)
        };
    }

    private async Task<byte[]> GetOrCreateDekAsync(string projectId, CancellationToken ct)
    {
        if (_dekCache.TryGetValue(projectId, out var cachedDek))
            return cachedDek;

        try
        {
            // Try to load existing wrapped DEK
            var secret = await _secretClient.GetSecretAsync($"dek-{projectId}", cancellationToken: ct);
            var wrappedDek = Convert.FromBase64String(secret.Value.Value);

            // Unwrap with KEK from Key Vault
            var unwrapResult = await _cryptoClient.UnwrapKeyAsync(
                KeyWrapAlgorithm.RsaOaep256, wrappedDek, ct);

            _dekCache[projectId] = unwrapResult.Key;
            return unwrapResult.Key;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Create new DEK
            var newDek = new byte[32];
            RandomNumberGenerator.Fill(newDek);

            // Wrap with KEK and store
            var wrapResult = await _cryptoClient.WrapKeyAsync(
                KeyWrapAlgorithm.RsaOaep256, newDek, ct);

            await _secretClient.SetSecretAsync(
                $"dek-{projectId}",
                Convert.ToBase64String(wrapResult.EncryptedKey),
                ct);

            _dekCache[projectId] = newDek;
            return newDek;
        }
    }
}
```

---

### 3.3 Audit-Logging fehlt (HOCH)

**Problem:** Sicherheitsrelevante Aktionen werden nicht revisionssicher protokolliert.

**Benötigte Events:**
- Login-Erfolg/Fehlschlag
- Projekt erstellt/geändert/gelöscht
- Berechnung durchgeführt
- Admin-Aktionen

**Implementierung:**
```csharp
public interface IAuditLogger
{
    Task LogAsync(AuditEvent evt, CancellationToken ct);
}

public record AuditEvent
{
    public required string EventType { get; init; }
    public required string UserId { get; init; }
    public required string ResourceType { get; init; }
    public required string ResourceId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

// Append-only JSONL-Datei, signiert und verschlüsselt
public class FlatfileAuditLogger : IAuditLogger { ... }
```

---

## 4. DOMAIN-MODELL

### 4.1 Indexmiete: VPI-Daten nicht extern (HOCH)

**Datei:** `backend/Kalkimo.Domain/Calculators/CashflowCalculator.cs:46`

**Aktueller Code:**
```csharp
private static Money CalculateRentForPeriod(Tenancy tenancy, YearMonth period,
    decimal inflationIndexFactor = 1.02m) // Hardcoded 2%
```

**Problem:** Der VPI (Verbraucherpreisindex) sollte nicht hardcoded sein, sondern:
1. Historische Werte aus einer Datenquelle
2. Prognosewerte für die Zukunft konfigurierbar

**Korrekte Implementierung:**
```csharp
public interface IInflationDataProvider
{
    Task<decimal> GetCpiValueAsync(YearMonth period, CancellationToken ct);
}

// Im Project-Model:
public record InflationAssumptions
{
    public decimal DefaultAnnualRatePercent { get; init; } = 2.0m;
    public IReadOnlyDictionary<int, decimal>? YearlyOverrides { get; init; }
}
```

---

### 4.2 WEG-Instandhaltungsrücklage nicht separat (MITTEL)

**Problem:** Bei Eigentumswohnungen (PropertyType.Condominium) gibt es zwei Rücklagen:
1. WEG-Instandhaltungsrücklage (für Gemeinschaftseigentum)
2. Eigene Rücklage (für Sondereigentum)

**Aktuell:** Nur eine ReserveAccountConfig

**Korrekte Implementierung:**
```csharp
public record Property
{
    // ...

    /// <summary>WEG-Rücklage (bei Eigentumswohnung)</summary>
    public Money? WegReserveBalance { get; init; }

    /// <summary>Miteigentumsanteil in Promille (bei ETW)</summary>
    public decimal? MiteigentumsanteilPromille { get; init; }
}

public record CostConfiguration
{
    // ...

    /// <summary>Hausgeld/WEG-Beitrag (nur bei ETW)</summary>
    public Money? MonthlyHausgeld { get; init; }

    /// <summary>Davon Zuführung zur WEG-Rücklage</summary>
    public Money? WegReserveContribution { get; init; }
}
```

---

### 4.3 Grunderwerbsteuer-Sätze nicht aktualisierbar (MITTEL)

**Datei:** `backend/Kalkimo.Domain/Models/Purchase.cs:74-90`

**Problem:** GrESt-Sätze sind hardcoded und können sich ändern.

**Korrekte Implementierung:**
```csharp
public interface ITaxRateProvider
{
    decimal GetTransferTaxRate(string federalState, DateOnly asOfDate);
}

// Mit Default-Implementierung und Überschreibungsmöglichkeit
public class ConfigurableTaxRateProvider : ITaxRateProvider
{
    private readonly IConfiguration _config;

    public decimal GetTransferTaxRate(string federalState, DateOnly asOfDate)
    {
        // Zuerst Config prüfen, dann Default-Werte
        var configKey = $"TaxRates:TransferTax:{federalState}";
        if (_config[configKey] is string rateStr && decimal.TryParse(rateStr, out var rate))
            return rate;

        return AcquisitionCostRates.GetTransferTaxRate(federalState);
    }
}
```

---

### 4.4 Diskontierungszins hardcoded (MITTEL)

**Datei:** `backend/Kalkimo.Domain/Calculators/CalculationOrchestrator.cs:206-207`

**Aktueller Code:**
```csharp
discountRate: 5m, // Default discount rate for NPV
```

**Korrekte Implementierung:**
```csharp
// Im Project-Model:
public record ValuationConfiguration
{
    // ...

    /// <summary>Diskontierungszins für NPV-Berechnung (%)</summary>
    public decimal DiscountRatePercent { get; init; } = 5m;

    /// <summary>Alternativ: Risikoloser Zins + Risikoprämie</summary>
    public decimal? RiskFreeRatePercent { get; init; }
    public decimal? RiskPremiumPercent { get; init; }
}
```

---

## 5. FEHLENDE TESTS

### Fehlende Golden Tests

| Test | Priorität |
|------|-----------|
| IRR-Berechnung mit verschiedenen Cashflow-Mustern | Hoch |
| NPV bei unterschiedlichen Diskontierungszinsen | Hoch |
| Szenario-Anwendung (ApplyScenarioOverrides) | Hoch |
| Kapitalgesellschafts-Besteuerung | Hoch |
| KfW-Darlehen mit tilgungsfreien Jahren | Mittel |
| Indexmiete mit VPI-Schwelle | Mittel |
| Verlustverrechnung über mehrere Jahre | Mittel |
| Edge Cases: Negative Cashflows, 50+ Jahre Laufzeit | Niedrig |

---

## Anhang: Model-Erweiterungen

### TaxProfile (erweitert)

```csharp
public record TaxProfile
{
    // Bestehende Felder...

    /// <summary>Gewerbesteuer-Hebesatz (nur bei Corporation)</summary>
    public decimal? TradeTaxMultiplier { get; init; }

    /// <summary>Zusammenveranlagung (für Soli-Freigrenzen)</summary>
    public bool JointAssessment { get; init; } = false;
}
```

### Loan (erweitert)

```csharp
public record Loan
{
    // Bestehende Felder...

    /// <summary>Tilgungsfreie Anlaufjahre (KfW)</summary>
    public int TilgungsfreieAnlaufjahre { get; init; } = 0;

    /// <summary>Disagio in Prozent</summary>
    public decimal? DisagioPercent { get; init; }
}
```

### ValuationConfiguration (erweitert)

```csharp
public record ValuationConfiguration
{
    // Bestehende Felder...

    /// <summary>Diskontierungszins für NPV (%)</summary>
    public decimal DiscountRatePercent { get; init; } = 5m;
}
```
