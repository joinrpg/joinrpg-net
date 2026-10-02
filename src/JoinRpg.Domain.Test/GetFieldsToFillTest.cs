using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Test;

/// <summary>
/// <see cref="CharacterInfo.GetFieldsToFill"/> — поля, которые на этом уровне есть кому заполнять.
/// Правило симметрично для персонажа и для заявки, и обе его стороны считает один метод, поэтому
/// здесь проверяются обе: порознь они разъехались бы, а расчёт проблемы «поле не заполнено»
/// опирается ровно на этот набор.
/// </summary>
public class GetFieldsToFillTest
{
    private readonly MockedProject mock = new();

    private ProjectFieldInfo CharacterField { get; }
    private ProjectFieldInfo ClaimField { get; }

    public GetFieldsToFillTest()
    {
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
    public void FreeCharacter_IsNotAskedForClaimFields()
        => FieldsToFill().ShouldBe([FieldId(CharacterField)]);

    [Fact]
    public void CharacterWithApprovedClaim_IsAskedForBothLayers()
    {
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        FieldsToFill().ShouldBe([FieldId(CharacterField), FieldId(ClaimField)], ignoreOrder: true);
    }

    /// <summary>
    /// Неутверждённая заявка роль не занимает, поэтому поля заявки с персонажа не спрашиваются —
    /// иначе у свободной роли висела бы ложная проблема.
    /// </summary>
    [Fact]
    public void CharacterWithPendingClaimOnly_IsNotAskedForClaimFields()
    {
        _ = mock.CreateClaim(mock.Character, mock.Player);

        FieldsToFill().ShouldBe([FieldId(CharacterField)]);
    }

    [Fact]
    public void PendingClaim_IsNotAskedForCharacterFields()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        FieldsToFill(claim).ShouldBe([FieldId(ClaimField)]);
    }

    [Fact]
    public void ApprovedClaim_IsAskedForBothLayers()
    {
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        FieldsToFill(claim).ShouldBe([FieldId(CharacterField), FieldId(ClaimField)], ignoreOrder: true);
    }

    /// <summary>
    /// Набор полей — это подмножество полного набора, а не другой его расчёт: значения должны
    /// совпадать с <see cref="CharacterInfo.GetAllFields"/> поле в поле.
    /// </summary>
    [Fact]
    public void FieldsToFill_AreSubsetOfAllFieldsWithSameValues()
    {
        MockedProject.AssignFieldValues(
            mock.Character,
            [new FieldWithValue(CharacterField, "персонаж"), new FieldWithValue(ClaimField, null)]);

        var character = mock.GetCharacterInfo(mock.Character);
        var all = character.GetAllFields().ToDictionary(f => f.Field.Id, f => f.Value);

        foreach (var field in character.GetFieldsToFill())
        {
            all.ShouldContainKey(field.Field.Id);
            field.Value.ShouldBe(all[field.Field.Id]);
        }
    }

    /// <summary>Поля, которые спрашиваются с персонажа.</summary>
    private IReadOnlyCollection<int> FieldsToFill()
        => [.. mock.GetCharacterInfo(mock.Character).GetFieldsToFill().Select(FieldId)];

    /// <summary>Поля, которые спрашиваются с конкретной заявки.</summary>
    private IReadOnlyCollection<int> FieldsToFill(Claim claim)
        => [.. mock.GetCharacterInfo(mock.Character).GetFieldsToFill(claim.GetId()).Select(FieldId)];

    /// <remarks>
    /// Сравниваем по id, а не по экземпляру <see cref="ProjectFieldInfo"/>: каждый
    /// <c>mock.AddField</c> пересобирает метаданные проекта, и поле, захваченное первым вызовом,
    /// становится устаревшим экземпляром, не равным по ссылке тому, что лежит в агрегате.
    /// </remarks>
    private static int FieldId(FieldWithValue field) => field.Field.Id.ProjectFieldId;

    private static int FieldId(ProjectFieldInfo field) => field.Id.ProjectFieldId;
}
