using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.DataModel;

public class AccommodationRequest
{
    [Key]
    public int Id { get; set; }

    public int ProjectId { get; set; }
    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; }

    [Obsolete("Нельзя использовать за пределами Dal.Impl (ADR022). Состав группы — из RoomCategoryPlan (AccommodationGroupInfo.Subjects) или CharacterClaimInfo.AccommodationGroupId; менять состав — внешним ключом Claim.AccommodationRequest_Id.", DiagnosticId = "JOIN001")]
    public virtual ICollection<Claim> Subjects { get; set; }

    public int AccommodationTypeId { get; set; }
    [ForeignKey("AccommodationTypeId")]
    [Obsolete("Нельзя использовать за пределами Dal.Impl (ADR022). Тип группы — CharacterClaimInfo.AccommodationTypeId или AccommodationGroupInfo.AccommodationTypeId из RoomCategoryPlan, сам тип — ProjectInfo.AccommodationSettings (ADR015).", DiagnosticId = "JOIN001")]
    public virtual ProjectAccommodationType AccommodationType { get; set; }

    public int? AccommodationId { get; set; }
    [ForeignKey("AccommodationId")]
    [Obsolete("Нельзя использовать за пределами Dal.Impl (ADR022). Комната группы — из RoomCategoryPlan: AccommodationGroupInfo.RoomId, FindRoomByClaim.", DiagnosticId = "JOIN001")]
    public virtual ProjectAccommodation? Accommodation { get; set; }

    public InviteState IsAccepted { get; set; }
}
