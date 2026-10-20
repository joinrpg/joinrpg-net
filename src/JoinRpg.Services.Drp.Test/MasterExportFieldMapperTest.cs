using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Drp.MasterExports;

namespace JoinRpg.Services.Drp.Test;

public class MasterExportFieldMapperTest
{
    private readonly MockedProject mock = new();

    private ProjectFieldInfo AddField(
        ProjectFieldType type,
        string name = "Поле",
        bool isActive = true,
        FieldBoundTo boundTo = FieldBoundTo.Character,
        string valuesOrdering = "",
        params (string Label, bool IsActive)[] variants)
    {
        return mock.AddField(f =>
        {
            f.FieldName = name;
            f.FieldType = type;
            f.FieldBoundTo = boundTo;
            f.IsActive = isActive;
            f.ValuesOrdering = valuesOrdering;
            f.DropdownValues = [.. variants.Select((v, i) => new ProjectFieldDropdownValue
            {
                ProjectFieldDropdownValueId = i + 1,
                Label = v.Label,
                IsActive = v.IsActive,
                PlayerSelectable = true,
                ProjectId = mock.Project.ProjectId,
                Project = mock.Project,
            })];
        });
    }

    private ProjectFieldInfo AddListField(ProjectFieldType type, params string[] labels)
        => AddField(type, variants: [.. labels.Select(l => (l, true))]);

    /// <summary>
    /// Хранимое значение → текст выгрузки → разбор: получаем то же хранимое значение,
    /// а его отображение совпадает с текстом выгрузки.
    /// </summary>
    private static MasterExportFieldValue RoundTrip(ProjectFieldInfo field, string? stored) => RoundTrip(field, stored, stored);

    private static MasterExportFieldValue RoundTrip(ProjectFieldInfo field, string? stored, string? expectedStored)
    {
        var display = new FieldWithValue(field, stored).ExportString;

        var result = MasterExportFieldMapper.MapValue(field, display, truncated: false);

        result.Field.ShouldBe(field.Id);
        result.StoredValue.ShouldBe(expectedStored);
        new FieldWithValue(field, result.StoredValue).ExportString.ShouldBe(display);
        return result;
    }

