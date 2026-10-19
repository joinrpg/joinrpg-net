using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using Xunit.Abstractions;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Режим восстановления (<see cref="FieldSaveMode.Restore"/>, DRP, #5529): значения из выгрузки
/// пишутся как были — без проверки обязательности и без значений по умолчанию, но с проверкой прав на
/// поле и самих значений.
/// </summary>
public class FieldSaveHelperRestoreModeTest(ITestOutputHelper testOutputHelper)
{
    private const string DefaultValue = "значение по умолчанию";

    /// <summary>Генерирует значение по умолчанию только для одного поля — остальные не трогает.</summary>
    private class SingleFieldDefaultValueGenerator(ProjectFieldIdentification fieldId) : IFieldDefaultValueGenerator
    {
        public string? CreateDefaultValue(Claim? claim, FieldWithValue field) => Generate(field);

        public string? CreateDefaultValue(Character? character, FieldWithValue field) => Generate(field);

        private string? Generate(FieldWithValue field) => field.Field.Id == fieldId ? DefaultValue : null;
    }

    private FieldSaveHelper CreateHelper(IFieldDefaultValueGenerator? generator = null)
        => new(generator ?? new MockedFieldDefaultValueGenerator(), new XUnitLogger<FieldSaveHelper>(testOutputHelper));

    private static FieldLayerContainer Layer(MockedProject mock, ProjectFieldInfo field, string? value)
        => new(mock.ProjectInfo, new Dictionary<int, string?> { [field.Id.ProjectFieldId] = value });

    [Theory]
    [InlineData(FieldSaveMode.Normal, true)]
    [InlineData(FieldSaveMode.Restore, false)]
    public void ClearingMandatoryCharacterField_ThrowsOnlyInNormalMode(FieldSaveMode mode, bool shouldThrow)
    {
        var mock = new MockedProject();
        var mandatory = mock.CreateField("Обязательное", mandatoryStatus: MandatoryStatus.Required);
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(mandatory, "было"));

        IReadOnlyCollection<FieldWithPreviousAndNewValue> Save() => CreateHelper().SaveCharacterFields(
            mock.Master.UserId, mock.Character, Layer(mock, mandatory, null), mock.ProjectInfo, mode: mode);

        if (shouldThrow)
        {
            _ = Should.Throw<CharacterFieldRequiredException>(Save);
            return;
        }

        var changed = Save();
        changed.ShouldHaveSingleItem().Field.Id.ShouldBe(mandatory.Id);
        mock.Character.JsonData.ShouldBe("{}");
    }

    [Theory]
    [InlineData(FieldSaveMode.Normal, true)]
    [InlineData(FieldSaveMode.Restore, false)]
    public void ClearingMandatoryFieldOfApprovedClaim_ThrowsOnlyInNormalMode(FieldSaveMode mode, bool shouldThrow)
    {
        var mock = new MockedProject();
        var mandatory = mock.CreateField("Обязательное", canPlayerEdit: true, mandatoryStatus: MandatoryStatus.Required);
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        void Save() => _ = CreateHelper().SaveCharacterFields(
            mock.Master.UserId, claim, Layer(mock, mandatory, ""), mock.ProjectInfo, mode);

        if (shouldThrow)
        {
            _ = Should.Throw<CharacterFieldRequiredException>(Save);
        }
        else
        {
            Should.NotThrow(Save);
        }
    }

    [Fact]
    public void RestoreMode_StillChecksFieldEditAccess()
    {
        var mock = new MockedProject();

        _ = Should.Throw<NoAccessToProjectException>(() => CreateHelper().SaveCharacterFields(
            mock.Player.UserId,
            mock.CreateApprovedClaim(mock.Character, mock.Player),
            Layer(mock, mock.MasterOnlyFieldInfo, "значение"),
            mock.ProjectInfo,
            FieldSaveMode.Restore));
    }

    [Fact]
    public void RestoreMode_StillValidatesDropdownValue()
    {
        var mock = new MockedProject();
        var dropdown = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.FieldName = "Поле с вариантами";
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 100,
                    Label = "Вариант 1",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
            ];
        });

        _ = Should.Throw<FieldValueInvalidException>(() => CreateHelper().SaveCharacterFields(
            mock.Master.UserId, mock.Character, Layer(mock, dropdown, "99999"), mock.ProjectInfo, mode: FieldSaveMode.Restore));
    }

    /// <summary>
    /// Выключенный вариант, которого нет в текущем значении: обычное сохранение его не даёт выбрать, а
    /// восстановление возвращает — его могли выключить уже после того, как выбрали.
    /// </summary>
    [Theory]
    [InlineData(FieldSaveMode.Normal, true)]
    [InlineData(FieldSaveMode.Restore, false)]
    public void InactiveVariantNotInCurrentValue_IsAllowedOnlyInRestoreMode(FieldSaveMode mode, bool shouldThrow)
    {
        var mock = new MockedProject();
        const int inactiveVariantId = 101;
        var dropdown = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.FieldName = "Поле с вариантами";
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = inactiveVariantId,
                    Label = "Выключенный вариант",
                    IsActive = false,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
            ];
        });

        void Save() => _ = CreateHelper().SaveCharacterFields(
            mock.Master.UserId, mock.Character, Layer(mock, dropdown, inactiveVariantId.ToString()), mock.ProjectInfo, mode: mode);

        if (shouldThrow)
        {
            Should.Throw<FieldValueInvalidException>(Save).VariantId.ShouldBe(inactiveVariantId);
            return;
        }

        Save();
        FieldLayerContainer.DeserializeFieldLayer(mock.ProjectInfo, mock.Character.JsonData)
            .GetValue(dropdown).ShouldBe(inactiveVariantId.ToString());
    }

    [Theory]
    [InlineData(FieldSaveMode.Normal, DefaultValue)]
    [InlineData(FieldSaveMode.Restore, null)]
    public void DefaultValues_AreGeneratedOnlyInNormalMode(FieldSaveMode mode, string? expected)
    {
        var mock = new MockedProject();
        var withDefault = mock.CreateField("С значением по умолчанию");

        _ = CreateHelper(new SingleFieldDefaultValueGenerator(withDefault.Id)).SaveCharacterFields(
            mock.Master.UserId,
            mock.Character,
            Layer(mock, mock.CharacterFieldInfo, "задано"),
            mock.ProjectInfo,
            mode: mode);

        var layer = FieldLayerContainer.DeserializeFieldLayer(mock.ProjectInfo, mock.Character.JsonData);
        layer.GetValue(withDefault).ShouldBe(expected);
        layer.GetValue(mock.CharacterFieldInfo).ShouldBe("задано");
    }
}
