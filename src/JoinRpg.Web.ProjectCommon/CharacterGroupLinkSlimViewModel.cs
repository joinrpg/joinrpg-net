using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.ProjectCommon;

public record CharacterGroupLinkSlimViewModel(ProjectIdentification ProjectId, int CharacterGroupId, string Name, bool IsPublic, bool IsActive)
{
}
