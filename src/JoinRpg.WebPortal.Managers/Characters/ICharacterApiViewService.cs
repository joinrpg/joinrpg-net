using System.Text.Json;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Characters;

/// <summary>
/// Общий слой над персонажами для x-game-api и (в будущем) MCP-инструментов (ADR012).
/// Каждый метод сам проверяет права мастера — вызывающая сторона может доверять результату.
/// </summary>
public interface ICharacterApiViewService
{
    Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(ProjectIdentification projectId, DateTime? modifiedSince);

    Task<CharacterInfo> GetCharacterInfo(CharacterIdentification characterId);

    // TODO пробросить в x-game-api (сейчас вызывается только будущим MCP-слоем). См. #4798.

    /// <summary>Персонажи по конкретным id одного проекта — не выгрузка всех подряд.</summary>
    Task<IReadOnlyCollection<CharacterInfo>> GetCharactersByIds(ProjectIdentification projectId, IReadOnlyCollection<int> characterIds);

    /// <summary>Персонажи группы, включая вложенные подгруппы (в т.ч. спецгруппы вариантов полей).</summary>
    Task<IReadOnlyCollection<CharacterInfo>> ListCharactersByGroup(CharacterGroupIdentification groupId);

    Task<CharacterHeader> CreateCharacter(ProjectIdentification projectId, CreateCharacterRequest request);

    Task SetCharacterFields(CharacterIdentification characterId, Dictionary<int, JsonElement> fieldValues);
}
