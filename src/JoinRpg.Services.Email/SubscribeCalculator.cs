using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Notifications;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Services.Email;
internal class SubscribeCalculator(
    IUserSubscribeRepository userSubscribeRepository,
    ICharacterRepository characterRepository
    )
{
    internal async Task<IReadOnlyCollection<NotificationRecepient>> GetRecepients(SubscribeCalculateArgs args, ProjectInfo projectInfo)
    {
        Dictionary<UserIdentification, NotificationRecepient> list = [];

        AddUserInfoHeaderIfNotPresent([.. args.Finance], SubscriptionReason.Finance);
        AddUserInfoHeaderIfNotPresent([.. args.RespondingTo], SubscriptionReason.AnswerToYourComment);
        AddUserInfoHeaderIfNotPresent([.. args.Player], SubscriptionReason.Player);

        AddIfPredicateAndNotAlreadyPresent([.. args.RespMasters.WhereNotNull().Select(id => CreateForRespMaster(projectInfo, id))], SubscriptionReason.ResponsibleMaster);

        var claim = await userSubscribeRepository.GetDirect(args.Claims);
        AddIfPredicateAndNotAlreadyPresent(claim, SubscriptionReason.SubscribedDirectMaster);

        IReadOnlyCollection<CharacterIdentification> characterIds = [.. args.Characters.WhereNotNull()];
        var characters = await characterRepository.GetCharacters(characterIds);

        var character = await userSubscribeRepository.GetForCharAndGroups(
            [.. characters.SelectMany(x => x.GetParentGroupIdsToTop()).Distinct()],
            characterIds);
        AddIfPredicateAndNotAlreadyPresent(character, SubscriptionReason.SubscribedMaster);

        return list.Values;

        void AddIfPredicateAndNotAlreadyPresent(IReadOnlyCollection<UserSubscribe> subscribe, SubscriptionReason reason)
        {
            foreach (var r in subscribe
                .Where(s => args.Predicate(s.Options))
                .Where(s => s.User.UserId != args.Initiator?.UserId)
                .Select(x => new NotificationRecepient(x.User.UserId, x.User.DisplayName.DisplayName, reason))
                )
            {
                list.TryAdd(r.UserId, r);
            }
        }

        void AddUserInfoHeaderIfNotPresent(IReadOnlyCollection<UserInfoHeader?> subscribe, SubscriptionReason reason)
        {
            AddIfPredicateAndNotAlreadyPresent([.. subscribe.WhereNotNull().Select(player => new UserSubscribe(player))], reason);
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
