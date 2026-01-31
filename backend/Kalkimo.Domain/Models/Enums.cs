namespace Kalkimo.Domain.Models;

/// <summary>
/// Objekttyp der Immobilie
/// </summary>
public enum PropertyType
{
    SingleFamilyHome,    // Einfamilienhaus
    MultiFamilyHome,     // Mehrfamilienhaus
    Condominium,         // Eigentumswohnung (WEG)
    Commercial,          // Gewerbeimmobilie
    Mixed                // Gemischt (Wohn- und Gewerbe)
}

/// <summary>
/// Zustand des Objekts/Bauteils
/// </summary>
public enum Condition
{
    Good = 0,             // Gut (backward compat: bleibt 0)
    Fair = 1,             // Befriedigend (war "Medium", bleibt 1)
    Poor = 2,             // Schlecht (backward compat: bleibt 2)
    New = 3,              // Neu
    NeedsRenovation = 4   // Sanierungsbedürftig
}

/// <summary>
/// Darlehenstyp
/// </summary>
public enum LoanType
{
    Annuity,      // Annuitätendarlehen
    BulletLoan,   // Endfälliges Darlehen
    KfW,          // KfW-Darlehen
    Subordinated  // Nachrangdarlehen
}

/// <summary>
/// Mietentwicklungsmodell
/// </summary>
public enum RentDevelopmentModel
{
    Fixed,       // Fest
    Annual,      // Jährliche Steigerung
    Indexed,     // Indexmiete
    Graduated    // Staffelmiete
}

/// <summary>
/// Steuerliche Klassifikation von Maßnahmen
/// </summary>
public enum TaxClassification
{
    MaintenanceExpense,           // Erhaltungsaufwand (sofort absetzbar)
    MaintenanceExpenseDistributed,// Erhaltungsaufwand verteilt (§82b EStDV)
    ManufacturingCosts,           // Herstellungskosten (aktivierungspflichtig)
    AcquisitionRelatedCosts,      // Anschaffungsnahe HK (15%-Regel)
    NotDeductible                 // Nicht absetzbar (z.B. Eigenleistung)
}

/// <summary>
/// CapEx-Kategorie für Bauteile
/// </summary>
public enum CapExCategory
{
    Heating,      // Heizung
    Roof,         // Dach
    Facade,       // Fassade
    Windows,      // Fenster
    Electrical,   // Elektrik
    Plumbing,     // Sanitär
    Interior,     // Innenausbau
    Energy,       // Energetische Sanierung
    Exterior,     // Außenanlagen
    Other         // Sonstiges
}

/// <summary>
/// Kaufnebenkostentyp
/// </summary>
public enum AcquisitionCostType
{
    Notary,              // Notar
    LandRegistry,        // Grundbuch
    TransferTax,         // Grunderwerbsteuer
    BrokerFee,           // Makler
    Appraisal,           // Gutachten
    FinancingCosts,      // Finanzierungskosten
    DueDiligence,        // Due Diligence
    Other                // Sonstiges
}

/// <summary>
/// Haltungsform für steuerliche Behandlung
/// </summary>
public enum OwnershipType
{
    PrivateIndividual,           // Privatperson (VuV)
    Partnership,                  // Personengesellschaft/Bruchteilsgemeinschaft
    Corporation                   // Kapitalgesellschaft
}

/// <summary>
/// Kostenklassifikation
/// </summary>
public enum CostClassification
{
    Transferable,        // Umlagefähig
    NonTransferable,     // Nicht umlagefähig
    Administration,      // Verwaltung
    Insurance,           // Versicherung
    Maintenance,         // Laufende Instandhaltung
    Other                // Sonstiges
}
