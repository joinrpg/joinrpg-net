using System.Globalization;

namespace JoinRpg.Domain;

/// <summary>
/// ВК отдаёт bdate либо как "dd.MM.yyyy", либо как "dd.MM" (год скрыт настройками приватности),
/// либо не отдаёт вовсе. Нас интересует только полная дата с годом.
/// </summary>
public static class VkBirthDateParser
{
    public static bool TryParse(string? raw, out DateOnly birthDate)
    {
        return DateOnly.TryParseExact(raw, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate);
    }
}
