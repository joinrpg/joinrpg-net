using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.ProjectCommon;

public record CharacterDto(CharacterIdentification CharacterId, string Name, string Description, bool IsPublic)
{
}
