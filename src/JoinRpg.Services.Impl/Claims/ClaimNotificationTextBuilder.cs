using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Impl.Claims;

internal class ClaimNotificationTextBuilder(INotificationUriLocator<ClaimIdentification> uriService)
{
    internal string GetText(ClaimSimpleChangedNotification model, ClaimWithPlayer claim)
    {
        var commentExtraActionView = (CommonUI.Models.CommentExtraAction?)model.CommentExtraAction;

        var extraText = commentExtraActionView?.GetDisplayName();

        var actionName = commentExtraActionView?.GetShortName() ?? "откомментирована"; // Если CommentExtraAction = Null, это просто комментарий

        if (extraText != null)
        {
            extraText = "**" + extraText + "**\n\n";
        }

        if (model.Money is not null)
        {
            extraText += $"Сумма операции: {model.Money}\n\n";
        }

        if (model.CommentExtraAction == CommentExtraAction.MoveByMaster)
        {
            // TODO Раньше писалось на какую новую роль
        }

        var text1 =
$@"Добрый день, %recepient.name%
Заявка {claim.CharacterName} игрока {claim.Player.DisplayName.DisplayName} {actionName} {GetInitiatorString(model.ClaimOperationType, model.Initiator)}

{extraText}{model.Text.TemplateContents}

Страница заявки: {uriService.GetUri(model.ClaimId)}
";
        return text1;
    }

    private static string GetInitiatorString(ClaimOperationType claimOperationType, UserInfoHeader initiator)
    {
        return claimOperationType switch
        {
            ClaimOperationType.MasterSecretChange => $"мастером {initiator.DisplayName.DisplayName}",
            ClaimOperationType.MasterVisibleChange => $"мастером {initiator.DisplayName.DisplayName}",
            ClaimOperationType.PlayerChange => "игроком",
            _ => throw new ArgumentOutOfRangeException(nameof(claimOperationType), claimOperationType, null),
        };
    }

    public string GetClaimEmailTitle(ProjectName projectName, ClaimWithPlayer claim) => $"{projectName.Value}: {claim.CharacterName}, игрок {claim.Player.DisplayName.DisplayName}";
}
