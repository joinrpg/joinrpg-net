using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Characters
{
    /// <summary>
    /// Make character inactive
    /// </summary>
    public record DeleteCharacterRequest(
        CharacterIdentification Id
        )
    { }
}
