using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.PrimitiveTypes.Characters;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using Xunit.Abstractions;

namespace JoinRpg.Domain.Test;

public class FieldSaveHelperTest
{
    private MockedProject _original = null!; // Should be initialized per fact
    private readonly ITestOutputHelper testOutputHelper;

    public FieldSaveHelperTest(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    private class MockedFieldSaveHelper(ITestOutputHelper testOutputHelper) : FieldSaveHelper(new MockedFieldDefaultValueGenerator(), new XUnitLogger<FieldSaveHelper>(testOutputHelper))
    {
        protected override void MarkAsUsed(IReadOnlyCollection<FieldWithPreviousAndNewValue> updatedFields, Project project) { }
    }

    private MockedFieldSaveHelper InitFieldSaveHelper() => new MockedFieldSaveHelper(testOutputHelper);

    [Fact]
    public void SaveOnAddTest()
    {
        _original = new MockedProject();

        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        // ReSharper disable once MustUseReturnValue
        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {mock.CharacterFieldInfo.Id.ProjectFieldId, "test"},
            },
            mock.ProjectInfo);
        mock.Character.JsonData
            .ShouldBe(_original.Character.JsonData,
                "Adding claim should not modify any character fields");

        mock.Character.Groups.Select(g => g.CharacterGroupId).ShouldBe(
            mock.Character.Groups.Select(g => g.CharacterGroupId),
            "Adding claim should not modify any character groups");

