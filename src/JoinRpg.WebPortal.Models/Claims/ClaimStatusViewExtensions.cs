using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.Models;

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
