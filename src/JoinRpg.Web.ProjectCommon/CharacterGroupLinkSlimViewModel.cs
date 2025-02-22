using JoinRpg.Data.Interfaces;

namespace JoinRpg.Web.ProjectCommon;

public record CharacterGroupLinkSlimViewModel(CharacterGroupIdentification CharacterGroupId, string Name, bool IsPublic, bool IsActive)
{
    public CharacterGroupLinkSlimViewModel(CharacterGroupHeaderDto dto) : this(dto.CharacterGroupId, dto.Name, dto.IsPublic, dto.IsActive)
    {
    }
}
