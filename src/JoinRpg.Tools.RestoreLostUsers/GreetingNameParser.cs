namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// Достаёт отображаемое имя пользователя из приветствия «Добрый день, %recepient.name%!»,
/// с которого начинаются почти все уведомления (регистрация, сброс пароля, заявки, форум, письма мастеров).
/// </summary>
internal static class GreetingNameParser
{
    public const string GreetingPrefix = "Добрый день, ";

    private const int MaxNameLength = 100;

    /// <summary>
    /// Имя из первой строки тела уведомления или null, если строка — не приветствие.
    /// </summary>
    public static string? TryExtract(string firstLine)
    {
        if (!firstLine.StartsWith(GreetingPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var name = firstLine[GreetingPrefix.Length..].Trim().TrimEnd('!', ',', '.').Trim();
        if (name.Length == 0 || name.Length > MaxNameLength || name.Contains('%'))
        {
            return null;
        }
        return name;
    }

    /// <summary>
    /// Выбирает имя для заготовки пользователя: самое частое из приветствий, при равенстве — самое позднее.
    /// Подстановки по умолчанию (user{id} при регистрации и часть адреса почты до @, когда имя не задано)
    /// за имя не считаются.
    /// </summary>
    public static string? ChooseName(IEnumerable<GreetingLine> greetings, int userId, string? email)
    {
        var emailUserPart = email is null ? null : email[..Math.Max(0, email.IndexOf('@'))];
        var placeholder = $"user{userId}";

        return greetings
            .Select(g => (Name: TryExtract(g.FirstLine), g.Count, g.LastSeen))
            .Where(g => g.Name is not null
                        && !string.Equals(g.Name, placeholder, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(g.Name, emailUserPart, StringComparison.OrdinalIgnoreCase))
            .GroupBy(g => g.Name!, StringComparer.Ordinal)
            .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count), LastSeen: g.Max(x => x.LastSeen)))
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.LastSeen)
            .Select(g => g.Name)
            .FirstOrDefault();
    }
}
