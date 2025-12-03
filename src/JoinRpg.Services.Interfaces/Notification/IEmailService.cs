namespace JoinRpg.Services.Interfaces.Notification;

public interface IEmailService
{
    Task Email(NewClaimEmail createClaimEmail);

    Task Email(ForumEmail model);
    Task Email(FieldsChangedEmail filedsEmail);
    Task Email(OccupyRoomEmail createClaimEmail);
    Task Email(UnOccupyRoomEmail email);
    Task Email(LeaveRoomEmail email);
    Task Email(NewInviteEmail email);
    Task Email(AcceptInviteEmail email);
    Task Email(DeclineInviteEmail email);
}
