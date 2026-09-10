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

    Task<CharacterHeader> CreateCharacter(ProjectIdentification projectId, CreateCharacterRequest request);

    Task SetCharacterFields(CharacterIdentification characterId, Dictionary<int, JsonElement> fieldValues);
}
