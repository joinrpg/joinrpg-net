namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>Системные колонки выгрузок — всё, что не поле проекта.</summary>
internal enum MasterExportColumn
{
    /// <summary>Ссылка на персонажа или заявку — опознаётся по содержимому.</summary>
    Link,
    Name,
    BusyStatus,
    GroupNames,
    GroupLinks,
    ResponsibleMaster,

    ClaimStatus,
    DenialReason,
    UpdatedAt,
    CreatedAt,
    CheckInDate,
    TotalFee,
    FeeDue,
    FeePaid,
    PreferentialFee,
    AccommodationType,
    Room,
    LastModifiedBy,

    PlayerDisplayName,
    PlayerSurName,
    PlayerFatherName,
    PlayerBornName,
    PlayerEmail,
    PlayerVk,
    PlayerSkype,
    PlayerTelegram,
    PlayerLivejournal,
    PlayerPhone,

    PassportData,
    RegistrationAddress,
}

/// <summary>Место системной колонки в выгрузке: все её прежние заголовки; необязательная колонка зависит от модулей проекта.</summary>
internal record MasterExportSlot(MasterExportColumn Column, string[] Headers, bool Optional = false);

/// <summary>
/// Заголовки системных колонок со всеми прежними написаниями — в том порядке, в котором экспортёры
/// (<c>CharacterListItemViewModelExporter</c>, <c>ClaimListItemViewModelExporter</c>) их выводят.
/// </summary>
/// <remarks>
/// <para>
/// Имя поля проекта может совпасть с системным заголовком («Комната», «Адрес», «ВК», «Игрок»), поэтому
/// системные колонки сопоставляются строго последовательностью на своём месте, а не поиском по имени:
/// <list type="bullet">
/// <item>заявки — начало до «Ответственный», затем блок игрока, затем поля. Блок игрока — либо полный
/// (от «Игрок» до телефона), либо одна колонка «Игрок» (архивный проект). Паспорт и адрес — только парой
/// и только сразу за полным блоком;</item>
/// <item>персонажи — начало до «Ответственный мастер», затем поля, в самом конце блок игрока
/// (полный или одна колонка «Игрок»).</item>
/// </list>
/// </para>
/// <para>
/// История заголовков:
/// до 2026-10-03 у персонажей и до 2026-10-04 у заявок колонки игрока назывались по свойствам
/// EF-сущности <c>User</c> («Игрок.SurName», «Игрок.Extra.PhoneNumber»); у заявок с 2025-11-09 до
/// 2026-10-04 префикс был «FullPlayer». Skype выгружался до 2025-09-21, ВК и Телеграм назывались
/// «Игрок.Extra.Vk»/«Игрок.Extra.Telegram» до 2025-10-14. До 2025-11-09 у заявок паспорт и адрес —
/// «PassportData»/«RegistrationAddress», ответственный и автор последнего комментария —
/// «Ответственный»/«LastModifiedBy»; после — «….DisplayName».
/// </para>
/// </remarks>
internal static class MasterExportColumns
{
    private static readonly string[] PlayerPrefixes = ["Игрок", "FullPlayer"];

    private static string[] Player(params string[] suffixes)
        => [.. PlayerPrefixes.SelectMany(p => suffixes.Select(s => s.Length == 0 ? p : $"{p}.{s}"))];

    internal static MasterExportSlot PlayerDisplayName { get; } = new(MasterExportColumn.PlayerDisplayName, Player(""));

    /// <summary>Полный блок игрока (проект не в архиве).</summary>
    internal static IReadOnlyList<MasterExportSlot> FullPlayerBlock { get; } =
    [
        PlayerDisplayName,
        new(MasterExportColumn.PlayerSurName, Player("Фамилия", "SurName")),
        new(MasterExportColumn.PlayerFatherName, Player("Отчество", "FatherName")),
        new(MasterExportColumn.PlayerBornName, Player("Имя", "BornName")),
        new(MasterExportColumn.PlayerEmail, Player("Email")),
        new(MasterExportColumn.PlayerVk, ["ВК", .. Player("Extra.Vk")]),
        new(MasterExportColumn.PlayerSkype, Player("Extra.Skype"), Optional: true),
        new(MasterExportColumn.PlayerTelegram, ["Телеграм", .. Player("Extra.Telegram")]),
        new(MasterExportColumn.PlayerLivejournal, Player("Livejournal", "Extra.Livejournal")),
        new(MasterExportColumn.PlayerPhone, Player("Телефон", "Extra.PhoneNumber")),
    ];

    internal static IReadOnlyList<MasterExportSlot> PassportBlock { get; } =
    [
        new(MasterExportColumn.PassportData, ["Паспортные данные", "PassportData"]),
        new(MasterExportColumn.RegistrationAddress, ["Адрес", "RegistrationAddress"]),
    ];

    /// <summary>Начало выгрузки персонажей, до полей. Ссылка на персонажа — между именем и «Занят?».</summary>
    internal static IReadOnlyList<MasterExportSlot> CharacterPrefix { get; } =
    [
        new(MasterExportColumn.Name, ["Персонаж"]),
        new(MasterExportColumn.BusyStatus, ["Занят?"]),
        new(MasterExportColumn.GroupNames, ["Группы"]),
        new(MasterExportColumn.GroupLinks, ["Входит в группы"]),
        new(MasterExportColumn.ResponsibleMaster, ["Ответственный мастер"]),
    ];

