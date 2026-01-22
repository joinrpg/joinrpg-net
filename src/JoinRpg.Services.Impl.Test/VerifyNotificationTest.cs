using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.Users;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test;

public class VerifyNotificationTest
{
    private readonly ClaimNotificationTextBuilder claimNotificationTextBuilder = new(new UriMock());

    private static readonly UserInfoHeader player = new(new UserIdentification(1), new UserDisplayName("PlayerName", null));
    private static readonly UserInfoHeader master = new(new UserIdentification(2), new UserDisplayName("Master", null));
    private readonly ClaimWithPlayer claimWithPlayer = new()
    {
        CharacterId = new CharacterIdentification(1, 1),
        CharacterName = "CharacterName",
        ClaimId = new ClaimIdentification(1, 1),
        ExtraNicknames = "",
        Player = player,
        ResponsibleMasterUserId = master.UserId,
    };

    private readonly CommentExtraAction[] financeActions =
    [
        CommentExtraAction.FeeChanged,
        CommentExtraAction.PaidFee,
        CommentExtraAction.RefundFee,
        CommentExtraAction.FeeChanged,
    ];

    [Theory]
    [ClassData(typeof(EnumTheoryDataGenerator<CommentExtraAction>))]
    public Task Text(CommentExtraAction commentExtraAction)
    {
        var model = new ClaimSimpleChangedNotification(
            new ClaimIdentification(1, 1),
            commentExtraAction,
            Initiator: master,
            new NotificationEventTemplate(""),
            ClaimOperationType.MasterVisibleChange
            );


        if (financeActions.Contains(commentExtraAction))
        {
            model = model with { Money = 300 };
        }

        var text = claimNotificationTextBuilder.GetText(model, claimWithPlayer);

        return Verify(text).UseParameters(commentExtraAction);
    }

    [Fact]
    public Task TextWithoutExtraAction()
    {
        var model = new ClaimSimpleChangedNotification(
            new ClaimIdentification(1, 1),
            CommentExtraAction: null,
            Initiator: master,
            new NotificationEventTemplate("Здесь длинный занимательный комментарий"),
            ClaimOperationType.MasterVisibleChange
            );

        var text = claimNotificationTextBuilder.GetText(model, claimWithPlayer);

        return Verify(text);
    }

    private class UriMock : INotificationUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"https://joinrpg.ru/{target.ProjectId.Value}/claim/{target.ClaimId}/edit");
    }
}
