namespace JoinRpg.WebPortal.Managers.Accommodation;

/// <summary>
/// Разбирает строку списка комнат, которую мастер вводит на странице «Комнаты»: имена через
/// запятую, числовые диапазоны через дефис — «1,2,5-8» даёт комнаты 1, 2, 5, 6, 7, 8.
/// </summary>
/// <remarks>
/// Живёт в web-слое, а не в сервисе бизнес-логики: это разбор пользовательского ввода конкретной
/// формы, а не правило предметной области. <c>IAccommodationService.AddRooms</c> принимает уже
/// готовый список имён и про синтаксис диапазонов ничего не знает.
/// </remarks>
public static class RoomNamesParser
{
    /// <summary>
    /// Имена комнат в порядке ввода. Пустые элементы списка отбрасываются, остальные имена
    /// отдаются как есть (с обрезанными пробелами): проверять их — дело сервиса.
    /// </summary>
    public static IReadOnlyList<string> Parse(string? rooms)
        => [.. ParseCore(rooms ?? "")];

    private static IEnumerable<string> ParseCore(string rooms)
    {
        foreach (var roomCandidate in rooms.Split(','))
        {
            if (TryParseRange(roomCandidate, out var rangeStart, out var rangeEnd))
            {
                for (var number = rangeStart; number <= rangeEnd; number++)
                {
                    yield return number.ToString();
                }

                continue;
            }

            var name = roomCandidate.Trim();
            if (name.Length > 0)
            {
                yield return name;
            }
        }
    }

    /// <summary>
    /// Числовой диапазон «5-8». Всё, что на диапазон не похоже (нечисловые границы, пустая
    /// граница, конец меньше начала), диапазоном не считается и остаётся именем комнаты как есть:
    /// имя комнаты вполне может содержать дефис.
    /// </summary>
    private static bool TryParseRange(string roomCandidate, out int rangeStart, out int rangeEnd)
    {
        rangeStart = 0;
        rangeEnd = 0;

        var rangePos = roomCandidate.IndexOf('-');
        if (rangePos < 0)
        {
            return false;
        }

        return int.TryParse(roomCandidate[..rangePos].Trim(), out rangeStart)
            && int.TryParse(roomCandidate[(rangePos + 1)..].Trim(), out rangeEnd)
            && rangeStart < rangeEnd;
    }
}
