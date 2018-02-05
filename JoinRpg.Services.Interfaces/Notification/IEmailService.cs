using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces.Notification
{
    public interface IEmailService
    {
        Task Email(AddCommentEmail model);
        Task Email(ApproveByMasterEmail createClaimEmail);
        Task Email(DeclineByMasterEmail createClaimEmail);
        Task Email(DeclineByPlayerEmail createClaimEmail);
        Task Email(NewClaimEmail createClaimEmail);
        Task Email(RemindPasswordEmail email);
        Task Email(ConfirmEmail email);
        Task Email(RestoreByMasterEmail createClaimEmail);
        Task Email(MoveByMasterEmail createClaimEmail);
        Task Email(FinanceOperationEmail createClaimEmail);
        Task Email(MassEmailModel model);
        Task Email(ChangeResponsibleMasterEmail createClaimEmail);
        Task Email(OnHoldByMasterEmail createClaimEmail);
        Task Email(ForumEmail model);
        Task Email(CheckedInEmal createClaimEmail);
        Task Email(SecondRoleEmail createClaimEmail);
        Task Email(FieldsChangedEmail filedsEmail);
        Task Email(OccupyRoomEmail createClaimEmail);
        Task Email(UnOccupyRoomEmail email);
        Task Email(LeaveRoomEmail email);
    }

    public static class EmailTokens
    {
        public const string Name = "%NAME%";
    }
}
