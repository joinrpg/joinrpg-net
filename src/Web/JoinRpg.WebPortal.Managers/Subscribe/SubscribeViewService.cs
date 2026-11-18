using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Subscribe;
using JoinRpg.Web.Models.Subscribe;
using JoinRpg.Web.ProjectMasterTools.Subscribe;

namespace JoinRpg.WebPortal.Managers.Subscribe;

internal class SubscribeViewService(IUriService uriService,
    IUserSubscribeRepository userSubscribeRepository,
    IUserRepository userRepository,
    IFinanceReportRepository financeReportRepository,
    ICurrentUserAccessor currentUserAccessor,
    IGameSubscribeService gameSubscribeService,
    IClaimsRepository claimsRepository,
    IProjectMetadataRepository projectMetadataRepository
        ) : IGameSubscribeClient
{
    public async Task<ClaimSubscribeViewModel> GetSubscribeForClaim(ClaimIdentification claimId)
    {
        var currentUser = await userRepository.GetWithSubscribe(currentUserAccessor.UserId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);
        var claim = await claimsRepository.GetClaim(claimId);
        var parents = claim!.Character.GetParentGroupsToTop(projectInfo).ToList();
        var isDirect = currentUser.Subscriptions.Any(s => s.ClaimId == claimId.ClaimId);
        if (isDirect)
        {
            return new ClaimSubscribeViewModel() { IsDirect = true, ParentSubscribe = SubscriptionOptions.CreateNoneSet(), SubscribeReason = [] };
        }

        var groupSubscriptions = parents
            .SelectMany(par => currentUser.Subscriptions
                .Where(s => s.CharacterGroupId == par.Id.Id)
                .Select(s => (Group: par, Options: ToSubscriptionOptions(s))))
            .ToList();
        return CalculateParentSubscriptions(groupSubscriptions);
    }

    private static SubscriptionOptions ToSubscriptionOptions(UserSubscription s) => new()
    {
        ClaimStatusChange = s.ClaimStatusChange,
        Comments = s.Comments,
        FieldChange = s.FieldChange,
        MoneyOperation = s.MoneyOperation,
        AccommodationChange = s.AccommodationChange,
        AccommodationInvitesChange = s.AccommodationChange,
    };

    public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
    {
        var data = await userSubscribeRepository.LoadSubscriptionsForProject(new UserIdentification(masterId), new ProjectIdentification(projectId));
        var currentUser = await userRepository.GetById(currentUserAccessor.UserId);

        var paymentTypes = await financeReportRepository.GetPaymentTypesForMaster(projectId, masterId);

        return data.ToSubscribeListViewModel(currentUser, uriService, projectId, paymentTypes);
    }

    public async Task RemoveSubscription(int projectId, int userSubscriptionsId)
        => await gameSubscribeService.RemoveSubscribe(new RemoveSubscribeRequest(projectId, userSubscriptionsId));

    public async Task<ClaimSubscribeViewModel> SubscribeClaimToUser(ClaimIdentification claimId)
    {
        await gameSubscribeService.SubscribeClaimToUser(claimId);
        return await GetSubscribeForClaim(claimId);
    }

    public async Task<ClaimSubscribeViewModel> UnsubscribeClaimToUser(ClaimIdentification claimId)
    {
        await gameSubscribeService.UnsubscribeClaimToUser(claimId);
        return await GetSubscribeForClaim(claimId);
    }

    public async Task SaveGroupSubscription(int projectId, EditSubscribeViewModel model)
    {
        await gameSubscribeService.UpdateSubscribeForGroup(new SubscribeForGroupRequest()
        {
            CharacterGroupId = model.GroupId,
            SubscriptionOptions = model.Options.ToOptions(),
            MasterId = model.MasterId,
        });
    }

    private static ClaimSubscribeViewModel CalculateParentSubscriptions(
        IEnumerable<(CharacterGroupInfo Group, SubscriptionOptions Options)> groupSubscriptions)
    {
        var options = SubscriptionOptions.CreateNoneSet();
        var subscribeReason = new Dictionary<string, string>();

        foreach (var (group, subscr) in groupSubscriptions)
        {
            var newFlags = subscr.Except(options);
            if (newFlags.ClaimStatusChange)
            {
                subscribeReason[nameof(SubscriptionOptions.ClaimStatusChange)] = group.Name;
            }

            if (newFlags.Comments)
            {
                subscribeReason[nameof(SubscriptionOptions.Comments)] = group.Name;
            }

            if (newFlags.FieldChange)
            {
                subscribeReason[nameof(SubscriptionOptions.FieldChange)] = group.Name;
            }

            if (newFlags.MoneyOperation)
            {
                subscribeReason[nameof(SubscriptionOptions.MoneyOperation)] = group.Name;
            }

            if (newFlags.AccommodationChange || newFlags.AccommodationInvitesChange)
            {
                subscribeReason[nameof(SubscriptionOptions.AccommodationChange)] = group.Name;
            }

            options = options.Union(subscr);

            if (options.AllSet)
            {
                break;
            }
        }

        return new ClaimSubscribeViewModel
        {
            IsDirect = false,
            ParentSubscribe = options,
            SubscribeReason = subscribeReason,
        };
    }
}
