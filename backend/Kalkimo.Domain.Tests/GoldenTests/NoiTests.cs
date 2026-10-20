using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;

namespace Kalkimo.Domain.Tests.GoldenTests;

/// <summary>
/// Tests für die NOI-Berechnung: NK-Vorauszahlungen sind ein durchlaufender Posten.
/// </summary>
public class NoiTests
{
    private static readonly YearMonth Start = new(2026, 1);
    private static readonly YearMonth End = new(2026, 12);

    private static MoneyTimeSeries Constant(decimal monthly)
    {
        var series = new MoneyTimeSeries(Start, End, "EUR");
        foreach (var period in series.Periods)
            series[period] = new Money(monthly, "EUR");
        return series;
    }

    private static decimal Noi(decimal rent, decimal serviceCharge, decimal transferable, decimal operating) =>
        CashflowCalculator.CalculateNoi(
            Constant(rent), Constant(serviceCharge), Constant(transferable), Constant(operating), Start, End)
        [Start].Amount;

    [Fact]
    public void ServiceChargeWithoutCosts_DoesNotIncreaseNoi()
    {
        // 1.550 € Kaltmiete, 150 € NK-Vorauszahlung, keine Kosten erfasst
        Assert.Equal(1550m, Noi(1550m, 150m, 0m, 0m));
    }

    [Fact]
    public void ServiceChargeCoveringTransferableCosts_IsNeutral()
    {
        // 150 € Vorauszahlung deckt 150 € umlagefähige Kosten, 50 € nicht umlagefähig
        Assert.Equal(1500m, Noi(1550m, 150m, 150m, 200m));
    }

    [Fact]
    public void ServiceChargeSurplus_IsRefundedToTenant()
    {
        // 200 € Vorauszahlung, nur 150 € umlagefähig -> Überschuss geht an Mieter zurück
        Assert.Equal(1550m, Noi(1550m, 200m, 150m, 150m));
    }

    [Fact]
    public void ServiceChargeShortfall_IsBorneByLandlord()
    {
        // 100 € Vorauszahlung, 150 € umlagefähig -> 50 € Unterdeckung trägt Vermieter
        Assert.Equal(1500m, Noi(1550m, 100m, 150m, 150m));
    }

    [Fact]
    public void CostSavingsBelowTransferable_CapRecoverableAtOperatingCosts()
    {
        // Umlagefähig 150 €, durch Einsparung insgesamt nur 80 € Kosten -> max. 80 € umlegbar
        Assert.Equal(1550m, Noi(1550m, 150m, 150m, 80m));
    }
}
