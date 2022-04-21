using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.ClaimList;

public class ClaimListItemForExportViewModel : ClaimListItemViewModelBase
{
    public IReadOnlyCollection<FieldWithValue> Fields { get; }


    [Display(Name = "Уплачено")]
    public int FeePaid { get; }

    [Display(Name = "Осталось")]
    public int FeeDue { get; }

    [Display(Name = "Итого взнос")]
    public int TotalFee { get; }

    [Display(Name = "Тип поселения")]
    public string? AccomodationType { get; }


    [Display(Name = "Комната")]
    public string? RoomName { get; }

    [Display(Name = "Льготник")]
    public bool PreferentialFeeUser { get; }

    public ClaimListItemForExportViewModel(Claim claim, int currentUserId) : base(claim, currentUserId)
    {
        Fields = claim.GetFields();

        FeePaid = claim.ClaimBalance();
        FeeDue = claim.ClaimFeeDue();
        TotalFee = claim.ClaimTotalFee();

        PreferentialFeeUser = claim.PreferentialFeeUser;


        AccomodationType = claim.AccommodationRequest?.AccommodationType.Name;
        RoomName = claim.AccommodationRequest?.Accommodation?.Name;
    }
}
