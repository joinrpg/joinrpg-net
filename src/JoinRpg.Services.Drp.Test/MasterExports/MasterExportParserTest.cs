using System.Text;
using JoinRpg.Services.Drp.MasterExports;
using static JoinRpg.Services.Drp.Test.MasterExports.MasterExportTestHelpers;

namespace JoinRpg.Services.Drp.Test.MasterExports;

/// <summary>
/// Фикстуры руками: старые заголовки, пересохранённые Excel файлы и прочее, чего круговой тест
/// на нынешних экспортёрах не покажет.
/// </summary>
public class MasterExportParserTest
{
    private const string CharacterLink = "https://joinrpg.ru/12/character/101/";

    [Fact]
    public void OldCharacterHeaders_PlayerColumnsRecognized()
    {
        // Так выгружались персонажи до 2025-09-21: колонки игрока по свойствам EF-сущности User, со Skype.
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж", "Занят?", "Группы", "Входит в группы", "Ответственный мастер", "Биография",
                "Игрок", "Игрок.SurName", "Игрок.FatherName", "Игрок.BornName", "Игрок.Email",
                "Игрок.Extra.Vk", "Игрок.Extra.Skype", "Игрок.Extra.Telegram", "Игрок.Extra.Livejournal", "Игрок.Extra.PhoneNumber"],
            ["Арагорн", CharacterLink, "Занят", "", "", "Мастер", "Следопыт",
                "Вася", "Пупкин", "", "Василий", "vasya@example.com", "id1", "skype", "@vasya", "", "+7"]));

        result.Kind.ShouldBe(MasterExportKind.Characters);
        result.ProjectId.ShouldBe(12);
        result.OtherHeaders.ShouldBe(["Биография"]);
        var row = result.Rows.ShouldHaveSingleItem();
        row.CharacterId.ShouldBe(101);
        row.Name.ShouldBe("Арагорн");
        row.ResponsibleMasterName.ShouldBe("Мастер");
        row.PlayerDisplayName.ShouldBe("Вася");
        row.PlayerEmail.ShouldBe("vasya@example.com");
        row.Other("Биография").ShouldBe("Следопыт");
    }

    [Fact]
    public void ClaimHeaders_2025_09_21_To_2025_11_09()
    {
        // Игрок — EF-сущность User с Display «Игрок»; ВК и Телеграм уже ссылками (с 2025-10-14);
        // паспорт и адрес — по именам свойств; без поселения и прочих модулей.
        var result = Parse(Xlsx(
            ["Имя", "", "Статус", "Причина отказа", "Обновлена", "Создана", "Итого взнос", "Осталось", "Уплачено",
                "LastModifiedBy", "Ответственный", "Игрок", "Игрок.SurName", "Игрок.FatherName", "Игрок.BornName",
                "Игрок.Email", "ВК", "Телеграм", "Игрок.Extra.Livejournal", "Игрок.Extra.PhoneNumber",
                "PassportData", "RegistrationAddress", "Комната"],
            ["Арагорн", "https://joinrpg.ru/12/claim/555/edit", "Принята", "", new DateTime(2025, 10, 2, 8, 0, 0), new DateTime(2025, 10, 1, 7, 30, 0),
                1500, 500, 1000, "Мастер", "Главный мастер", "Вася", "", "", "", "vasya@example.com", "", "", "", "", "1234", "Москва", "101"]));

        result.Kind.ShouldBe(MasterExportKind.Claims);
        // «Комната» после колонок игрока — не системная колонка поселения, а поле проекта.
        result.OtherHeaders.ShouldBe(["Комната"]);
        var row = result.Rows.ShouldHaveSingleItem();
        row.ClaimId.ShouldBe(555);
        row.CharacterId.ShouldBeNull();
        row.Status.ShouldBe(ClaimStatus.Approved);
        row.TotalFee.ShouldBe(1500);
        row.ClaimCreatedAt.ShouldBe(new DateTimeOffset(2025, 10, 1, 7, 30, 0, TimeSpan.Zero));
        row.ClaimUpdatedAt.ShouldBe(new DateTimeOffset(2025, 10, 2, 8, 0, 0, TimeSpan.Zero));
        row.ResponsibleMasterName.ShouldBe("Главный мастер");
        row.PlayerDisplayName.ShouldBe("Вася");
        row.PlayerEmail.ShouldBe("vasya@example.com");
        row.Other("Комната").ShouldBe("101");
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ClaimHeaders_2025_11_09_To_2026_10_04()
    {
        // Префикс FullPlayer (EF-сущность без Display), хвосты .DisplayName, паспорт и адрес по-русски.
        var result = Parse(Xlsx(
            ["Имя", "", "Статус", "Причина отказа", "Обновлена", "Создана", "Итого взнос", "Осталось", "Уплачено",
                "LastModifiedBy.DisplayName", "Ответственный.DisplayName", "FullPlayer", "FullPlayer.SurName",
                "FullPlayer.FatherName", "FullPlayer.BornName", "FullPlayer.Email", "ВК", "Телеграм",
                "FullPlayer.Extra.Livejournal", "FullPlayer.Extra.PhoneNumber", "Паспортные данные", "Адрес", "Рост"],
            ["Арагорн", "https://joinrpg.ru/12/claim/555/edit", "Отклонена", "Игрок не отвечает", null, null,
                0, 0, 0, "Мастер", "Главный мастер", "Вася", "", "", "", "vasya@example.com", "", "", "", "", "", "", "190"]));

        result.Kind.ShouldBe(MasterExportKind.Claims);
        result.OtherHeaders.ShouldBe(["Рост"]);
        var row = result.Rows.ShouldHaveSingleItem();
        row.Status.ShouldBe(ClaimStatus.DeclinedByMaster);
        row.ResponsibleMasterName.ShouldBe("Главный мастер");
        row.PlayerDisplayName.ShouldBe("Вася");
        row.PlayerEmail.ShouldBe("vasya@example.com");
        row.Other("Рост").ShouldBe("190");
    }

    [Theory]
    [InlineData("Паспортные данные")]
    [InlineData("Адрес")]
    [InlineData("PassportData")]
    public void ClaimsWithoutPassport_FirstFieldNamedLikePassport_IsField(string fieldName)
    {
        var result = Parse(Xlsx(
            [.. ClaimHeaders(), fieldName, "Рост"],
            ClaimRow(1, "Принята", archived: false, "значение", "190")));

        result.OtherHeaders.ShouldBe([fieldName, "Рост"]);
        result.Rows.ShouldHaveSingleItem().Other(fieldName).ShouldBe("значение");
    }

    [Theory]
    [InlineData("ВК")]
    [InlineData("Телеграм")]
    [InlineData("Игрок.Фамилия")]
    public void ArchivedClaims_FirstFieldNamedLikePlayerColumn_IsField(string fieldName)
    {
        var result = Parse(Xlsx(
            [.. ClaimHeaders(archived: true), fieldName],
            ClaimRow(1, "Принята", archived: true, "значение")));

        result.OtherHeaders.ShouldBe([fieldName]);
        result.Rows.ShouldHaveSingleItem().Other(fieldName).ShouldBe("значение");
    }

    [Fact]
    public void FieldNamedLikePlayerColumn_BeforePlayerBlock_IsField()
    {
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж", "Занят?", "Группы", "Входит в группы", "Ответственный мастер", "ВК", "Игрок"],
            ["Арагорн", CharacterLink, "", "", "", "", "vk.com/aragorn", "Вася"]));

        result.OtherHeaders.ShouldBe(["ВК"]);
        result.Rows.ShouldHaveSingleItem().PlayerDisplayName.ShouldBe("Вася");
    }

    [Fact]
    public void CharacterLinksInClaimField_DoNotMakeExportAmbiguous()
    {
        var result = Parse(Xlsx(
            [.. ClaimHeaders(), "Персонаж"],
            ClaimRow(1, "Принята", archived: false, CharacterLink)));

        result.Kind.ShouldBe(MasterExportKind.Claims);
        result.Rows.ShouldHaveSingleItem().Other("Персонаж").ShouldBe(CharacterLink);
    }

    [Fact]
    public void TruncatedCell_IsMarkedAndUnwrapped()
    {
        const string prefix = "(обрезано) ";
        const string suffix = "...";
        var kept = new string('а', 32767 - prefix.Length - suffix.Length);
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж", "Биография"],
            ["Арагорн", CharacterLink, prefix + kept + suffix]));

        var cell = result.Rows.ShouldHaveSingleItem().OtherCells.ShouldHaveSingleItem();
        cell.Truncated.ShouldBeTrue();
        cell.Value.ShouldBe(kept);
        result.Warnings.ShouldContain(MasterExportWarning.TruncatedValues);
    }

    /// <summary>Маркеры без длины ровно в лимит Excel — это просто текст мастера.</summary>
    [Fact]
    public void ShortTextWithMarkers_IsNotTruncated()
    {
        const string text = "(обрезано) сам так написал...";
        var cell = Parse(Xlsx(["Персонаж", "Персонаж", "Биография"], ["Арагорн", CharacterLink, text]))
            .Rows.ShouldHaveSingleItem().OtherCells.ShouldHaveSingleItem();

        cell.Truncated.ShouldBeFalse();
        cell.Value.ShouldBe(text);
    }

    /// <summary>CSV-выгрузку не разбираем — только Excel.</summary>
    [Fact]
    public void CsvFile_IsRejected()
    {
        var csv = Encoding.UTF8.GetBytes("\"Персонаж\",\"Персонаж\"\r\n\"Арагорн\",\"https://joinrpg.ru/12/character/101/\"\r\n");

        var ex = Should.Throw<MasterExportFormatException>(() => Parse(csv, "export.csv"));
        ex.Error.ShouldBe(MasterExportFormatError.UnsupportedFile);
    }

    /// <summary>Если мастер пересохранил книгу и дата стала текстом — разбираем её как UTC.</summary>
    [Fact]
    public void TextDate_IsParsedAsUtc()
    {
        var row = Parse(Xlsx(
            ClaimHeaders(),
            ["Арагорн", "https://joinrpg.ru/12/claim/555/edit", "Обсуждается", null, null, "01.09.2026 7:30:00", 1500]))
            .Rows.ShouldHaveSingleItem();

        row.ClaimCreatedAt.ShouldBe(new DateTimeOffset(2026, 9, 1, 7, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void XlsxEmptyRowInTheMiddle_IsSkippedRowNumbersKept()
    {
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж"],
            ["А", "https://joinrpg.ru/12/character/1/"],
            [null, null],
            ["Б", "https://joinrpg.ru/12/character/2/"]));

        result.Rows.Select(r => (r.RowNumber, r.CharacterId)).ShouldBe([(2, (int?)1), (4, 2)]);
    }

    [Fact]
    public void GroupNameWithSeparator_GroupsKeptWithoutNames()
    {
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж", "Занят?", "Группы", "Входит в группы"],
            ["Арагорн", CharacterLink, "", "Эльфы | Лес | Север", "https://joinrpg.ru/12/roles/7/details | https://joinrpg.ru/12/roles/8/details"]));

        var row = result.Rows.ShouldHaveSingleItem();
        row.Groups.ShouldBe([new MasterExportGroupRef(7, ""), new MasterExportGroupRef(8, "")]);
        row.Warnings.ShouldBe([MasterExportWarning.GroupsCountMismatch]);
        result.Warnings.ShouldContain(MasterExportWarning.GroupsCountMismatch);
    }

    /// <summary>Ссылки на группы ищутся только сразу за «Группы»; такие же ссылки в поле — это поле.</summary>
    [Fact]
    public void GroupLinksOutsideTheirPlace_AreField()
    {
        const string groupLink = "https://joinrpg.ru/12/roles/7/details";
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж", "Занят?", "Группы", "Входит в группы", "Ответственный мастер", "Ссылка на группу"],
            ["Арагорн", CharacterLink, "", "", "", "", groupLink]));

        var row = result.Rows.ShouldHaveSingleItem();
        row.Groups.ShouldBeEmpty();
        row.Other("Ссылка на группу").ShouldBe(groupLink);
    }

    [Theory]
    [InlineData("https://joinrpg.ru/12/character/101")]
    [InlineData("https://joinrpg.ru/12/character/101/")]
    [InlineData("https://joinrpg.ru/12/character/101/details")]
    [InlineData("http://localhost:5001/12/character/101/")]
    public void CharacterLinkVariants(string link)
    {
        var row = Parse(Xlsx(["Персонаж", "Персонаж"], ["Арагорн", link])).Rows.ShouldHaveSingleItem();
        row.CharacterId.ShouldBe(101);
    }

    [Fact]
    public void LinkTextReplaced_HyperlinkUsed()
    {
        var row = Parse(Xlsx(["Персонаж", "Персонаж"], ["Арагорн", new Hyperlinked("Арагорн", CharacterLink)]))
            .Rows.ShouldHaveSingleItem();

        row.CharacterId.ShouldBe(101);
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ProjectIdMismatch_MostFrequentWins()
    {
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж"],
            ["А", "https://joinrpg.ru/12/character/1/"],
            ["Б", "https://joinrpg.ru/12/character/2/"],
            ["В", "https://joinrpg.ru/13/character/3/"]));

        result.ProjectId.ShouldBe(12);
        result.Warnings.ShouldContain(MasterExportWarning.ProjectIdMismatch);
    }

    [Fact]
    public void DuplicateIds_AreWarned()
    {
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж"],
            ["А", "https://joinrpg.ru/12/character/1/"],
            ["А снова", "https://joinrpg.ru/12/character/1"],
            ["Б", "https://joinrpg.ru/12/character/2/"]));

        result.Rows.Select(r => r.Warnings.Contains(MasterExportWarning.DuplicateEntityId)).ShouldBe([true, true, false]);
        result.Warnings.ShouldContain(MasterExportWarning.DuplicateEntityId);
    }

    [Fact]
    public void RowWithoutLink_IsKeptWithoutIds()
    {
        var result = Parse(Xlsx(
            ["Персонаж", "Персонаж"],
            ["А", CharacterLink],
            ["Дописал мастер", null]));

        result.Rows.Count.ShouldBe(2);
        var row = result.Rows[1];
        row.CharacterId.ShouldBeNull();
        row.Name.ShouldBe("Дописал мастер");
        row.Warnings.ShouldBe([MasterExportWarning.RowWithoutLink]);
        result.Warnings.ShouldContain(MasterExportWarning.RowWithoutLink);
    }

    /// <summary>Мусор в колонке ссылок (до 10% непустых) не мешает её найти — такие строки остаются без id.</summary>
    [Fact]
    public void GarbageInLinkColumn_RowWithoutLink()
    {
        object?[][] rows =
        [
            ["Персонаж", "Персонаж"],
            .. Enumerable.Range(1, 9).Select(i => new object?[] { $"П{i}", $"https://joinrpg.ru/12/character/{i}/" }),
            ["Мусор", "см. выше"],
        ];
        var result = Parse(Xlsx(rows));

        result.Rows.Count.ShouldBe(10);
        result.Rows.Count(r => r.CharacterId is not null).ShouldBe(9);
        result.Rows[^1].Warnings.ShouldBe([MasterExportWarning.RowWithoutLink]);
    }

    [Fact]
    public void TooMuchGarbageInLinkColumn_NotAnExport()
    {
        var ex = Should.Throw<MasterExportFormatException>(() => Parse(Xlsx(
            ["Персонаж", "Персонаж"],
            ["А", CharacterLink],
            ["Б", "мусор"])));
        ex.Error.ShouldBe(MasterExportFormatError.UnknownExportType);
    }

    [Fact]
    public void UnknownStatus_IsWarning()
    {
        var row = Parse(Xlsx(ClaimHeaders(), ClaimRow(1, "Непонятно"))).Rows.ShouldHaveSingleItem();

        row.Status.ShouldBeNull();
        row.Warnings.ShouldBe([MasterExportWarning.UnknownClaimStatus]);
    }

    [Fact]
    public void AllClaimStatusesRecognized()
    {
        string[] names = ["Подана", "Предложена", "Принята", "Отозвана", "Отклонена", "Обсуждается", "В листе ожидания", "Игрок заехал"];
        var result = Parse(Xlsx([ClaimHeaders(), .. names.Select((n, i) => ClaimRow(i + 1, n))]));

        result.Rows.Select(r => r.Status).ShouldBe(
        [
            ClaimStatus.AddedByUser, ClaimStatus.AddedByMaster, ClaimStatus.Approved, ClaimStatus.DeclinedByUser,
            ClaimStatus.DeclinedByMaster, ClaimStatus.Discussed, ClaimStatus.OnHold, ClaimStatus.CheckedIn,
        ]);
    }

    [Fact]
    public void NoLinkColumn_Throws()
    {
        var ex = Should.Throw<MasterExportFormatException>(() => Parse(Xlsx(["Персонаж"], ["Арагорн"])));
        ex.Error.ShouldBe(MasterExportFormatError.LinkColumnNotFound);
    }

    [Fact]
    public void HeadersOnly_Throws()
    {
        var ex = Should.Throw<MasterExportFormatException>(() => Parse(Xlsx(["Персонаж", "Персонаж"])));
        ex.Error.ShouldBe(MasterExportFormatError.NoRows);
    }

    [Fact]
    public void EmptyWorkbook_Throws()
    {
        var ex = Should.Throw<MasterExportFormatException>(() => Parse(Xlsx()));
        ex.Error.ShouldBe(MasterExportFormatError.Empty);
    }

    [Fact]
    public void EmptyFile_Throws()
    {
        var ex = Should.Throw<MasterExportFormatException>(() => Parse([]));
        ex.Error.ShouldBe(MasterExportFormatError.UnsupportedFile);
    }

    [Fact]
    public void OldBinaryExcel_Throws()
    {
        var ex = Should.Throw<MasterExportFormatException>(() => Parse([0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]));
        ex.Error.ShouldBe(MasterExportFormatError.UnsupportedFile);
    }
}
