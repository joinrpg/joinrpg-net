using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Characters;

/// <summary>
/// Поиск персонажей внутри одного проекта для x-api/MCP (ADR012). Проверяет права мастера
/// сама — <see cref="Services.Interfaces.ISearchService"/> под капотом рассчитан на общий
/// сайтовый поиск и такой проверки не делает.
/// </summary>
public interface ISearchApiViewService
{
    Task<IReadOnlyCollection<CharacterSearchResult>> SearchCharacters(ProjectIdentification projectId, string query);
}
