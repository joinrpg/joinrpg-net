using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Problems;
using JoinRpg.Domain.Problems.CharacterProblemFilters;
using JoinRpg.Domain.Problems.CommonProblemFilters;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test.Problems;

/// <summary>
/// Проблемы персонажа, посчитанные по доменному агрегату (ADR013): набор полей для проверки
/// и правила по значениям полей.
/// </summary>
public class CharacterProblemValidatorTest
{
    private readonly MockedProject mock = new();

    private readonly ICharacterProblemValidator validator = new CharacterProblemValidator(
        [new BrokenCharactersFilter()],
        [new FieldNotSetFilter(), new InActiveVariantsFilter()]);

    [Fact]
    public void EmptyRequiredFieldIsWarning()
    {
        var field = AddStringField(MandatoryStatus.Required);

        var problems = FieldProblems(mock.Character);

        problems.ShouldHaveSingleItem();
        problems[0].ProblemType.ShouldBe(ClaimProblemType.FieldIsEmpty);
        problems[0].Severity.ShouldBe(ProblemSeverity.Warning);
        problems[0].Field.ShouldBe(field);
    }

    [Fact]
    public void EmptyRecommendedFieldIsHint()
    {
        _ = AddStringField(MandatoryStatus.Recommended);

        var problems = FieldProblems(mock.Character);

        problems.ShouldHaveSingleItem();
        problems[0].ProblemType.ShouldBe(ClaimProblemType.FieldIsEmpty);
        problems[0].Severity.ShouldBe(ProblemSeverity.Hint);
    }

    [Fact]
    public void EmptyOptionalFieldIsNotAProblem()
    {
        _ = AddStringField(MandatoryStatus.Optional);

        FieldProblems(mock.Character).ShouldBeEmpty();
    }

    /// <summary>
    /// Поле, привязанное к заявке, со свободного персонажа не спрашиваем: заполнять его некому.
    /// </summary>
    [Fact]
    public void ClaimBoundFieldIsNotAskedFromCharacterWithoutApprovedClaim()
    {
        _ = AddStringField(MandatoryStatus.Required, FieldBoundTo.Claim);

        FieldProblems(mock.Character).ShouldBeEmpty();
    }

    [Fact]
    public void ClaimBoundFieldIsAskedWhenClaimIsApproved()
    {
        _ = AddStringField(MandatoryStatus.Required, FieldBoundTo.Claim);
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        FieldProblems(mock.Character).ShouldHaveSingleItem()
            .ProblemType.ShouldBe(ClaimProblemType.FieldIsEmpty);
    }

    [Fact]
    public void ValueInUnavailableFieldIsHint()
    {
        // Поле доступно только персонажам другой группы, а значение в нём есть — его забыли убрать.
        var otherGroup = mock.CreateCharacterGroup();
        var field = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.MandatoryStatus = MandatoryStatus.Optional;
            f.ValidForNpc = true;
            f.AvailableForCharacterGroupIds = [otherGroup.CharacterGroupId];
        });
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(field, "значение"));

        var problems = FieldProblems(mock.Character);

        problems.ShouldHaveSingleItem();
        problems[0].ProblemType.ShouldBe(ClaimProblemType.FieldShouldNotHaveValue);
        problems[0].Severity.ShouldBe(ProblemSeverity.Hint);
    }

    [Fact]
    public void InActiveVariantIsWarning()
    {
        var (field, inactiveVariantId) = AddDropdownFieldWithInactiveVariant();
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(field, inactiveVariantId.ToString()));

        var problems = FieldProblems(mock.Character);

        problems.ShouldHaveSingleItem();
        problems[0].ProblemType.ShouldBe(ClaimProblemType.InActiveVariant);
        problems[0].Severity.ShouldBe(ProblemSeverity.Warning);
    }

    [Fact]
    public void ActiveVariantIsNotAProblem()
    {
        var (field, _) = AddDropdownFieldWithInactiveVariant();
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(field, "1"));

        FieldProblems(mock.Character).ShouldBeEmpty();
    }

    /// <summary>
    /// <c>ValidateFieldOnly</c> отвечает только про запрошенное поле — на нём стоит фильтр списка
    /// персонажей «поле не проставлено».
    /// </summary>
    [Fact]
    public void ValidateFieldOnlyIgnoresOtherFields()
    {
        var asked = AddStringField(MandatoryStatus.Required);
        _ = AddStringField(MandatoryStatus.Required);

        var problems = validator.ValidateFieldOnly(mock.GetCharacterInfo(mock.Character), asked.Id).ToArray();

        problems.ShouldHaveSingleItem().Field.Id.ShouldBe(asked.Id);
    }

    /// <summary>
    /// <see cref="ICharacterProblemValidator.Validate"/> складывает проблемы полей и проблемы
    /// самого персонажа и отсекает их по минимальной важности.
    /// </summary>
    [Fact]
    public void ValidateCombinesFilterAndFieldProblemsAndFiltersBySeverity()
    {
        _ = AddStringField(MandatoryStatus.Recommended);
        var character = mock.CreateCharacter("В битой группе");
        character.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];
        var characterInfo = mock.GetCharacterInfo(character);

        validator.Validate(characterInfo).Select(p => p.ProblemType)
            .ShouldBe([ClaimProblemType.GroupIsBroken, ClaimProblemType.FieldIsEmpty], ignoreOrder: true);

        validator.Validate(characterInfo, ProblemSeverity.Warning).Select(p => p.ProblemType)
            .ShouldBe([ClaimProblemType.GroupIsBroken]);
    }

    private FieldRelatedProblem[] FieldProblems(Character character)
        => [.. validator.ValidateFieldsOnly(mock.GetCharacterInfo(character))];

    private ProjectFieldInfo AddStringField(MandatoryStatus mandatoryStatus, FieldBoundTo boundTo = FieldBoundTo.Character)
        => mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = boundTo;
            f.MandatoryStatus = mandatoryStatus;
            f.ValidForNpc = true;
        });

    /// <summary>Дропдаун с двумя вариантами, второй из которых удалён. Возвращает его id.</summary>
    private (ProjectFieldInfo Field, int InActiveVariantId) AddDropdownFieldWithInactiveVariant()
    {
        var field = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.MandatoryStatus = MandatoryStatus.Optional;
            f.ValidForNpc = true;
            f.ValuesOrdering = "";
            f.DropdownValues =
            [
                MakeVariant(1, isActive: true),
                MakeVariant(2, isActive: false),
            ];
        });

        return (field, 2);

        ProjectFieldDropdownValue MakeVariant(int id, bool isActive) => new()
        {
            ProjectFieldDropdownValueId = id,
            Label = $"Вариант {id}",
            IsActive = isActive,
            PlayerSelectable = true,
            ProjectId = mock.Project.ProjectId,
            Project = mock.Project,
        };
    }
}
