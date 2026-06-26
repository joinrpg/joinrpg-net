namespace JoinRpg.DomainTypes.Characters.Claims;

public record class CaptainAccessRule
    (CharacterGroupIdentification CharacterGroup, UserIdentification Player, bool CanApprove)
{
    public ProjectIdentification ProjectId => CharacterGroup.ProjectId;
}
