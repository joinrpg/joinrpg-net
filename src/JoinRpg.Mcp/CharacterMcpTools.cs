using System.ComponentModel;
using JoinRpg.DomainTypes;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.XGameApi.Contract;
using ModelContextProtocol.Server;

namespace JoinRpg.Mcp;

[McpServerToolType]
public sealed class CharacterMcpTools(
    ICharacterApiViewService characterApiViewService,
    ISearchApiViewService searchApiViewService,
    McpAuthContext authContext)
{
    [McpServerTool(Name = "list_characters"), Description(
        "Персонажи, лежащие непосредственно в указанной группе или в её подгруппах (в т.ч. в спецгруппах " +
        "вариантов полей — id спецгрупп берутся из get_project_overview). Используй это, а не выгрузку " +
        "всех персонажей проекта с фильтрацией в контексте.")]
    public async Task<IReadOnlyCollection<CharacterHeader>> ListCharacters(
        [Description("Id проекта")] int projectId,
        [Description("Id группы персонажей (или спецгруппы поля/варианта)")] int groupId)
    {
        authContext.EnsureProjectGranted(projectId);
        var characters = await characterApiViewService.ListCharactersByGroup(
            new CharacterGroupIdentification(new ProjectIdentification(projectId), groupId));
        return [.. characters.Select(c => new CharacterHeader
        {
            CharacterId = c.CharacterId,
            UpdatedAt = c.UpdatedAt,
            IsActive = c.IsActive,
            CharacterLink = $"/x-game-api/{projectId}/characters/{c.CharacterId}/",
        })];
    }

    [McpServerTool(Name = "get_characters"), Description(
        "Полные данные нескольких персонажей по их id одним вызовом — не вызывай по одному персонажу, " +
        "сначала отбери id через list_characters или search_characters.")]
    public async Task<IReadOnlyCollection<CharacterInfo>> GetCharacters(
        [Description("Id проекта")] int projectId,
        [Description("Id персонажей одного проекта")] int[] characterIds)
    {
        authContext.EnsureProjectGranted(projectId);
        return await characterApiViewService.GetCharactersByIds(new ProjectIdentification(projectId), characterIds);
    }

    [McpServerTool(Name = "search_characters"), Description(
        "Полнотекстовый поиск персонажей по имени в пределах одного проекта. Возвращает только id и имя — " +
        "за деталями обращайся к get_characters по найденным id.")]
    public async Task<IReadOnlyCollection<CharacterSearchResult>> SearchCharacters(
        [Description("Id проекта")] int projectId,
        [Description("Поисковый запрос")] string query)
    {
        authContext.EnsureProjectGranted(projectId);
        return await searchApiViewService.SearchCharacters(new ProjectIdentification(projectId), query);
    }
}
