namespace Kalkimo.Domain.Models;

/// <summary>
/// Immutable value object für Geldbeträge
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency = "EUR")
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency = "EUR") => new(0, currency);
    public static Money Euro(decimal amount) => new(amount, "EUR");

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    public Money Divide(decimal divisor) => new(Amount / divisor, Currency);

    public Money Negate() => new(-Amount, Currency);

    public Money Round(int decimals = 2) => new(Math.Round(Amount, decimals, MidpointRounding.AwayFromZero), Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot operate on different currencies: {Currency} vs {other.Currency}");
    }

    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    public static Money operator *(Money a, decimal b) => a.Multiply(b);
    public static Money operator /(Money a, decimal b) => a.Divide(b);
    public static Money operator -(Money a) => a.Negate();

    public static bool operator >(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount > b.Amount;
    }

    public static bool operator <(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount < b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount >= b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount <= b.Amount;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
