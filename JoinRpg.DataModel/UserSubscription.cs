using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace JoinRpg.DataModel
{
  // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by EF

    public class UserSubscription:IValidatableObject, ISubscriptionOptions
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
    public bool MoneyOperation { get; set; }

      public bool AccommodationChange { get; set; }

      public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
      {
          if (new[] {CharacterGroupId, CharacterId, ClaimId}.Count(id => id != null) != 1)
          {
              yield return new ValidationResult(
                  "Should link to the only one of kind (CharacterGroup, Character, Claim)");
          }
      }
    }
}