        claim.JsonData.ShouldBe($"{{\"{mock.CharacterFieldInfo.Id.ProjectFieldId}\":\"test\"}}");
    }

    [Fact]
    public void TryToChangeMasterOnlyFieldOnAdd()
    {
        _original = new MockedProject();
        var mock = new MockedProject();

        _ = Should.Throw<NoAccessToProjectException>(() =>
              InitFieldSaveHelper().SaveCharacterFields(
                  mock.Player.UserId,
                  mock.CreateClaim(mock.Character, mock.Player),
                  new Dictionary<int, string?>()
                  {
                    {mock.MasterOnlyFieldInfo.Id.ProjectFieldId, "test"},
                  },
                  mock.ProjectInfo));
    }

    [Fact]
    public void ApprovedClaimHiddenChangeTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var publicField = new FieldWithValue(mock.PublicFieldInfo, "Public");

        MockedProject.AssignFieldValues(claim, publicField);

        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {mock.HideForUnApprovedClaimInfo.Id.ProjectFieldId, "test"},
                {mock.CharacterFieldInfo.Id.ProjectFieldId, null},
            }, mock.ProjectInfo);

        mock.Character.FieldValuesShouldBe(new FieldWithValue(mock.HideForUnApprovedClaimInfo, "test"), publicField);
        ShouldBeTestExtensions.ShouldBe(claim.JsonData, "{}");
    }

    [Fact]
    public void MasterHiddenChangeTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var publicField = new FieldWithValue(mock.PublicFieldInfo, "Public");
        MockedProject.AssignFieldValues(mock.Character, publicField);
        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Master.UserId,
            mock.Character,
            new Dictionary<int, string?>()
            {
                {mock.HideForUnApprovedClaimInfo.Id.ProjectFieldId, "test"},
                {mock.CharacterFieldInfo.Id.ProjectFieldId, null},
            },
            mock.ProjectInfo);

        mock.Character.FieldValuesShouldBe(new FieldWithValue(mock.HideForUnApprovedClaimInfo, "test"), publicField);
    }

    [Fact]
    public void ApprovedClaimChangeTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {mock.CharacterFieldInfo.Id.ProjectFieldId, "test"},
            }, mock.ProjectInfo);

        mock.Character.FieldValuesShouldBe(new FieldWithValue(mock.CharacterFieldInfo, "test"));

        ShouldBeTestExtensions.ShouldBe(claim.JsonData, "{}");
    }

    [Fact]
    public void ConditionalFieldChangeTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var conditionalField = mock.CreateConditionalField();

        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {conditionalField.Id.ProjectFieldId, "test"},
            },
            mock.ProjectInfo);
        ShouldBeTestExtensions.ShouldBe(claim.JsonData,
            $"{{\"{conditionalField.Id.ProjectFieldId}\":\"test\"}}");
        ShouldBeTestExtensions.ShouldBe(mock.Character.JsonData,
            _original.Character.JsonData,
            "Adding claim should not modify any character fields");
    }


    [Fact]
    public void ConditionalFieldChangeTestForGroup()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var conditionalField = mock.CreateConditionalField();

        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {conditionalField.Id.ProjectFieldId, "test"},
            },
            mock.ProjectInfo);
        ShouldBeTestExtensions.ShouldBe(claim.JsonData,
            $"{{\"{conditionalField.Id.ProjectFieldId}\":\"test\"}}");
        ShouldBeTestExtensions.ShouldBe(mock.Character.JsonData,
            _original.Character.JsonData,
            "Adding claim should not modify any character fields");
    }


    [Fact]
    public void HiddenFieldChangeFailedTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        _ = Should.Throw<NoAccessToProjectException>(() =>
              InitFieldSaveHelper().SaveCharacterFields(
                  mock.Player.UserId,
                  claim,
                  new Dictionary<int, string?>()
                  {
                    {mock.HideForUnApprovedClaimInfo.Id.ProjectFieldId, "test"},
                  }, mock.ProjectInfo));
    }

    [Fact]
    public void DisableUnapprovedClaimToChangeCharacterTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {mock.CharacterFieldInfo.Id.ProjectFieldId, "test"},
            }, mock.ProjectInfo);
        ShouldBeTestExtensions.ShouldBe(mock.Character.JsonData,
            _original.Character.JsonData,
            "Adding claim should not modify any character fields");
        mock.Character.Groups.Select(g => g.CharacterGroupId).ToList().ShouldBe(
            (IEnumerable<int>)_original.Character.Groups.Select(g => g.CharacterGroupId)
                .ToList(),
            "Adding claim should not modify any character groups");
        ShouldBeTestExtensions.ShouldBe(claim.JsonData,
            $"{{\"{mock.CharacterFieldInfo.Id.ProjectFieldId}\":\"test\"}}");
    }

    [Fact]
    public void TryToChangeAnotherUserCharacter()
    {
        _original = new MockedProject();
        var mock = new MockedProject();

        _ = Should.Throw<NoAccessToProjectException>(() =>
              InitFieldSaveHelper().SaveCharacterFields(
                  mock.Player.UserId,
                  mock.Character,
                  new Dictionary<int, string?>()
                  {
                    {mock.CharacterFieldInfo.Id.ProjectFieldId, "test"},
                  }, mock.ProjectInfo));
    }

    [Fact]
    public void TryToSkipMandatoryField()
    {
        _original = new MockedProject();
        var mock = new MockedProject();

        var mandatoryField = mock.CreateField("Mandatory", canPlayerEdit: true, mandatoryStatus: MandatoryStatus.Required);

        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var exception = Should.Throw<CharacterFieldRequiredException>(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Player.UserId,
                claim,
                new Dictionary<int, string?>()
                {
                    {mandatoryField.Id.ProjectFieldId, ""},
                }, mock.ProjectInfo));

        exception.FieldName.ShouldBe(mandatoryField.Name);
    }

    [Fact]
    public void SetMandatoryField()
    {
        _original = new MockedProject();
        var mock = new MockedProject();

        mock.CharacterFieldInfo = mock.CharacterFieldInfo with { MandatoryStatus = MandatoryStatus.Required };

        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var exception = Should.NotThrow(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Player.UserId,
                claim,
                new Dictionary<int, string?>()
                {
                    {mock.CharacterFieldInfo.Id.ProjectFieldId, "test"},
                },
                mock.ProjectInfo));

        mock.Character.JsonData.ShouldBe($"{{\"{mock.CharacterFieldInfo.Id.ProjectFieldId}\":\"test\"}}");
    }

    [Fact]
    public void SkipOptionalField()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        mock.CharacterFieldInfo = mock.CharacterFieldInfo with { MandatoryStatus = MandatoryStatus.Optional };

        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var exception = Should.NotThrow(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Player.UserId,
                claim,
                new Dictionary<int, string?>()
                {
                    {mock.CharacterFieldInfo.Id.ProjectFieldId, ""},
                },
                mock.ProjectInfo));

        mock.Character.JsonData.ShouldBe("{}");
    }

    [Fact]
    public void TryToSetHeaderField()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var headerField = mock.CreateField("Заголовок", fieldType: ProjectFieldType.Header);

        _ = Should.Throw<FieldCannotHaveValueException>(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Master.UserId,
                mock.Character,
                new Dictionary<int, string?>()
                {
                    { headerField.Id.ProjectFieldId, "значение" },
                },
                mock.ProjectInfo));
    }

    [Fact]
    public void TryToSetInvalidDropdownValue()
    {
        var mock = new MockedProject();
        const int validVariantId = 100;
        var dropdownField = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.FieldName = "Поле с вариантами";
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = validVariantId,
                    Label = "Вариант 1",
                    IsActive = true,
                    PlayerSelectable = false,
                    Description = new MarkdownString(),
                    MasterDescription = new MarkdownString(),
                },
            ];
        });

        var exception = Should.Throw<FieldValueInvalidException>(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Master.UserId,
                mock.Character,
                new Dictionary<int, string?>
                {
                    { dropdownField.Id.ProjectFieldId, "99999" },
                },
                mock.ProjectInfo));

        exception.FieldName.ShouldBe("Поле с вариантами");
        exception.VariantId.ShouldBe(99999);
    }

    [Fact]
    public void SetValidDropdownValue()
    {
        var mock = new MockedProject();
        const int validVariantId = 100;
        var dropdownField = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.FieldName = "Поле с вариантами";
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = validVariantId,
                    Label = "Вариант 1",
                    IsActive = true,
                    PlayerSelectable = false,
                    Description = new MarkdownString(),
                    MasterDescription = new MarkdownString(),
                },
            ];
        });

        Should.NotThrow(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Master.UserId,
                mock.Character,
                new Dictionary<int, string?>
                {
                    { dropdownField.Id.ProjectFieldId, validVariantId.ToString() },
                },
                mock.ProjectInfo));

        mock.Character.JsonData.ShouldContain(validVariantId.ToString());
    }

    [Fact]
    public void TryToSetInvalidMultiSelectValue()
    {
        var mock = new MockedProject();
        const int validVariantId = 200;
        var multiSelectField = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.MultiSelect;
            f.FieldName = "Мультивыбор";
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = validVariantId,
                    Label = "Вариант A",
                    IsActive = true,
                    PlayerSelectable = false,
                    Description = new MarkdownString(),
                    MasterDescription = new MarkdownString(),
                },
            ];
        });

        // "200,99999" — первое значение корректное, второе нет
        _ = Should.Throw<FieldValueInvalidException>(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Master.UserId,
                mock.Character,
                new Dictionary<int, string?>
                {
                    { multiSelectField.Id.ProjectFieldId, $"{validVariantId},99999" },
                },
                mock.ProjectInfo));
    }
}

public class MockedFieldDefaultValueGenerator : IFieldDefaultValueGenerator
{
    public string? CreateDefaultValue(Claim? claim, FieldWithValue feld) => null;
    public string? CreateDefaultValue(Character? character, FieldWithValue field) => null;
}
