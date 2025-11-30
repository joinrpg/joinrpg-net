using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.Domain;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Email;
internal class ClaimNotificationService(
    IClaimsRepository claimsRepository,
    IUriService uriService,
    INotificationService notificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IUserSubscribeRepository userSubscribeRepository,
    ICharacterRepository characterRepository
    ) : IClaimNotificationService

{
    public async Task SendNotification(ClaimSimpleChangedNotification model)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(model.ProjectId);


        var claim = (await claimsRepository.GetClaimHeadersWithPlayer([model.ClaimId])).Single();

        var commentExtraActionView = (CommonUI.Models.CommentExtraAction?)model.CommentExtraAction;

        var extraText = commentExtraActionView?.GetDisplayName();

        var actionName = commentExtraActionView?.GetShortName() ?? "изменена";

        if (extraText != null)
        {
            extraText = "**" + extraText + "**\n\n";
        }

        if (model.CommentExtraAction == CommentExtraAction.MoveByMaster)
        {
            // TODO Раньше писалось на какую новую роль
        }

        var text1 =
$@"Добрый день, %recepient.name%
Заявка {claim.CharacterName} игрока {claim.Player.DisplayName.DisplayName} {actionName} {GetInitiatorString(model.ClaimOperationType, model.Initiator)}

{extraText}{model.Text.TemplateContents}

Страница заявки: {uriService.Get(model.ClaimId)}
";

        await notificationService.QueueNotification(new NotificationEvent(
            NotificationClass.Claims,
            model.ClaimId,
            EmailModelHelpers.GetClaimEmailTitle(projectInfo.ProjectName, claim),
            new NotificationEventTemplate(text1),
            await GetRecepients(model, projectInfo, claim),
            model.Initiator.UserId
            ));
    }

    private async Task<IReadOnlyCollection<NotificationRecepient>> GetRecepients(ClaimSimpleChangedNotification model, ProjectInfo projectInfo, ClaimWithPlayer claimWithPlayer)
    {


        IReadOnlyCollection<UserSubscribe> character = await GetForCharacter(projectInfo, claimWithPlayer.CharacterId);

        if (model.AnotherCharacterId is not null)
        {
            character = [.. character.Union(await GetForCharacter(projectInfo, model.AnotherCharacterId))];
        }

        IReadOnlyCollection<UserSubscribe> claim = await userSubscribeRepository.GetDirect(claimWithPlayer.ClaimId);
        IReadOnlyCollection<UserSubscribe> resp = [
            CreateForRespMaster(projectInfo, claimWithPlayer.ResponsibleMasterUserId),
            CreateForRespMaster(projectInfo, model.OldResponsibleMaster ?? claimWithPlayer.ResponsibleMasterUserId), // Дубликаты удалят позже
            ];
        IReadOnlyCollection<UserSubscribe> player = [CreateForPlayer(claimWithPlayer.Player)];

        Func<SubscriptionOptions, bool> predicate = GetSubscribePredicate(model.CommentExtraAction);

        Dictionary<UserIdentification, NotificationRecepient> list = [];

        if (model.ClaimOperationType == ClaimOperationType.MasterVisibleChange)
        {
            // Меняется что-то видимое для игрока, добавляем его
            AddIfPredicateAndNotAlreadyPresent(player, SubscriptionReason.Player);
        }

        AddIfPredicateAndNotAlreadyPresent(resp, SubscriptionReason.ResponsibleMaster);

        AddIfPredicateAndNotAlreadyPresent(claim, SubscriptionReason.SubscribedDirectMaster);
        AddIfPredicateAndNotAlreadyPresent(character, SubscriptionReason.SubscribedMaster);

        return list.Values;

        void AddIfPredicateAndNotAlreadyPresent(IReadOnlyCollection<UserSubscribe> subscribe, SubscriptionReason reason)
        {
            foreach (var r in subscribe.Where(s => predicate(s.Options)).Select(x => new NotificationRecepient(x.User.UserId, x.User.DisplayName.DisplayName, reason)))
            {
                list.TryAdd(r.UserId, r);
            }
        }
    }

    private UserSubscribe CreateForRespMaster(ProjectInfo projectInfo, UserIdentification responsibleMasterUserId)
    {
        return new UserSubscribe(
            projectInfo.Masters.Single(x => x.UserId == responsibleMasterUserId).UserInfo,
            SubscriptionOptions.CreateAllSet() with { AccommodationInvitesChange = false });
    }

    private async Task<IReadOnlyCollection<UserSubscribe>> GetForCharacter(ProjectInfo projectInfo, CharacterIdentification characterId)
    {
        return await userSubscribeRepository.GetForCharAndGroups(await GetCharacterGroups(projectInfo, characterId), characterId);
    }

    private Func<SubscriptionOptions, bool> GetSubscribePredicate(CommentExtraAction? commentExtraAction)
    {
        return commentExtraAction switch
        {
            CommentExtraAction.ApproveFinance or CommentExtraAction.RejectFinance or CommentExtraAction.FeeChanged or CommentExtraAction.RequestPreferential
            => s => s.MoneyOperation,
            CommentExtraAction.ApproveByMaster or CommentExtraAction.DeclineByMaster or CommentExtraAction.RestoreByMaster
            or CommentExtraAction.MoveByMaster or CommentExtraAction.DeclineByPlayer or CommentExtraAction.ChangeResponsible
            or CommentExtraAction.OnHoldByMaster or CommentExtraAction.CheckedIn or CommentExtraAction.SecondRole or CommentExtraAction.OutOfGame
            or CommentExtraAction.NewClaim
                => s => s.ClaimStatusChange,
            null => s => s.Comments,
            _ => throw new ArgumentOutOfRangeException(nameof(commentExtraAction), commentExtraAction, "Неожиданное значение")
        };
    }

    private UserSubscribe CreateForPlayer(UserInfoHeader user) => new UserSubscribe(user, SubscriptionOptions.CreateAllSet());

    private async Task<IReadOnlyCollection<CharacterGroupIdentification>> GetCharacterGroups(ProjectInfo projectInfo, CharacterIdentification characterId)
    {
        //TODO иметь список групп в projectInfo и вычислять по нему
        _ = projectInfo;
        var character = await characterRepository.GetCharacterAsync(characterId);
        return [.. character.GetParentGroupIdsToTop()];
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
}