    /// <summary>Начало выгрузки заявок, до блока игрока. Ссылка на заявку — между именем и статусом.</summary>
    internal static IReadOnlyList<MasterExportSlot> ClaimPrefix { get; } =
    [
        new(MasterExportColumn.Name, ["Имя"]),
        new(MasterExportColumn.ClaimStatus, ["Статус"]),
        new(MasterExportColumn.DenialReason, ["Причина отказа"]),
        new(MasterExportColumn.UpdatedAt, ["Обновлена"]),
        new(MasterExportColumn.CreatedAt, ["Создана"]),
        new(MasterExportColumn.CheckInDate, ["Дата заезда"], Optional: true),
        new(MasterExportColumn.TotalFee, ["Итого взнос"]),
        new(MasterExportColumn.FeeDue, ["Осталось"]),
        new(MasterExportColumn.FeePaid, ["Уплачено"]),
        new(MasterExportColumn.PreferentialFee, ["Льготник"], Optional: true),
        new(MasterExportColumn.AccommodationType, ["Тип поселения"], Optional: true),
        new(MasterExportColumn.Room, ["Комната"], Optional: true),
        new(MasterExportColumn.LastModifiedBy, ["LastModifiedBy.DisplayName", "LastModifiedBy"]),
        new(MasterExportColumn.ResponsibleMaster, ["Ответственный.DisplayName", "Ответственный"]),
    ];

    /// <summary>
    /// Раскладывает колонки на системные и поля. Колонка ссылок <paramref name="linkColumn"/> уже
    /// опознана по содержимому и последовательность не прерывает.
    /// </summary>
    /// <returns>Номер колонки → системная колонка; колонок полей в словаре нет.</returns>
    public static Dictionary<int, MasterExportColumn> Assign(MasterExportKind kind, IReadOnlyList<string> headers, int linkColumn)
    {
        var result = new Dictionary<int, MasterExportColumn> { [linkColumn] = MasterExportColumn.Link };
        return kind == MasterExportKind.Claims ? AssignClaim(headers, result) : AssignCharacter(headers, result);
    }

    /// <summary>Заявки: начало до «Ответственный», блок игрока (полный или одна колонка), паспорт с адресом, поля.</summary>
    private static Dictionary<int, MasterExportColumn> AssignClaim(IReadOnlyList<string> headers, Dictionary<int, MasterExportColumn> result)
    {
        var (end, complete) = Match(headers, 0, ClaimPrefix, result, allOrNothing: false);
        if (!complete)
        {
            return result;
        }
        var playerEnd = Match(headers, end, FullPlayerBlock, result, allOrNothing: true);
        if (playerEnd.Complete)
        {
            _ = Match(headers, playerEnd.End, PassportBlock, result, allOrNothing: true);
        }
        else
        {
            _ = Match(headers, end, [PlayerDisplayName], result, allOrNothing: true);
        }
        return result;
    }

    /// <summary>Персонажи: начало до «Ответственный мастер», поля, в самом конце блок игрока.</summary>
    private static Dictionary<int, MasterExportColumn> AssignCharacter(IReadOnlyList<string> headers, Dictionary<int, MasterExportColumn> result)
    {
        var (prefixEnd, _) = Match(headers, 0, CharacterPrefix, result, allOrNothing: false);

        // Колонки игрока — в самом конце, после полей: ищем, откуда полный блок дотягивается до конца.
        for (var start = prefixEnd; start < headers.Count; start++)
        {
            var attempt = new Dictionary<int, MasterExportColumn>(result);
            var (end, complete) = Match(headers, start, FullPlayerBlock, attempt, allOrNothing: true);
            if (complete && end == headers.Count)
            {
                return attempt;
            }
        }
        if (headers.Count - 1 >= prefixEnd)
        {
            _ = Match(headers, headers.Count - 1, [PlayerDisplayName], result, allOrNothing: true);
        }
        return result;
    }

    /// <summary>
    /// Сопоставляет колонки с <paramref name="slots"/> строго подряд, начиная с <paramref name="start"/>;
    /// уже опознанные колонки (ссылки) пропускает. Необязательный слот можно пропустить.
    /// </summary>
    /// <param name="allOrNothing">Если не сошлось целиком — ничего не записывать.</param>
    /// <returns>Колонка сразу за сопоставленными и сошлись ли все обязательные слоты.</returns>
    private static (int End, bool Complete) Match(
        IReadOnlyList<string> headers,
        int start,
        IReadOnlyList<MasterExportSlot> slots,
        Dictionary<int, MasterExportColumn> result,
        bool allOrNothing)
    {
        var assigned = new Dictionary<int, MasterExportColumn>();
        var column = start;
        var complete = true;
        foreach (var slot in slots)
        {
            while (result.ContainsKey(column))
            {
                column++;
            }
            if (column < headers.Count && slot.Headers.Contains(headers[column], StringComparer.Ordinal))
            {
                assigned[column] = slot.Column;
                column++;
            }
            else if (!slot.Optional)
            {
                complete = false;
                break;
            }
        }

        if (complete || !allOrNothing)
        {
            foreach (var (index, value) in assigned)
            {
                result[index] = value;
            }
        }
        return (column, complete);
    }
}
