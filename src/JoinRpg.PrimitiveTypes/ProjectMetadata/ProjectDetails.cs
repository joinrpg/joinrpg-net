using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes.ProjectMetadata;
/// <summary>
/// Сюда добавляем те свойства проекта, которые большие и нет смысла грузить на каждой странице, например анонс, связи с КогдаИгрой etc
/// </summary>
public record ProjectDetails(
    MarkdownString ProjectDescription,
    IReadOnlyCollection<KogdaIgraIdentification> KogdaIgraLinkedIds,
    bool DisableKogdaIgraMapping
    );
