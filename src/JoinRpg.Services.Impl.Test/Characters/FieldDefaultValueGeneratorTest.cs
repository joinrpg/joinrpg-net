using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Characters;

/// <summary>
/// Пустое поле «Ведущий мероприятия» при пересохранении заполняется игроком принятой заявки.
/// </summary>
public class FieldDefaultValueGeneratorTest
{
    private readonly MockedProject mock = new();
    private readonly FieldDefaultValueGenerator generator = new();

    private FieldWithValue AuthorField(FieldBoundTo boundTo)
        => new(mock.AddField(f =>
        {
            f.FieldName = "Ведущий";
            f.FieldType = ProjectFieldType.ScheduleAuthorField;
            f.FieldBoundTo = boundTo;
        }), null);

    [Fact]
    public void CharacterWithApprovedClaim_AuthorIsPlayer()
    {
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);

        generator.CreateDefaultValue(mock.Character, AuthorField(FieldBoundTo.Character))
            .ShouldBe(mock.Player.UserId.ToString());
    }

    [Fact]
    public void CharacterWithoutApprovedClaim_NoDefault()
    {
        _ = mock.CreateClaim(mock.Character, mock.Player);

        generator.CreateDefaultValue(mock.Character, AuthorField(FieldBoundTo.Character)).ShouldBeNull();
    }

    [Fact]
    public void ApprovedClaim_AuthorIsPlayer()
    {
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);

        generator.CreateDefaultValue(claim, AuthorField(FieldBoundTo.Claim))
            .ShouldBe(mock.Player.UserId.ToString());
    }

    [Fact]
    public void NotApprovedClaim_NoDefault()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        generator.CreateDefaultValue(claim, AuthorField(FieldBoundTo.Claim)).ShouldBeNull();
    }
}
