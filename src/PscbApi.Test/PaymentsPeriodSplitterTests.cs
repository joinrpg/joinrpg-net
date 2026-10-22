namespace PscbApi.Test;

/// <summary>
/// Нарезка периода на чанки для запроса списка платежей (ADR025).
/// </summary>
/// <remarks>
/// Банк принимает не больше месяца за запрос, а на превышение отвечает не ошибкой, а тихим
/// обрезанием до последнего месяца. Поэтому чанки проверяются не на «похоже на правду», а на то,
/// что они короче любого календарного месяца, идут встык и вместе покрывают период целиком.
/// </remarks>
public class PaymentsPeriodSplitterTests
{
    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public void ShortPeriodIsOneChunk()
    {
        var chunks = PaymentsPeriodSplitter.Split(From, From.AddDays(5));

        chunks.Count.ShouldBe(1);
        chunks[0].From.ShouldBe(From);
        chunks[0].To.ShouldBe(From.AddDays(5));
    }

    [Fact]
    public void ChunksAreContiguousAndCoverTheWholePeriod()
    {
        var to = From.AddDays(100);

        var chunks = PaymentsPeriodSplitter.Split(From, to);

        chunks.Count.ShouldBeGreaterThan(1);
        chunks[0].From.ShouldBe(From);
        chunks[^1].To.ShouldBe(to);
        for (var i = 1; i < chunks.Count; i++)
        {
            // Верхняя граница у банка исключительная, поэтому конец предыдущего чанка — это
            // начало следующего: ни дыр, ни перекрытий.
            chunks[i].From.ShouldBe(chunks[i - 1].To);
        }
    }

    [Fact]
    public void NoChunkIsLongerThanTheConfiguredLength()
    {
        var chunks = PaymentsPeriodSplitter.Split(From, From.AddDays((int)PaymentsPeriodSplitter.MaxPeriodLength.TotalDays));

        chunks.ShouldNotBeEmpty();
        foreach (var chunk in chunks)
        {
            (chunk.To - chunk.From).ShouldBeLessThanOrEqualTo(PaymentsPeriodSplitter.ChunkLength);
        }
    }

    /// <summary>
    /// Документация не говорит, что банк считает месяцем. 20 дней короче любого календарного
    /// месяца, в том числе февральского, поэтому обрезание не сработает ни при каком толковании.
    /// </summary>
    [Fact]
    public void ChunkIsShorterThanAnyCalendarMonth()
    {
        PaymentsPeriodSplitter.ChunkLength.ShouldBeLessThan(TimeSpan.FromDays(28));
    }

    [Fact]
    public void YearTakesNineteenRequests()
    {
        var chunks = PaymentsPeriodSplitter.Split(From, From.AddDays(365));

        chunks.Count.ShouldBe(19);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmptyOrInvertedPeriodIsRejected(int days)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => PaymentsPeriodSplitter.Split(From, From.AddDays(days)));
    }

    [Fact]
    public void TooLongPeriodIsRejected()
    {
        var tooLong = PaymentsPeriodSplitter.MaxPeriodLength + TimeSpan.FromDays(1);

        Should.Throw<ArgumentOutOfRangeException>(
            () => PaymentsPeriodSplitter.Split(From, From + tooLong));
    }
}
