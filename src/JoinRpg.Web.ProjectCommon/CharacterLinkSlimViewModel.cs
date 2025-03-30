using JoinRpg.Data.Interfaces;

namespace JoinRpg.Web.ProjectCommon;

public record CharacterLinkSlimViewModel(CharacterIdentification CharacterId, string Name, bool IsActive, ViewMode ViewMode)
{
    public CharacterLinkSlimViewModel(CharacterWithProject dto, bool hasMasterAccess)
        : this(dto.CharacterId, dto.CharacterName, dto.IsActive, ViewModeSelector.Create(dto.IsPublic, hasMasterAccess))
    {
    }
}
