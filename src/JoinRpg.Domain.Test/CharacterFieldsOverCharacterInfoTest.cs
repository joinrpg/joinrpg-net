using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Значения полей персонажа должны читаться одинаково через EF-путь
/// (<c>Character.GetFields</c>) и через доменный агрегат (<see cref="CharacterInfo.GetAllFields"/>),
/// пока живы оба. Вьюмодели переезжают на агрегат (ADR013), и расхождение здесь означало бы,
/// что страница начнёт показывать не то, что показывала раньше.
/// </summary>
public class CharacterFieldsOverCharacterInfoTest
{
    private readonly MockedProject mock = new();

    private ProjectFieldInfo CharacterField { get; }
    private ProjectFieldInfo ClaimField { get; }

    public CharacterFieldsOverCharacterInfoTest()
    {
        // Именно AddField: он кладёт поле в Project, а не только в ProjectInfo, и потому переживает
        // перестроение метаданных, которое делает следующий AddField.
        CharacterField = MakeField(FieldBoundTo.Character);
        ClaimField = MakeField(FieldBoundTo.Claim);
    }

    private ProjectFieldInfo MakeField(FieldBoundTo boundTo)
        => mock.AddField(field =>
        {
            field.FieldType = ProjectFieldType.String;
            field.FieldBoundTo = boundTo;
        });

    [Fact]
    public void CharacterWithoutClaimsShouldMatch()
    {
        SetCharacterFields(characterBound: "персонаж");

        ShouldMatchEntityPath();
    }

    [Fact]
    public void ClaimBoundValueOfApprovedClaimShouldMatch()
    {
        SetCharacterFields(characterBound: "персонаж");
        SetApprovedClaimFields(claimBound: "заявка");

        ShouldMatchEntityPath();
    }

    [Fact]
    public void EmptyCharacterShouldMatch() => ShouldMatchEntityPath();

    /// <summary>
    /// Единственное известное расхождение: character-bound поле записано и в персонажа, и в JSON
    /// утверждённой заявки. EF-путь отдаёт значение персонажа, агрегат — значение заявки.
    /// </summary>
    /// <remarks>
    /// Побеждать должен агрегат: путь записи (<c>FieldSaveStrategyBase</c>) давно считает слои
    /// именно так, и при следующем сохранении значение заявки всё равно затрёт персонажа. То есть
    /// сегодня показ и запись расходятся между собой, а переезд на агрегат их мирит.
    ///
    /// Данных с таким перекрытием быть не должно: при принятии заявки поля пересохраняются
    /// (<c>SaveToCharacterAndClaimStrategy</c>), и в заявке остаются только claim-bound значения.
    /// Уцелеть оно может лишь в старых строках, записанных до появления этого правила.
    /// </remarks>
    [Fact]
    public void OverlappingCharacterBoundValueIsTakenFromClaimLayer()
    {
        SetCharacterFields(characterBound: "персонаж");
        SetApprovedClaimFields(characterBound: "заявка");

        EntityValue(CharacterField).ShouldBe("персонаж");
        AggregateValue(CharacterField).ShouldBe("заявка");
    }

    private void SetCharacterFields(string? characterBound = null, string? claimBound = null)
        => MockedProject.AssignFieldValues(mock.Character, Fields(characterBound, claimBound));

    private void SetApprovedClaimFields(string? characterBound = null, string? claimBound = null)
        => MockedProject.AssignFieldValues(
            mock.CreateApprovedClaim(mock.Character, mock.Player),
            Fields(characterBound, claimBound));

    private FieldWithValue[] Fields(string? characterBound, string? claimBound)
        =>
        [
            new FieldWithValue(CharacterField, characterBound),
            new FieldWithValue(ClaimField, claimBound),
        ];

    private string? EntityValue(ProjectFieldInfo field)
        => mock.Character.GetFields(mock.ProjectInfo).Single(f => f.Field.Id == field.Id).Value;

    private string? AggregateValue(ProjectFieldInfo field)
        => mock.GetCharacterInfo(mock.Character).GetAllFields().Single(f => f.Field.Id == field.Id).Value;

    private void ShouldMatchEntityPath()
    {
        var fromAggregate = mock.GetCharacterInfo(mock.Character).GetAllFields()
            .ToDictionary(f => f.Field.Id, f => f.Value);
        var fromEntity = mock.Character.GetFields(mock.ProjectInfo)
            .ToDictionary(f => f.Field.Id, f => f.Value);

        fromAggregate.ShouldBe(fromEntity, ignoreOrder: true);
    }
}
