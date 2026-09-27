using System.ComponentModel;
using JoinRpg.DomainTypes;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.XGameApi.Contract;
using ModelContextProtocol;
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
    public async Task<IReadOnlyCollection<CharacterListItem>> ListCharacters(
        [Description("Id проекта")] int projectId,
        [Description("Id группы персонажей (или спецгруппы поля/варианта)")] int groupId)
    {
        authContext.EnsureProjectGranted(projectId);
        var characters = await characterApiViewService.ListCharactersByGroup(
            new CharacterGroupIdentification(new ProjectIdentification(projectId), groupId));
        return [.. characters.Select(c => new CharacterListItem
        {
            CharacterId = c.CharacterId,
            CharacterName = c.CharacterName,
            IsActive = c.IsActive,
        })];
    }

    [McpServerTool(Name = "get_characters"), Description(
        "Полные данные нескольких персонажей по их id одним вызовом — не вызывай по одному персонажу, " +
        "сначала отбери id через list_characters или search_characters.")]
    public async Task<IReadOnlyCollection<CharacterInfo>> GetCharacters(
        [Description("Id проекта")] int projectId,
        // Именно List<int>, а не int[] и не IReadOnlyList<int>. Autofac неявно разрешает
        // массивы и интерфейсные коллекции как «все регистрации элемента»: для int это пустой
        // массив, то есть GetService(typeof(int[])) успешно возвращает [] . SDK из этого
        // заключает, что параметр приходит из контейнера, убирает его из схемы инструмента и
        // подставляет пустоту сам. Клиент такой параметр передать не может в принципе — он его
        // не видит. List<int> Autofac не разрешает, поэтому параметр остаётся в схеме.
        // Проверено на всех формах, см. CharacterMcpToolsSchemaTests.
        [Description("Id персонажей одного проекта")] List<int> characterIds)
    {
        authContext.EnsureProjectGranted(projectId);

        // Пустой список id иначе молча давал пустой результат, и отличить «таких персонажей
        // нет» от «аргумент не доехал» было нечем.
        if (characterIds is not { Count: > 0 })
        {
            throw new McpException(
                "Не передан characterIds — список id персонажей обязателен. "
                + "Отбери id через list_characters или search_characters и передай их массивом.");
        }

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
