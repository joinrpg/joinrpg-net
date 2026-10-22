using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class CharacterNameFieldCandidatesTest
{
    private static IReadOnlyList<int> Candidates(ProjectInfo project)
        => [.. project.GetUnboundCharacterNameFieldCandidates().Select(f => f.Id.ProjectFieldId)];

    [Theory]
    [InlineData("Имя персонажа")]
    [InlineData("Имя")]
    [InlineData("  имя и титул")]
    [InlineData("Название мероприятия")]
    public void NameLikeField_WithoutNameField_IsCandidate(string name)
        => Candidates(Build(fields: [MakeField(1, name: name)])).ShouldBe([1]);

    [Theory]
    [InlineData("Описание")]
    [InlineData("Полное имя")]
    [InlineData("Мероприятие")]
    public void OtherField_IsNotCandidate(string name)
        => Candidates(Build(fields: [MakeField(1, name: name)])).ShouldBeEmpty();

    [Fact]
    public void NameFieldSelected_NoCandidates()
    {
        var nameField = MakeField(1, name: "Имя персонажа");
        Candidates(Build(fields: [nameField, MakeField(2, name: "Имя на бейдже")], nameField: nameField.Id))
            .ShouldBeEmpty();
    }

    [Fact]
    public void ClaimField_IsNotCandidate()
        => Candidates(Build(fields: [MakeField(1, boundTo: FieldBoundTo.Claim, name: "Имя игрока")])).ShouldBeEmpty();

    [Fact]
    public void NonStringField_IsNotCandidate()
        => Candidates(Build(fields: [MakeField(1, type: ProjectFieldType.Text, name: "Имя персонажа")])).ShouldBeEmpty();

    [Fact]
    public void InactiveField_IsNotCandidate()
        => Candidates(Build(fields: [MakeField(1, name: "Имя персонажа", isActive: false)])).ShouldBeEmpty();

    [Fact]
    public void SeveralCandidates_AllReturned()
        => Candidates(Build(fields: [MakeField(1, name: "Имя персонажа"), MakeField(2, name: "Описание"), MakeField(3, name: "Имя на бейдже")]))
            .ShouldBe([1, 3], ignoreOrder: true);
}
