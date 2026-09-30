namespace JoinRpg.Services.Impl.Accommodation;

/// <summary>
/// Готовые к подстановке в текст данные уведомления о комнате: всё уже разрешено в строки, ни
/// репозиториев, ни метаданных здесь больше не требуется.
/// </summary>
/// <param name="Changed">Имена игроков, которых операция сдвинула — в порядке заявок.</param>
/// <param name="Remaining">Имена игроков, оставшихся в комнате.</param>
internal record RoomOccupancyTextData(
    ProjectName ProjectName,
    string AccommodationTypeName,
    string RoomName,
    UserDisplayName Initiator,
    IReadOnlyCollection<UserDisplayName> Changed,
    IReadOnlyCollection<UserDisplayName> Remaining,
    RoomOccupancyChangeKind Kind);

/// <summary>
/// Собирает текст уведомлений о проживании. Вынесен отдельным классом без зависимостей — чтобы
/// каждый видимый игроку текст закрывался verify-тестом (ADR003, «Тестирование»).
/// </summary>
internal class AccommodationNotificationTextBuilder
{
    public string GetHeader(RoomOccupancyTextData data)
        => $"{data.ProjectName.Value}: комната {data.AccommodationTypeName} {data.RoomName}";

    public string GetBody(RoomOccupancyTextData data)
        => $"""
            Добрый день, %recepient.name%
            Изменен состав жителей комнаты {data.AccommodationTypeName} {data.RoomName}

            {GetChangeDescription(data)}

            {data.Initiator.DisplayName}
            """;

    private static string GetChangeDescription(RoomOccupancyTextData data)
    {
        var stayed = data.Remaining.Count > 0
            ? $"\n\nОстались в комнате:\n{PlayerList(data.Remaining)}"
            : "";

        return data.Kind switch
        {
            RoomOccupancyChangeKind.Occupied => data.Remaining.Count > 0
                ? $"Вселились в комнату:\n{PlayerList(data.Changed)}\n\nУже были в комнате:\n{PlayerList(data.Remaining)}"
                : $"Вселились в комнату:\n{PlayerList(data.Changed)}",

            RoomOccupancyChangeKind.Evicted => data.Remaining.Count > 0
                ? $"Покинули комнату:\n{PlayerList(data.Changed)}{stayed}"
                : $"Все жители покинули комнату:\n{PlayerList(data.Changed)}",

            // Заявка всегда одна: выезжает тот, чью заявку отклонили или отозвали.
            RoomOccupancyChangeKind.ClaimDeclined =>
                $"{PlayerNames(data.Changed)} покинул комнату, так как его заявка была отозвана или отклонена.{stayed}",

            RoomOccupancyChangeKind.LeftRoom =>
                $"{PlayerNames(data.Changed)} покинул комнату.{stayed}",

            _ => throw new ArgumentOutOfRangeException(nameof(data), data.Kind, "Неожиданное значение"),
        };
    }

    /// <summary>
    /// Список игроков маркированным списком. Раньше первый в списке оставался без маркера
    /// (<c>JoinStrings(" \n- ")</c>) — при переносе на markdown-шаблонизацию это починено.
    /// </summary>
    private static string PlayerList(IEnumerable<UserDisplayName> players)
        => string.Join("\n", players.Select(p => $"- {p.DisplayName}"));

    private static string PlayerNames(IEnumerable<UserDisplayName> players)
        => string.Join(", ", players.Select(p => p.DisplayName));
}
