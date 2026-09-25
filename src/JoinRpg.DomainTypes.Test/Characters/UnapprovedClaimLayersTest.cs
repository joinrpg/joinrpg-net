using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.DomainTypes.Test.ProjectInfoFixture;

namespace JoinRpg.DomainTypes.Test.Characters;

/// <summary>
/// Слои глазами ещё не утверждённой заявки: игрок такой заявки доступа к персонажу не имеет,
/// поэтому из персонажа ему видны только публичные поля.
/// </summary>
public class UnapprovedClaimLayersTest
{
    private const int PublicFieldId = 1;
    private const int NonPublicFieldId = 2;

    /// <remarks>
    /// Публичность поля (<c>IsPublic</c>) — это <see cref="ProjectFieldVisibility.Public"/>,
    /// то есть видно всем, а не только игроку с доступом.
    /// </remarks>
    private static ProjectInfo Project => MakeProject(
        MakeField(PublicFieldId, visibility: ProjectFieldVisibility.Public),
        MakeField(NonPublicFieldId, visibility: ProjectFieldVisibility.PlayerAndMaster));

    [Fact]
    public void NonPublicCharacterValueShouldNotLeakIntoUnapprovedClaim()
    {
        var project = Project;
        var layers = CharacterFieldLayers.ForUnapprovedClaim(
            claimLayer: Layer(project),
            characterLayer: Layer(project, (PublicFieldId, "видно всем"), (NonPublicFieldId, "тайна персонажа")),
            AccessArgumentsPlayer);

        layers.CharacterLayer.LayerData.Keys.Select(id => id.ProjectFieldId)
            .ShouldBe([PublicFieldId]);
    }

    [Fact]
    public void PublicCharacterValueShouldBeVisible()
    {
        var project = Project;
        var layers = CharacterFieldLayers.ForUnapprovedClaim(
            claimLayer: Layer(project),
            characterLayer: Layer(project, (PublicFieldId, "видно всем")),
            AccessArgumentsPlayer);

        layers.GetFieldValue(new ProjectFieldIdentification(project.ProjectId, PublicFieldId))?.Value
            .ShouldBe("видно всем");
    }

    /// <summary>
    /// Слой самой заявки не фильтруется: это то, что игрок в ней и заполнил.
    /// </summary>
    [Fact]
    public void ClaimLayerShouldNotBeFiltered()
    {
        var project = Project;
        var layers = CharacterFieldLayers.ForUnapprovedClaim(
            claimLayer: Layer(project, (NonPublicFieldId, "введено в заявке")),
            characterLayer: Layer(project),
            AccessArgumentsPlayer);

        layers.ClaimLayer!.LayerData.Keys.Select(id => id.ProjectFieldId)
            .ShouldBe([NonPublicFieldId]);
    }

    /// <summary>
    /// Страж от вырожденных проверок выше: без фильтрации непубличное значение персонажа видно.
    /// </summary>
    [Fact]
    public void WithoutFilteringNonPublicValueIsVisible()
    {
        var project = Project;
        var layers = new CharacterFieldLayers(
            Layer(project),
            Layer(project, (NonPublicFieldId, "тайна персонажа")),
            AccessArgumentsPlayer);

        layers.CharacterLayer.LayerData.Keys.Select(id => id.ProjectFieldId)
            .ShouldBe([NonPublicFieldId]);
    }

    private static FieldLayerContainer Layer(ProjectInfo project, params (int FieldId, string Value)[] values)
        => new(project, values.ToDictionary(v => v.FieldId, v => (string?)v.Value));
}
