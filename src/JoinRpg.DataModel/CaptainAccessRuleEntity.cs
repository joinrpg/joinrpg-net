using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.DataModel;
public class CaptainAccessRuleEntity : IProjectEntityWithId
{
    public required int CaptainAccessRuleEntityId { get; set; }
    public required int ProjectId { get; set; }

    public required int CharacterGroupId { get; set; }

    public required int CaptainUserId { get; set; }

    public required bool CanApprove { get; set; }

    public Project Project { get; set; }
    public CharacterGroup CharacterGroup { get; set; }
    public User CaptainUser { get; set; }

    int IOrderableEntity.Id => CaptainAccessRuleEntityId;
}
