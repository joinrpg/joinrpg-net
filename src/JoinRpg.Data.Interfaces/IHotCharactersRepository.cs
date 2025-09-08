using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Data.Interfaces;

public interface IHotCharactersRepository
{
    Task<IReadOnlyCollection<CharacterWithProject>> GetHotCharactersFromAllProjects(KeySetPagination<CharacterIdentification>? pagination = null);
}
public record class CharacterWithProject(
    CharacterIdentification CharacterId,
    string CharacterName,
    bool IsPublic,
    bool IsActive,
    ProjectName ProjectName,
    MarkdownString CharacterDesc,
    MarkdownString ProjectDesc,
    KogdaIgraIdentification[] KogdaIgraLinkedIds);
