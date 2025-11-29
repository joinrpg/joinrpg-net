using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Data.Interfaces;

public interface IHotCharactersRepository
{
    Task<IReadOnlyCollection<CharacterWithProject>> GetHotCharactersFromAllProjects(KeySetPagination? pagination = null);
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
