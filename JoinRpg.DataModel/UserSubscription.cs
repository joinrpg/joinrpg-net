using System.Linq;

namespace JoinRpg.DataModel
{
  public class UserSubscription
  {
    public int UserSubscriptionId { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; }

    public int ProjectId { get; set; }
    public virtual Project Project {  get; set; }

    public int? CharacterGroupId { get; set; }
    public CharacterGroup CharacterGroup { get; set; }

    public int? CharacterId { get; set; }
    public virtual Character Character { get; set; }

    public int? ClaimId { get; set; }
    public virtual Claim Claim { get; set; }

    public bool ClaimStatusChange { get; set; }
    public bool Comments { get; set; }
    public bool FieldChange { get; set; }

    //TODO: This should be indepedent subscription
    public bool MoneyOperation
      => Project.ProjectAcls.SingleOrDefault(acl => acl.User == this.User)?.CanManageMoney ?? false;
  }
}
