using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
    public enum ClaimStatusView
    {
        [Display(Name = "Подана"), UsedImplicitly]
        AddedByUser,

        [Display(Name = "Предложена"), UsedImplicitly]
        AddedByMaster,

        [Display(Name = "Принята"), UsedImplicitly]
        Approved,

        [Display(Name = "Отозвана"), UsedImplicitly]
        DeclinedByUser,

        [Display(Name = "Отклонена"), UsedImplicitly]
        DeclinedByMaster,

        [Display(Name = "Обсуждается"), UsedImplicitly]
        Discussed,

        [Display(Name = "В листе ожидания"), UsedImplicitly]
        OnHold,

        [Display(Name = "Игрок заехал"), UsedImplicitly]
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
            => ((Claim.Status)fromStatus.ClaimStatus).CanChangeTo((Claim.Status)targetStatus);

        // TODO A lot of checks should be unified to Domain (like CanChangeTo)

        public static bool CanMove(this ClaimFullStatusView status)
            => status.ClaimStatus == ClaimStatusView.AddedByUser ||
               status.ClaimStatus == ClaimStatusView.Approved ||
               status.ClaimStatus == ClaimStatusView.Discussed ||
               status.ClaimStatus == ClaimStatusView.OnHold;


        public static bool IsActive(this ClaimFullStatusView status) =>
            ((Claim.Status)status.ClaimStatus).IsActive();

        public static bool IsAlreadyApproved(this ClaimFullStatusView status) =>
            status.ClaimStatus == ClaimStatusView.Approved ||
            status.ClaimStatus == ClaimStatusView.CheckedIn;

        public static bool CanHaveSecondRole(this ClaimFullStatusView status) =>
            status.ClaimStatus == ClaimStatusView.CheckedIn;

    }
}
