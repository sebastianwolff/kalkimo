using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kalkimo.Domain.Models;

/// <summary>
/// Monatliche Zeitreihe für einen Wert
/// </summary>
public class TimeSeries<T> : IEnumerable<(YearMonth Period, T Value)>
{
    private readonly Dictionary<YearMonth, T> _values = new();

    public YearMonth StartPeriod { get; }
    public YearMonth EndPeriod { get; }
    public int MonthCount => YearMonth.MonthsBetween(StartPeriod, EndPeriod) + 1;

    public TimeSeries(YearMonth startPeriod, YearMonth endPeriod)
    {
        if (endPeriod < startPeriod)
            throw new ArgumentException("End period must be >= start period");

        StartPeriod = startPeriod;
        EndPeriod = endPeriod;
    }

    public T this[YearMonth period]
    {
        get => _values.TryGetValue(period, out var value) ? value : default!;
        set
        {
            if (period < StartPeriod || period > EndPeriod)
                throw new ArgumentOutOfRangeException(nameof(period), $"Period {period} is outside range [{StartPeriod}, {EndPeriod}]");
            _values[period] = value;
        }
    }

    public bool HasValue(YearMonth period) => _values.ContainsKey(period);

    public IEnumerable<YearMonth> Periods
    {
        get
        {
            var current = StartPeriod;
            while (current <= EndPeriod)
            {
                yield return current;
                current = current.AddMonths(1);
            }
        }
    }

    public IEnumerator<(YearMonth Period, T Value)> GetEnumerator()
    {
        foreach (var period in Periods)
        {
            yield return (period, this[period]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Spezialisierte Zeitreihe für Geldbeträge mit Aggregationsmethoden
/// </summary>
public class MoneyTimeSeries : TimeSeries<Money>
{
    public string Currency { get; }

    public MoneyTimeSeries(YearMonth startPeriod, YearMonth endPeriod, string currency = "EUR")
        : base(startPeriod, endPeriod)
    {
        Currency = currency;
    }

    /// <summary>
    /// Überschreibt den Indexer um Money.Zero mit korrekter Währung zurückzugeben
    /// </summary>
    public new Money this[YearMonth period]
    {
        get
        {
            var value = base[period];
            // Wenn kein Wert gesetzt oder Währung leer, gebe Zero mit korrekter Währung zurück
            return string.IsNullOrEmpty(value.Currency) ? Money.Zero(Currency) : value;
        }
        set => base[period] = value;
    }

    public Money Sum()
    {
        var total = Money.Zero(Currency);
        foreach (var period in Periods)
        {
            total += this[period];
        }
        return total;
    }

    public Money SumForYear(int year)
    {
        var total = Money.Zero(Currency);
        foreach (var period in Periods.Where(p => p.Year == year))
        {
            total += this[period];
        }
        return total;
    }

    public Dictionary<int, Money> SumByYear()
    {
        var result = new Dictionary<int, Money>();
        foreach (var period in Periods)
        {
            var year = period.Year;
            if (!result.ContainsKey(year))
                result[year] = Money.Zero(Currency);
            result[year] += this[period];
        }
        return result;
    }

    public MoneyTimeSeries Add(MoneyTimeSeries other)
    {
        if (StartPeriod != other.StartPeriod || EndPeriod != other.EndPeriod)
            throw new InvalidOperationException("Time series must have same periods");

        var result = new MoneyTimeSeries(StartPeriod, EndPeriod, Currency);
        foreach (var period in Periods)
        {
            result[period] = this[period] + other[period];
        }
        return result;
    }

    public MoneyTimeSeries Cumulative()
    {
        var result = new MoneyTimeSeries(StartPeriod, EndPeriod, Currency);
        var running = Money.Zero(Currency);
        foreach (var period in Periods)
        {
            running += this[period];
            result[period] = running;
        }
        return result;
    }
}

/// <summary>
/// Jahr-Monat Werttyp
/// </summary>
[JsonConverter(typeof(YearMonthJsonConverter))]
public readonly record struct YearMonth : IComparable<YearMonth>
{
    public int Year { get; }
    public int Month { get; }

    /// <summary>
    /// Minimum value (year 1, month 1)
    /// </summary>
    public static YearMonth MinValue => new(1, 1);

    /// <summary>
    /// Maximum value (year 9999, month 12)
    /// </summary>
    public static YearMonth MaxValue => new(9999, 12);

    public YearMonth(int year, int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12");
        Year = year;
        Month = month;
    }

    public static YearMonth FromDate(DateOnly date) => new(date.Year, date.Month);
    public static YearMonth FromDateTime(DateTime date) => new(date.Year, date.Month);

    public YearMonth AddMonths(int months)
    {
        var totalMonths = Year * 12 + Month - 1 + months;
        var newYear = totalMonths / 12;
        var newMonth = totalMonths % 12 + 1;
        return new YearMonth(newYear, newMonth);
    }

    public YearMonth AddYears(int years) => new(Year + years, Month);

    public DateOnly ToFirstDayOfMonth() => new(Year, Month, 1);
    public DateOnly ToLastDayOfMonth() => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public static int MonthsBetween(YearMonth start, YearMonth end)
    {
        return (end.Year - start.Year) * 12 + (end.Month - start.Month);
    }

    public int CompareTo(YearMonth other)
    {
        var yearComparison = Year.CompareTo(other.Year);
        return yearComparison != 0 ? yearComparison : Month.CompareTo(other.Month);
    }

    public static bool operator <(YearMonth a, YearMonth b) => a.CompareTo(b) < 0;
    public static bool operator >(YearMonth a, YearMonth b) => a.CompareTo(b) > 0;
    public static bool operator <=(YearMonth a, YearMonth b) => a.CompareTo(b) <= 0;
    public static bool operator >=(YearMonth a, YearMonth b) => a.CompareTo(b) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}

/// <summary>
/// JSON Converter für YearMonth
/// </summary>
public class YearMonthJsonConverter : JsonConverter<YearMonth>
{
    public override YearMonth Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var month))
                {
                    return new YearMonth(year, month);
                }
            }
            throw new JsonException($"Cannot convert '{value}' to YearMonth");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int? year = null;
            int? month = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString()?.ToLowerInvariant();
                    reader.Read();

                    if (propertyName == "year")
                        year = reader.GetInt32();
                    else if (propertyName == "month")
                        month = reader.GetInt32();
                }
            }

            if (year.HasValue && month.HasValue)
                return new YearMonth(year.Value, month.Value);

            throw new JsonException("YearMonth object must have 'year' and 'month' properties");
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when parsing YearMonth");
    }

    public override void Write(Utf8JsonWriter writer, YearMonth value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("year", value.Year);
        writer.WriteNumber("month", value.Month);
        writer.WriteEndObject();
    }
}
