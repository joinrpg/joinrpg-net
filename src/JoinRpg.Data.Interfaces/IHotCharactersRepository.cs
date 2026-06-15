using JoinRpg.DataModel;
using JoinRpg.Interfaces;

namespace JoinRpg.Data.Interfaces;

public interface IHotCharactersRepository
{
    Task<IReadOnlyCollection<CharacterWithProject>> GetHotCharactersFromPublicProjects(KeySetPagination? pagination = null);
}
public record class CharacterWithProject(
    CharacterIdentification CharacterId,
    string CharacterName,
    bool IsPublic,
    bool IsActive,
    ProjectName ProjectName,
    MarkdownDbValue CharacterDesc,
    MarkdownDbValue ProjectDesc,
    KogdaIgraIdentification[] KogdaIgraLinkedIds);
