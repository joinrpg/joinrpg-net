using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.DomainTypes.ProjectMetadata;
/// <summary>
/// Сюда добавляем те свойства проекта, которые большие и нет смысла грузить на каждой странице, например анонс, связи с КогдаИгрой etc
/// </summary>
/// <remarks>
/// В <see cref="ProjectInfo"/> такие поля не кладём: он собирается на каждый запрос. Получать —
/// через <c>IProjectMetadataRepository.GetProjectDetails</c>, а не с EF-сущности (docs/project-entities.md).
/// </remarks>
public record ProjectDetails(
    ProjectInfo ProjectInfo,
    MarkdownString ProjectDescription,
    MarkdownString ClaimApplyRules,
    IReadOnlyCollection<KogdaIgraGameData> KogdaIgraCards,
    bool DisableKogdaIgraMapping,
    MarkdownString PreferentialFeeConditions,
    IReadOnlyCollection<PaymentTypePlayerInfo> PlayerPaymentTypes)
{
    public KogdaIgraGameData? NearestFutureKogdaIgraCard =>
        KogdaIgraCards
            .Where(g => g.IsActive && g.Begin > DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderBy(g => g.Begin)
            .FirstOrDefault();
}
