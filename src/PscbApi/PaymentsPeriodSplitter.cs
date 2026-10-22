namespace PscbApi;

/// <summary>
/// Splits a period into chunks short enough for a single payments list request
/// </summary>
/// <remarks>
/// The bank accepts at most one month per request and, instead of reporting an error, silently
/// returns payments for the last month of a longer period only. What exactly it treats as a month —
/// a calendar month, 30 days or <c>dateFrom.AddMonths(1)</c> — is not documented, so the chunk is
/// taken shorter than any calendar month. See ADR025.
/// </remarks>
public static class PaymentsPeriodSplitter
{
    /// <summary>
    /// Length of a single chunk. Shorter than any calendar month, so the bank's limit holds
    /// whatever it means by "a month"
    /// </summary>
    public static readonly TimeSpan ChunkLength = TimeSpan.FromDays(20);

    /// <summary>
    /// Longest period that may be requested at once. A year is already 19 sequential calls to the
    /// bank, and a request must not be able to turn into an endless series of them
    /// </summary>
    public static readonly TimeSpan MaxPeriodLength = TimeSpan.FromDays(730);

    /// <summary>
    /// Splits the half-open period into contiguous chunks. The bank treats the upper bound as
    /// exclusive, so the end of a chunk is the start of the next one and there is neither a gap
    /// nor an overlap
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The period is empty or longer than <see cref="MaxPeriodLength"/>
    /// </exception>
    public static IReadOnlyList<PaymentsPeriod> Split(DateTimeOffset dateFrom, DateTimeOffset dateTo)
    {
        if (dateTo <= dateFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(dateTo), dateTo, "The end of the period must be later than its start");
        }
        if (dateTo - dateFrom > MaxPeriodLength)
        {
            throw new ArgumentOutOfRangeException(nameof(dateTo), dateTo, $"The period must not be longer than {(int)MaxPeriodLength.TotalDays} days");
        }

        var chunks = new List<PaymentsPeriod>();
        for (var current = dateFrom; current < dateTo;)
        {
            var next = current + ChunkLength;
            if (next > dateTo)
            {
                next = dateTo;
            }
            chunks.Add(new PaymentsPeriod(current, next));
            current = next;
        }
        return chunks;
    }
}

/// <summary>
/// A half-open period: <see cref="From"/> is inclusive, <see cref="To"/> is exclusive
/// </summary>
public readonly record struct PaymentsPeriod(DateTimeOffset From, DateTimeOffset To);
