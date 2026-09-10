using System.Globalization;

namespace JoinRpg.Domain;

/// <summary>
/// ВК отдаёт bdate либо как "d.M.yyyy" (день/месяц без ведущего нуля, например "12.6.1985"),
/// либо как "d.M" (год скрыт настройками приватности), либо не отдаёт вовсе.
/// Нас интересует только полная дата с годом.
/// </summary>
public static class VkBirthDateParser
{
    public static bool TryParse(string? raw, out DateOnly birthDate)
    {
        return DateOnly.TryParseExact(raw, "d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate);
    }
}
