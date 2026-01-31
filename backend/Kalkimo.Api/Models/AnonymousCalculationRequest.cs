using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kalkimo.Api.Models;

// ===========================================
// Request-DTOs für anonyme Berechnung
// Akzeptiert Frontend-Project-JSON-Struktur direkt.
// ===========================================

/// <summary>
/// Request für anonyme Berechnung — Frontend sendet Projektdaten direkt
/// </summary>
public record AnonymousCalculationRequest
{
    [Required] public required string Currency { get; init; }
    [Required] public required YearMonthDto StartPeriod { get; init; }
    [Required] public required YearMonthDto EndPeriod { get; init; }
    [Required] public required PropertyDto Property { get; init; }
    [Required] public required PurchaseDto Purchase { get; init; }
    [Required] public required FinancingDto Financing { get; init; }
    [Required] public required RentConfigurationDto Rent { get; init; }
    [Required] public required CostConfigurationDto Costs { get; init; }
    [Required] public required TaxProfileDto TaxProfile { get; init; }
    public IReadOnlyList<CapExMeasureDto>? Capex { get; init; }
}

// === Shared Value Types ===

public record YearMonthDto
{
    public int Year { get; init; }
    public int Month { get; init; }
}

public record MoneyInputDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
}

// === Property ===

public record PropertyDto
{
    public string? Id { get; init; }
    public required string Type { get; init; }
    public int ConstructionYear { get; init; }
    public required string OverallCondition { get; init; }
    public decimal TotalArea { get; init; }
    public decimal LivingArea { get; init; }
    public decimal? LandArea { get; init; }
    public int UnitCount { get; init; } = 1;
    public IReadOnlyList<UnitDto>? Units { get; init; }
    public IReadOnlyList<ComponentConditionDto>? Components { get; init; }
    public WegConfigurationDto? WegConfiguration { get; init; }
    public decimal? RegionalPricePerSqm { get; init; }
}

public record UnitDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public decimal Area { get; init; }
    public int? Rooms { get; init; }
    public string? Floor { get; init; }
    public required string Status { get; init; }
}

public record ComponentConditionDto
{
    public required string Category { get; init; }
    public required string Condition { get; init; }
    public int? LastRenovationYear { get; init; }
    public int ExpectedCycleYears { get; init; }
}

// === WEG ===

public record WegConfigurationDto
{
    public string? WegName { get; init; }
    public decimal MeaPerMille { get; init; }
    public decimal TotalMeaPerMille { get; init; } = 1000m;
    public required HausgeldConfigurationDto Hausgeld { get; init; }
    public MoneyInputDto? CurrentReserveBalance { get; init; }
    public IReadOnlyList<SonderumlageDto>? Sonderumlagen { get; init; }
    public IReadOnlyList<CostDistributionKeyDto>? DistributionKeys { get; init; }
}

public record HausgeldConfigurationDto
{
    public required MoneyInputDto MonthlyTotal { get; init; }
    public required MoneyInputDto MonthlyReserveContribution { get; init; }
    public MoneyInputDto? MonthlyAdministration { get; init; }
    public MoneyInputDto? MonthlyMaintenance { get; init; }
    public MoneyInputDto? MonthlyHeating { get; init; }
    public MoneyInputDto? MonthlyOperatingCosts { get; init; }
    public decimal AnnualIncreasePercent { get; init; }
}

public record SonderumlageDto
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public required MoneyInputDto Amount { get; init; }
    public required YearMonthDto DueDate { get; init; }
    public string? RelatedMeasure { get; init; }
    public bool IsTaxDeductible { get; init; } = true;
    public string TaxClassification { get; init; } = "MaintenanceExpense";
}

public record CostDistributionKeyDto
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public decimal? CustomShare { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string>? ApplicableCostTypes { get; init; }
}

// === Purchase ===

public record PurchaseDto
{
    public required MoneyInputDto PurchasePrice { get; init; }
    public required YearMonthDto PurchaseDate { get; init; }
    public decimal LandValuePercent { get; init; }
    public IReadOnlyList<PurchaseCostItemDto>? Costs { get; init; }
}

public record PurchaseCostItemDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required MoneyInputDto Amount { get; init; }
    public bool IsDeductible { get; init; }
    public string TaxClassification { get; init; } = "MaintenanceExpense";
}

// === Financing ===

public record FinancingDto
{
    public required MoneyInputDto Equity { get; init; }
    public IReadOnlyList<LoanDto>? Loans { get; init; }
}

public record LoanDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required MoneyInputDto Principal { get; init; }
    public decimal InterestRatePercent { get; init; }
    public decimal InitialRepaymentPercent { get; init; }
    public int FixedInterestYears { get; init; }
    public required YearMonthDto StartDate { get; init; }
    public decimal? SpecialRepaymentPercentPerYear { get; init; }
}

// === Rent ===

public record RentConfigurationDto
{
    public required IReadOnlyList<RentUnitDto> Units { get; init; }
    public decimal VacancyRatePercent { get; init; }
    public int RentLossReserveMonths { get; init; }
}

public record RentUnitDto
{
    public required string UnitId { get; init; }
    public required MoneyInputDto MonthlyRent { get; init; }
    public required MoneyInputDto MonthlyServiceCharge { get; init; }
    public decimal AnnualRentIncreasePercent { get; init; }
}

// === Costs ===

public record CostConfigurationDto
{
    public required IReadOnlyList<CostItemDto> Items { get; init; }
    public required MoneyInputDto MaintenanceReserveMonthly { get; init; }
    public decimal MaintenanceReserveAnnualIncreasePercent { get; init; }
}

public record CostItemDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required MoneyInputDto MonthlyAmount { get; init; }
    public bool IsTransferable { get; init; }
    public decimal AnnualIncreasePercent { get; init; }
}

// === CapEx ===

public record CapExMeasureDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required MoneyInputDto Amount { get; init; }
    public required YearMonthDto ScheduledDate { get; init; }
    public required string TaxClassification { get; init; }
    public int? DistributionYears { get; init; }
    public MeasureImpactDto? Impact { get; init; }
    public bool IsRecurring { get; init; }
    public RecurringMeasureConfigDto? RecurringConfig { get; init; }
}

public record MeasureImpactDto
{
    public MoneyInputDto? CostSavingsMonthly { get; init; }
    public MoneyInputDto? RentIncreaseMonthly { get; init; }
    public decimal? RentIncreasePercent { get; init; }
    public int? DelayMonths { get; init; }
}

public record RecurringMeasureConfigDto
{
    public decimal IntervalPercent { get; init; }
    public decimal CostPercent { get; init; }
    public decimal CycleExtensionPercent { get; init; }
}

// === Tax Profile ===

public record TaxProfileDto
{
    public decimal MarginalTaxRatePercent { get; init; }
    public decimal? ChurchTaxPercent { get; init; }
    public bool IsCorporate { get; init; }
    public decimal? DepreciationRatePercent { get; init; }
    public bool Use82bDistribution { get; init; }
    public int DistributionYears82b { get; init; } = 5;
}
