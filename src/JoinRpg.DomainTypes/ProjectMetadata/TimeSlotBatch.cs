using System.Globalization;

namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Значение поля-таймслота, которое будет создано массовым добавлением
/// </summary>
public record TimeSlotBatchItem(string Label, TimeSlotOptions Options);

/// <summary>
/// Нарезка промежутка времени на таймслоты для массового добавления значений
/// </summary>
public static class TimeSlotBatch
{
    /// <summary>
    /// Предел длины слота и перерыва: промежуток всё равно не длиннее суток
    /// </summary>
    public const int MaxMinutes = 24 * 60;

    /// <summary>
    /// Режет промежуток [<paramref name="start"/>, <paramref name="end"/>) дня <paramref name="date"/> на слоты
    /// длиной <paramref name="slotMinutes"/> с перерывом <paramref name="breakMinutes"/> между ними.
    /// Если конец раньше начала, он считается на следующий день. Хвост, в который слот целиком не влезает, отбрасывается.
    /// Если начало совпадает с концом, слотов нет.
    /// Шаг идёт по местному времени: в поясах с переходом на летнее время слоты возле перехода
    /// получат несуществующее или неоднозначное время. Переходы обычно ночью, когда слоты не нарезают.
    /// </summary>
    /// <param name="timeZone">Таймзона, в которой заданы дата и время</param>
    public static IReadOnlyList<TimeSlotBatchItem> Generate(
        string? prefix,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        int slotMinutes,
        int breakMinutes,
        TimeZoneInfo timeZone)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(slotMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slotMinutes, MaxMinutes);
        ArgumentOutOfRangeException.ThrowIfNegative(breakMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(breakMinutes, MaxMinutes);

        if (start == end)
        {
            return [];
        }

        var from = date.ToDateTime(start);
        var to = date.ToDateTime(end);
        if (to < from)
        {
            to = to.AddDays(1);
        }

        var result = new List<TimeSlotBatchItem>();
        for (var slotStart = from; slotStart.AddMinutes(slotMinutes) <= to; slotStart = slotStart.AddMinutes(slotMinutes + breakMinutes))
        {
            var options = new TimeSlotOptions
            {
                StartTime = new DateTimeOffset(slotStart, timeZone.GetUtcOffset(slotStart)),
                TimeSlotInMinutes = slotMinutes,
            };
            result.Add(new TimeSlotBatchItem(FormatLabel(prefix, slotStart, slotStart.AddMinutes(slotMinutes)), options));
        }
        return result;
    }

    private static string FormatLabel(string? prefix, DateTime slotStart, DateTime slotEnd)
    {
        var time = string.Create(CultureInfo.InvariantCulture, $"{slotStart:HH:mm}–{slotEnd:HH:mm}");
        return string.IsNullOrWhiteSpace(prefix) ? time : $"{prefix.Trim()} {time}";
    }
}
