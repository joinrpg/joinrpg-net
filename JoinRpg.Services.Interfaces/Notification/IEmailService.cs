using System.Threading.Tasks;
using JetBrains.Annotations;

namespace JoinRpg.Services.Interfaces.Notification
{
    public interface IEmailService
    {
        Task Email([NotNull] AddCommentEmail model);
        Task Email([NotNull] ApproveByMasterEmail createClaimEmail);
        Task Email([NotNull] DeclineByMasterEmail createClaimEmail);
        Task Email([NotNull] DeclineByPlayerEmail createClaimEmail);
        Task Email([NotNull] NewClaimEmail createClaimEmail);
        Task Email([NotNull] RemindPasswordEmail email);
        Task Email([NotNull] ConfirmEmail email);
        Task Email([NotNull] RestoreByMasterEmail createClaimEmail);
        Task Email([NotNull] MoveByMasterEmail createClaimEmail);
        Task Email([NotNull] FinanceOperationEmail createClaimEmail);
        Task Email([NotNull] MassEmailModel model);
        Task Email([NotNull] ChangeResponsibleMasterEmail createClaimEmail);
        Task Email([NotNull] OnHoldByMasterEmail createClaimEmail);
        Task Email([NotNull] ForumEmail model);
        Task Email([NotNull] CheckedInEmal createClaimEmail);
        Task Email([NotNull] SecondRoleEmail createClaimEmail);
        Task Email([NotNull] FieldsChangedEmail filedsEmail);
        Task Email([NotNull] OccupyRoomEmail createClaimEmail);
        Task Email([NotNull] UnOccupyRoomEmail email);
        Task Email([NotNull] LeaveRoomEmail email);
        Task Email([NotNull] NewInviteEmail email);
        Task Email([NotNull] AcceptInviteEmail email);
        Task Email([NotNull] DeclineInviteEmail email);
        Task Email([NotNull] PublishPlotElementEmail email);
    }

    public static class EmailTokens
    {
        public const string Name = "%NAME%";
    }
}
