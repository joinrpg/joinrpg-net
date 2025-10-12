using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Web.Models;

public enum ClaimStatusView
{
    [Display(Name = "Подана")]
    AddedByUser,

    [Display(Name = "Предложена")]
    AddedByMaster,

    [Display(Name = "Принята")]
    Approved,

    [Display(Name = "Отозвана")]
    DeclinedByUser,

    [Display(Name = "Отклонена")]
    DeclinedByMaster,

    [Display(Name = "Обсуждается")]
    Discussed,

    [Display(Name = "В листе ожидания")]
    OnHold,

    [Display(Name = "Игрок заехал")]
    CheckedIn,
}

public class ClaimFullStatusView
{
    public ClaimFullStatusView(Claim claim, AccessArguments accessArguments)
    {
        AccessArguments = accessArguments;
        ClaimStatus = (ClaimStatusView)claim.ClaimStatus;
        ClaimDenialStatus = (ClaimDenialStatusView?)claim.ClaimDenialStatus;
    }

    [Display(Name = "Статус")]
    public ClaimStatusView ClaimStatus { get; }

    [Display(Name = "Причина отказа")]
    public ClaimDenialStatusView? ClaimDenialStatus { get; }

    public AccessArguments AccessArguments { get; }
}



public static class ClaimStatusViewExtensions
{

    public static bool CanChangeTo(this ClaimFullStatusView fromStatus,
        ClaimStatusView targetStatus)
        => ((ClaimStatus)fromStatus.ClaimStatus).CanChangeTo((ClaimStatus)targetStatus);

    // TODO A lot of checks should be unified to Domain (like CanChangeTo)

    public static bool CanMove(this ClaimFullStatusView status)
        => status.ClaimStatus == ClaimStatusView.AddedByUser ||
           status.ClaimStatus == ClaimStatusView.Approved ||
           status.ClaimStatus == ClaimStatusView.Discussed ||
           status.ClaimStatus == ClaimStatusView.OnHold;


    public static bool IsActive(this ClaimFullStatusView status) =>
        ((ClaimStatus)status.ClaimStatus).IsActive();

    public static bool IsAlreadyApproved(this ClaimFullStatusView status) =>
        status.ClaimStatus == ClaimStatusView.Approved ||
        status.ClaimStatus == ClaimStatusView.CheckedIn;

    public static bool CanHaveSecondRole(this ClaimFullStatusView status) =>
        status.ClaimStatus == ClaimStatusView.CheckedIn;

}
