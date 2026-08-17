namespace JoinRpg.DomainTypes.ProjectMetadata;
/// <summary>
/// Сюда добавляем те свойства проекта, которые большие и нет смысла грузить на каждой странице, например анонс, связи с КогдаИгрой etc
/// </summary>
public record ProjectDetails(
    MarkdownString ProjectDescription,
    IReadOnlyCollection<KogdaIgraGameData> KogdaIgraCards,
    bool DisableKogdaIgraMapping)
{
    public KogdaIgraGameData? NearestFutureKogdaIgraCard =>
        KogdaIgraCards
            .Where(g => g.IsActive && g.Begin > DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderBy(g => g.Begin)
            .FirstOrDefault();
}
