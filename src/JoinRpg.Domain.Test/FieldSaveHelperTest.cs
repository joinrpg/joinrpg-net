using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.TestHelpers;
using Shouldly;
using Xunit;
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

    private FieldSaveHelper InitFieldSaveHelper()
    {
        return new FieldSaveHelper(new MockedFieldDefaultValueGenerator(), new XUnitLogger<FieldSaveHelper>(testOutputHelper));
    }

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
                {mock.CharacterField.ProjectFieldId, "test"},
            },
            mock.ProjectInfo);
        mock.Character.JsonData
            .ShouldBe(_original.Character.JsonData,
                "Adding claim should not modify any character fields");

        mock.Character.Groups.Select(g => g.CharacterGroupId).ShouldBe(
            mock.Character.Groups.Select(g => g.CharacterGroupId),
            "Adding claim should not modify any character groups");

        claim.JsonData.ShouldBe($"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}");
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
                    {mock.MasterOnlyField.ProjectFieldId, "test"},
                  },
                  mock.ProjectInfo));
    }

    [Fact]
    public void ApprovedClaimHiddenChangeTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var publicField = new FieldWithValue(mock.PublicField, "Public");

        MockedProject.AssignFieldValues(claim, publicField);

        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {mock.HideForUnApprovedClaim.ProjectFieldId, "test"},
                {mock.CharacterField.ProjectFieldId, null},
            }, mock.ProjectInfo);

        mock.Character.FieldValuesShouldBe(new FieldWithValue(mock.HideForUnApprovedClaim, "test"), publicField);
        ShouldBeTestExtensions.ShouldBe(claim.JsonData, "{}");
    }

    [Fact]
    public void MasterHiddenChangeTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var publicField = new FieldWithValue(mock.PublicField, "Public");
        MockedProject.AssignFieldValues(mock.Character, publicField);
        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Master.UserId,
            mock.Character,
            new Dictionary<int, string?>()
            {
                {mock.HideForUnApprovedClaim.ProjectFieldId, "test"},
                {mock.CharacterField.ProjectFieldId, null},
            },
            mock.ProjectInfo);

        mock.Character.FieldValuesShouldBe(new FieldWithValue(mock.HideForUnApprovedClaim, "test"), publicField);
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
                {mock.CharacterField.ProjectFieldId, "test"},
            }, mock.ProjectInfo);

        mock.Character.FieldValuesShouldBe(new FieldWithValue(mock.CharacterField, "test"));

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
                {conditionalField.ProjectFieldId, "test"},
            },
            mock.ProjectInfo);
        ShouldBeTestExtensions.ShouldBe(claim.JsonData,
            $"{{\"{conditionalField.ProjectFieldId}\":\"test\"}}");
        ShouldBeTestExtensions.ShouldBe(mock.Character.JsonData,
            _original.Character.JsonData,
            "Adding claim should not modify any character fields");
    }


    [Fact]
    public void ConditionalFieldChangeTestForGroup()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Group, mock.Player);
        var conditionalField = mock.CreateConditionalField();

        _ = InitFieldSaveHelper().SaveCharacterFields(
            mock.Player.UserId,
            claim,
            new Dictionary<int, string?>()
            {
                {conditionalField.ProjectFieldId, "test"},
            },
            mock.ProjectInfo);
        ShouldBeTestExtensions.ShouldBe(claim.JsonData,
            $"{{\"{conditionalField.ProjectFieldId}\":\"test\"}}");
        ShouldBeTestExtensions.ShouldBe(mock.Character.JsonData,
            _original.Character.JsonData,
            "Adding claim should not modify any character fields");
    }


    [Fact]
    public void HiddenFieldChangeFailedTest()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Group, mock.Player);

        _ = Should.Throw<NoAccessToProjectException>(() =>
              InitFieldSaveHelper().SaveCharacterFields(
                  mock.Player.UserId,
                  claim,
                  new Dictionary<int, string?>()
                  {
                    {mock.HideForUnApprovedClaim.ProjectFieldId, "test"},
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
                {mock.CharacterField.ProjectFieldId, "test"},
            }, mock.ProjectInfo);
        ShouldBeTestExtensions.ShouldBe(mock.Character.JsonData,
            _original.Character.JsonData,
            "Adding claim should not modify any character fields");
        mock.Character.Groups.Select(g => g.CharacterGroupId).ToList().ShouldBe(
            (IEnumerable<int>)_original.Character.Groups.Select(g => g.CharacterGroupId)
                .ToList(),
            "Adding claim should not modify any character groups");
        ShouldBeTestExtensions.ShouldBe(claim.JsonData,
            $"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}");
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
                    {mock.CharacterField.ProjectFieldId, "test"},
                  }, mock.ProjectInfo));
    }

    [Fact]
    public void TryToSkipMandatoryField()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        mock.CharacterField.MandatoryStatus = MandatoryStatus.Required;

        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var exception = Should.Throw<FieldRequiredException>(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Player.UserId,
                claim,
                new Dictionary<int, string?>()
                {
                    {mock.CharacterField.ProjectFieldId, ""},
                }, mock.ProjectInfo));

        exception.FieldName.ShouldBe(mock.CharacterField.FieldName);
    }

    [Fact]
    public void SetMandatoryField()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        mock.CharacterField.MandatoryStatus = MandatoryStatus.Required;

        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var exception = Should.NotThrow(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Player.UserId,
                claim,
                new Dictionary<int, string?>()
                {
                    {mock.CharacterField.ProjectFieldId, "test"},
                },
                mock.ProjectInfo));

        mock.Character.JsonData.ShouldBe($"{{\"{mock.CharacterField.ProjectFieldId}\":\"test\"}}");
    }

    [Fact]
    public void SkipOptionalField()
    {
        _original = new MockedProject();
        var mock = new MockedProject();
        mock.CharacterField.MandatoryStatus = MandatoryStatus.Optional;

        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        var exception = Should.NotThrow(() =>
            InitFieldSaveHelper().SaveCharacterFields(
                mock.Player.UserId,
                claim,
                new Dictionary<int, string?>()
                {
                    {mock.CharacterField.ProjectFieldId, ""},
                },
                mock.ProjectInfo));

        mock.Character.JsonData.ShouldBe("{}");
    }
}

public class MockedFieldDefaultValueGenerator : IFieldDefaultValueGenerator
{
    public string? CreateDefaultValue(Claim? claim, FieldWithValue feld) => null;
    public string? CreateDefaultValue(Character? character, FieldWithValue field) => null;
}