    private static void ShouldApplyCleanly(MasterExportFieldValue result)
    {
        result.Warnings.ShouldBeEmpty();
        result.ApplyByDefault.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ProjectFieldType.String, "Привет, мир")]
    [InlineData(ProjectFieldType.Number, "42")]
    [InlineData(ProjectFieldType.Uri, "https://joinrpg.ru")]
    [InlineData(ProjectFieldType.Text, "Первый абзац\nВторой абзац")]
    [InlineData(ProjectFieldType.Login, "elf_42")]
    [InlineData(ProjectFieldType.PinCode, "0042")]
    public void PlainValueRoundTrip(ProjectFieldType type, string stored)
        => ShouldApplyCleanly(RoundTrip(AddField(type), stored));

    [Theory]
    [InlineData(ProjectFieldType.String)]
    [InlineData(ProjectFieldType.Text)]
    [InlineData(ProjectFieldType.MultiSelect)]
    [InlineData(ProjectFieldType.UserLink)]
    public void EmptyValueRoundTrip(ProjectFieldType type)
        => ShouldApplyCleanly(RoundTrip(AddField(type), null));

    [Fact]
    public void WhitespaceIsEmpty()
        => MasterExportFieldMapper.MapValue(AddField(ProjectFieldType.String), "  ", false).StoredValue.ShouldBeNull();

    [Fact]
    public void CheckboxOnRoundTrip() => ShouldApplyCleanly(RoundTrip(AddField(ProjectFieldType.Checkbox), "on"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CheckboxOffRoundTrip(string? stored)
        => ShouldApplyCleanly(RoundTrip(AddField(ProjectFieldType.Checkbox), stored, expectedStored: null));

    [Fact]
    public void CheckboxExportIsDistinguishable()
        => new FieldWithValue(AddField(ProjectFieldType.Checkbox), "on").ExportString
            .ShouldNotBe(new FieldWithValue(AddField(ProjectFieldType.Checkbox), null).ExportString);

    // Выгрузки до перехода на «да»/«нет» писали значки
    [Theory]
    [InlineData("☑️", "on")]
    [InlineData("☑", "on")]
    [InlineData("☐", null)]
    [InlineData("Да", "on")]
    [InlineData("НЕТ", null)]
    public void CheckboxOtherSpellingsAreParsed(string text, string? expectedStored)
    {
        var result = MasterExportFieldMapper.MapValue(AddField(ProjectFieldType.Checkbox), text, false);

        result.StoredValue.ShouldBe(expectedStored);
        ShouldApplyCleanly(result);
    }

    [Fact]
    public void CheckboxGarbageIsMarked()
    {
        var result = MasterExportFieldMapper.MapValue(AddField(ProjectFieldType.Checkbox), "может быть", false);

        result.StoredValue.ShouldBeNull();
        result.Warnings.ShouldBe([MasterExportFieldWarning.InvalidCheckboxValue]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void DropdownRoundTrip()
        => ShouldApplyCleanly(RoundTrip(AddListField(ProjectFieldType.Dropdown, "Эльф", "Гном", "Человек"), "2"));

    [Fact]
    public void MultiSelectRoundTrip()
        => ShouldApplyCleanly(RoundTrip(AddListField(ProjectFieldType.MultiSelect, "Меч", "Лук", "Посох", "Щит"), "1,3,4"));

    [Fact]
    public void LabelWithCommaRoundTrip()
    {
        // «юг» не совпадает с подписью «Юг», так что раскладка единственная
        var field = AddListField(ProjectFieldType.MultiSelect, "Север, юг", "Север", "Юг");

        ShouldApplyCleanly(RoundTrip(field, "1,3"));
    }

    [Fact]
    public void LabelWithCommaAmbiguous()
    {
        var field = AddListField(ProjectFieldType.MultiSelect, "А", "Б", "А, Б");

        var result = RoundTrip(field, "3");

        result.Warnings.ShouldBe([MasterExportFieldWarning.VariantAmbiguous]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void SameLabelOnTwoVariantsIsAmbiguous()
    {
        var field = AddListField(ProjectFieldType.Dropdown, "Да", "Да");

        var result = MasterExportFieldMapper.MapValue(field, "Да", false);

        result.StoredValue.ShouldBe("1");
        result.Warnings.ShouldBe([MasterExportFieldWarning.VariantAmbiguous]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void InactiveVariantRoundTrip()
    {
        var field = AddField(ProjectFieldType.MultiSelect, variants: [("Старый", false), ("Новый", true)]);

        var result = RoundTrip(field, "1,2");

        result.Warnings.ShouldBe([MasterExportFieldWarning.VariantInactive]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void InactiveVariantInDropdown()
    {
        var field = AddField(ProjectFieldType.Dropdown, variants: [("Старый", false), ("Новый", true)]);

        var result = RoundTrip(field, "1");

        result.Warnings.ShouldBe([MasterExportFieldWarning.VariantInactive]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void SeveralVariantsInDropdownAreMarked()
    {
        var field = AddListField(ProjectFieldType.Dropdown, "Эльф", "Гном");

        var result = MasterExportFieldMapper.MapValue(field, "Эльф, Гном", false);

        result.StoredValue.ShouldBe("1,2");
        result.Warnings.ShouldBe([MasterExportFieldWarning.MultipleVariantsForSingleChoice]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void VariantOrderDisambiguatesLabelWithComma()
    {
        // Выгрузка пишет подписи в порядке вариантов: «Б, А» из «А» и «Б» получиться не могло
        var field = AddListField(ProjectFieldType.MultiSelect, "Б, А", "А", "Б");

        ShouldApplyCleanly(RoundTrip(field, "1"));
    }

    [Fact]
    public void ValuesOrderingDoesNotAffectRoundTrip()
    {
        var field = AddField(ProjectFieldType.MultiSelect, valuesOrdering: "3,1,2",
            variants: [("Меч", true), ("Лук", true), ("Посох", true)]);

        ShouldApplyCleanly(RoundTrip(field, "1,2,3"));
        ShouldApplyCleanly(RoundTrip(field, "3,1", expectedStored: "1,3"));
    }

    [Fact]
    public void LabelsWithEdgeSpacesRoundTrip()
    {
        var field = AddListField(ProjectFieldType.MultiSelect, " Меч ", "Лук ", " Посох");

        ShouldApplyCleanly(RoundTrip(field, "1,2,3"));
        ShouldApplyCleanly(RoundTrip(field, "1"));
        ShouldApplyCleanly(RoundTrip(field, "3"));
    }

    [Fact]
    public void TimeSlotRoundTrip()
    {
        var field = AddListField(ProjectFieldType.ScheduleTimeSlotField, "Пятница, 10:00", "Пятница, 12:00", "Суббота, 10:00");

        ShouldApplyCleanly(RoundTrip(field, "1,3"));
    }

    [Fact]
    public void RoomRoundTrip()
        => ShouldApplyCleanly(RoundTrip(AddListField(ProjectFieldType.ScheduleRoomField, "Большой зал", "Беседка"), "1,2"));

    [Fact]
    public void ManyTokensAreParsedQuickly()
    {
        var labels = Enumerable.Range(0, 20).Select(i => $"Вариант {i}").ToArray();
        var field = AddListField(ProjectFieldType.MultiSelect, labels);
        // 10 000 кусков: известные подписи вперемешку с неизвестными
        var text = string.Join(", ", Enumerable.Range(0, 10_000).Select(i => i % 2 == 0 ? $"Вариант {i % 20}" : $"Мусор {i}"));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = MasterExportFieldMapper.MapValue(field, text, false);
        stopwatch.Stop();

        result.Warnings.ShouldBe([MasterExportFieldWarning.VariantNotFound]);
        result.StoredValue.ShouldBe(string.Join(",", Enumerable.Range(0, 20).Where(i => i % 2 == 0).Select(i => i + 1)));
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10)); // с запасом на медленный CI: кубический разбор шёл бы минуты
    }

    [Fact]
    public void UnknownLabelIsSkipped()
    {
        var field = AddListField(ProjectFieldType.MultiSelect, "Меч", "Лук");

        var result = MasterExportFieldMapper.MapValue(field, "Лук, Секира", false);

        result.StoredValue.ShouldBe("2");
        result.Warnings.ShouldBe([MasterExportFieldWarning.VariantNotFound]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData(ProjectFieldType.UserLink, "42")]
    [InlineData(ProjectFieldType.MultiUserLink, "42,17")]
    [InlineData(ProjectFieldType.ScheduleAuthorField, "5")]
    // Повтор одного пользователя — не ошибка и у одиночного поля
    [InlineData(ProjectFieldType.UserLink, "42,42")]
    public void UserLinkRoundTrip(ProjectFieldType type, string stored)
        => ShouldApplyCleanly(RoundTrip(AddField(type), stored));

    [Theory]
    [InlineData(ProjectFieldType.UserLink, "Вася", null)]
    // Двух разных пользователей в поле на одного домен не примет — не восстанавливаем
    [InlineData(ProjectFieldType.UserLink, "1,2", null)]
    [InlineData(ProjectFieldType.MultiUserLink, "1,Петя", "1")]
    public void InvalidUserIdsAreMarked(ProjectFieldType type, string text, string? expected)
    {
        var result = MasterExportFieldMapper.MapValue(AddField(type), text, false);

        result.StoredValue.ShouldBe(expected);
        result.Warnings.ShouldBe([MasterExportFieldWarning.InvalidUserIds]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void TruncatedIsNotAppliedByDefault()
    {
        var result = MasterExportFieldMapper.MapValue(AddField(ProjectFieldType.String), "Начало…", truncated: true);

        result.StoredValue.ShouldBe("Начало…");
        result.Warnings.ShouldBe([MasterExportFieldWarning.Truncated]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void InactiveFieldIsMarked()
    {
        var result = MasterExportFieldMapper.MapValue(AddField(ProjectFieldType.String, isActive: false), "Значение", false);

        result.StoredValue.ShouldBe("Значение");
        result.Warnings.ShouldBe([MasterExportFieldWarning.FieldInactive]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void HeaderFieldCannotHaveValue()
    {
        var result = MasterExportFieldMapper.MapValue(AddField(ProjectFieldType.Header), "Текст", false);

        result.StoredValue.ShouldBeNull();
        result.Warnings.ShouldBe([MasterExportFieldWarning.FieldCannotHaveValue]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void ColumnsMappedByName()
    {
        var name = AddField(ProjectFieldType.String, "Имя");
        var age = AddField(ProjectFieldType.Number, "Возраст", boundTo: FieldBoundTo.Claim);
        var old = AddField(ProjectFieldType.String, "Старое", isActive: false);
        _ = AddField(ProjectFieldType.Header, "Раздел");

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["Имя", "  Возраст ", "Старое", "Раздел"]);

        result.Select(m => m.Header).ShouldBe(["Имя", "  Возраст ", "Старое", "Раздел"]);
        result.Select(m => m.Field).ShouldBe([name.Id, age.Id, old.Id, null]);
        result[0].Warnings.ShouldBeEmpty();
        result[1].Warnings.ShouldBeEmpty();
        result[2].Warnings.ShouldBe([MasterExportFieldWarning.FieldInactive]);
        // Заголовков разделов в выгрузке нет — с ними колонки не сопоставляются
        result[3].Warnings.ShouldBe([MasterExportFieldWarning.ColumnNotMapped]);
        result[0].ApplyByDefault.ShouldBeTrue();
        result[2].ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void SectionWithSameNameDoesNotMakeFieldAmbiguous()
    {
        _ = AddField(ProjectFieldType.Header, "Легенда");
        var field = AddField(ProjectFieldType.Text, "Легенда");

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["Легенда"]).Single();

        result.Field.ShouldBe(field.Id);
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void FieldsDifferingByCaseAreAmbiguousForThirdCase()
    {
        _ = AddField(ProjectFieldType.String, "Имя");
        _ = AddField(ProjectFieldType.String, "ИМЯ");

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["имя"]).Single();

        result.Field.ShouldBeNull();
        result.Warnings.ShouldContain(MasterExportFieldWarning.ColumnAmbiguous);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void UnknownColumnNotMapped()
    {
        _ = AddField(ProjectFieldType.String, "Имя");

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["Статус заявки"]).Single();

        result.Field.ShouldBeNull();
        result.Warnings.ShouldBe([MasterExportFieldWarning.ColumnNotMapped]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void SameNamedFieldsAreAmbiguous()
    {
        _ = AddField(ProjectFieldType.String, "Дубль");
        _ = AddField(ProjectFieldType.String, "Дубль", isActive: false);

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["Дубль"]).Single();

        result.Field.ShouldBeNull();
        result.Warnings.ShouldBe([MasterExportFieldWarning.ColumnAmbiguous]);
    }

    [Fact]
    public void ExactMatchWinsOverCaseInsensitive()
    {
        var exact = AddField(ProjectFieldType.String, "Имя");
        _ = AddField(ProjectFieldType.String, "ИМЯ");

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["Имя"]).Single();

        result.Field.ShouldBe(exact.Id);
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void CaseInsensitiveMatchIsMarked()
    {
        var field = AddField(ProjectFieldType.String, "Имя");

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["имя"]).Single();

        result.Field.ShouldBe(field.Id);
        result.Warnings.ShouldBe([MasterExportFieldWarning.ColumnCaseMismatch]);
        result.ApplyByDefault.ShouldBeFalse();
    }

    [Fact]
    public void DuplicatedHeaderIsMarked()
    {
        // Например, поле названо так же, как служебная колонка выгрузки
        var field = AddField(ProjectFieldType.String, "Персонаж");

        var result = MasterExportFieldMapper.MapColumns(mock.ProjectInfo, ["Персонаж", "Персонаж "]);

        result.Count.ShouldBe(2);
        foreach (var mapping in result)
        {
            mapping.Field.ShouldBe(field.Id);
            mapping.Warnings.ShouldBe([MasterExportFieldWarning.ColumnDuplicated]);
        }
    }
}
