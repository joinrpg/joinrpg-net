using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Interfaces;

public interface IHotCharactersRepository
{
    Task<IReadOnlyCollection<CharacterWithProject>> GetHotCharactersFromAllProjects(KeySetPagination<CharacterIdentification>? pagination = null);
}
