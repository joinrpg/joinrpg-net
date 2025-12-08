using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Email;
internal class SubscribeCalculator(
    IUserSubscribeRepository userSubscribeRepository,
    ICharacterRepository characterRepository,
    IClaimsRepository claimsRepository,
    IProjectRepository projectRepository
    )
{
    internal async Task<IReadOnlyCollection<NotificationRecepient>> GetRecepients(SubscribeCalculateArgs args, ProjectInfo projectInfo)
    {
        Dictionary<UserIdentification, NotificationRecepient> list = [];

        AddUserInfoHeaderIfNotPresent(list, args.Finance, SubscriptionReason.Finance);
        AddUserInfoHeaderIfNotPresent(list, args.RespondingTo, SubscriptionReason.AnswerToYourComment);
        AddUserInfoHeaderIfNotPresent(list, args.Player, SubscriptionReason.Player);

        AddIfPredicateAndNotAlreadyPresent(args.RespMasters.WhereNotNull().Select(id => CreateForRespMaster(projectInfo, id)), SubscriptionReason.ResponsibleMaster);

        var claim = await userSubscribeRepository.GetDirect(args.Claims);
        AddIfPredicateAndNotAlreadyPresent(claim, SubscriptionReason.SubscribedDirectMaster);

        IReadOnlyCollection<CharacterIdentification> characterIds = [.. args.Characters.WhereNotNull()];
        var characters = await characterRepository.GetCharacters(characterIds);

        var character = await userSubscribeRepository.GetForCharAndGroups(
            [.. characters.SelectMany(x => x.GetParentGroupIdsToTop()).Distinct()],
            characterIds);
        AddIfPredicateAndNotAlreadyPresent(character, SubscriptionReason.SubscribedMaster);

        if (args.Initiator?.UserId is UserIdentification userId)
        {
            list.Remove(userId);
        }

        return list.Values;

        void AddIfPredicateAndNotAlreadyPresent(IEnumerable<UserSubscribe> subscribe, SubscriptionReason reason)
        {
            AddUserInfoHeaderIfNotPresent(list, subscribe.Where(s => args.Predicate(s.Options)).Select(x => x.User), reason);
        }
    }

    internal async Task<IReadOnlyCollection<NotificationRecepient>> GetRecepients(ForumCalculateArgs args, ProjectInfo projectInfo)
    {
        Dictionary<UserIdentification, NotificationRecepient> list = [];

        //TODO В форумах есть подписка, но она никогда не ставится, соответственно использовать ее тоже пока не надо

        AddUserInfoHeaderIfNotPresent(list, args.RespondingTo, SubscriptionReason.AnswerToYourComment);

        var groups = await projectRepository.LoadGroups(args.Groups);

        var claims = await claimsRepository.GetClaimHeadersWithPlayer([.. groups.SelectMany(g => g.GetChildrenGroupsIdentificationRecursiveIncludingThis())], ClaimStatusSpec.Approved);

        AddUserInfoHeaderIfNotPresent(list, claims.Select(c => c.Player), SubscriptionReason.Forum);

        AddUserInfoHeaderIfNotPresent(list, args.Masters, SubscriptionReason.MasterOfGame);

        if (args.Initiator?.UserId is UserIdentification userId)
        {
            list.Remove(userId);
        }

        return list.Values;
    }

    private static void AddUserInfoHeaderIfNotPresent(Dictionary<UserIdentification, NotificationRecepient> list, IEnumerable<UserInfoHeader?> subscribe, SubscriptionReason reason)
    {
        foreach (var r in subscribe
            .WhereNotNull()
            .Select(x => new NotificationRecepient(x.UserId, x.DisplayName.DisplayName, reason))
            )
        {
            list.TryAdd(r.UserId, r);
        }
    }

    private static UserSubscribe CreateForRespMaster(ProjectInfo projectInfo, UserIdentification responsibleMasterUserId)
    {
        return new UserSubscribe(
            projectInfo.Masters.Single(x => x.UserId == responsibleMasterUserId).UserInfo,
            SubscriptionOptions.CreateAllSet() with { AccommodationInvitesChange = false });
    }
}


internal record SubscribeCalculateArgs
    (Func<SubscriptionOptions, bool> Predicate,
     UserInfoHeader? Initiator,
     UserInfoHeader[] Player,
     UserIdentification?[] RespMasters,
     ClaimIdentification[] Claims,
     IReadOnlyCollection<CharacterIdentification?> Characters,
     UserInfoHeader?[] Finance,
     UserInfoHeader?[] RespondingTo
     );

internal record ForumCalculateArgs
    (UserInfoHeader Initiator,
     IReadOnlyCollection<CharacterGroupIdentification> Groups,
     IReadOnlyCollection<UserInfoHeader> RespondingTo,
     IReadOnlyCollection<UserInfoHeader> Masters
     );
