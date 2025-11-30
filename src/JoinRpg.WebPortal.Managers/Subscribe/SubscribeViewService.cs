using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.Domain;
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
    IClaimsRepository claimsRepository
        ) : IGameSubscribeClient
{
    public async Task<ClaimSubscribeViewModel> GetSubscribeForClaim(int projectId, int claimId)
    {
        var currentUser = await userRepository.GetWithSubscribe(currentUserAccessor.UserId);

        var claim = await claimsRepository.GetClaim(projectId, claimId);


        return GetFullSubscriptionTooltip(claim.Character.GetParentGroupsToTop(), currentUser.Subscriptions, claimId);
    }

    public async Task<SubscribeListViewModel> GetSubscribeForMaster(int projectId, int masterId)
    {
        var data = await userSubscribeRepository.LoadSubscriptionsForProject(new UserIdentification(masterId), new ProjectIdentification(projectId));
        var currentUser = await userRepository.GetById(currentUserAccessor.UserId);

        var paymentTypes = await financeReportRepository.GetPaymentTypesForMaster(projectId, masterId);

        return data.ToSubscribeListViewModel(currentUser, uriService, projectId, paymentTypes);
    }

    public async Task RemoveSubscription(int projectId, int userSubscriptionsId)
        => await gameSubscribeService.RemoveSubscribe(new RemoveSubscribeRequest(projectId, userSubscriptionsId));

    public async Task SaveGroupSubscription(int projectId, EditSubscribeViewModel model)
    {
        await gameSubscribeService.UpdateSubscribeForGroup(new SubscribeForGroupRequest()
        {
            CharacterGroupId = model.GroupId,
            SubscriptionOptions = model.Options.ToOptions(),
            MasterId = model.MasterId,
        });
    }

    private static ClaimSubscribeViewModel GetFullSubscriptionTooltip(IEnumerable<CharacterGroup> parents,
    IReadOnlyCollection<UserSubscription> subscriptions, int claimId)
    {
        var claimStatusChangeGroup = "";
        var commentsGroup = "";
        var fieldChangeGroup = "";
        var moneyOperationGroup = "";

        var subscrTooltip = new ClaimSubscribeViewModel()
        {
            HasFullParentSubscription = false,
            Tooltip = "",
            IsDirect = false,
            ClaimStatusChange = false,
            Comments = false,
            FieldChange = false,
            MoneyOperation = false,
        };

        subscrTooltip.IsDirect = subscriptions.FirstOrDefault(s => s.ClaimId == claimId) != null;

        foreach (var par in parents)
        {
            foreach (var subscr in subscriptions)
            {
                if (par.CharacterGroupId == subscr.CharacterGroupId &&
                    !(subscrTooltip.ClaimStatusChange && subscrTooltip.Comments &&
                      subscrTooltip.FieldChange && subscrTooltip.MoneyOperation))
                {
                    if (subscrTooltip.ClaimStatusChange && subscrTooltip.Comments &&
                        subscrTooltip.FieldChange && subscrTooltip.MoneyOperation)
                    {
                        break;
                    }
                    if (subscr.ClaimStatusChange && !subscrTooltip.ClaimStatusChange)
                    {
                        subscrTooltip.ClaimStatusChange = true;
                        claimStatusChangeGroup = par.CharacterGroupName;
                    }
                    if (subscr.Comments && !subscrTooltip.Comments)
                    {
                        subscrTooltip.Comments = true;
                        commentsGroup = par.CharacterGroupName;
                    }
                    if (subscr.FieldChange && !subscrTooltip.FieldChange)
                    {
                        subscrTooltip.FieldChange = true;
                        fieldChangeGroup = par.CharacterGroupName;
                    }
                    if (subscr.MoneyOperation && !subscrTooltip.MoneyOperation)
                    {
                        subscrTooltip.MoneyOperation = true;
                        moneyOperationGroup = par.CharacterGroupName;
                    }
                }
            }
        }

        if (subscrTooltip.ClaimStatusChange && subscrTooltip.Comments && subscrTooltip.FieldChange &&
            subscrTooltip.MoneyOperation)
        {
            subscrTooltip.HasFullParentSubscription = true;
        }

        subscrTooltip.Tooltip = GetFullSubscriptionText(subscrTooltip, claimStatusChangeGroup,
          commentsGroup, fieldChangeGroup, moneyOperationGroup);
        return subscrTooltip;
    }

    private static string GetFullSubscriptionText(ClaimSubscribeViewModel subscrTooltip,
      string claimStatusChangeGroup, string commentsGroup, string fieldChangeGroup,
      string moneyOperationGroup)
    {
        // TODO: Это текст должен формироваться на уровне View
        string res;
        if (subscrTooltip.IsDirect || subscrTooltip.HasFullParentSubscription)
        {
            res = "Вы подписаны на эту заявку";
        }
        else if (!(subscrTooltip.ClaimStatusChange || subscrTooltip.Comments ||
                   subscrTooltip.FieldChange || subscrTooltip.MoneyOperation))
        {
            res = "Вы не подписаны на эту заявку";
        }
        else
        {
            res = "Вы не подписаны на эту заявку, но будете получать уведомления в случаях: <br><ul>";

            if (subscrTooltip.ClaimStatusChange)
            {
                res += "<li>Изменение статуса (группа \"" + claimStatusChangeGroup + "\")</li>";
            }
            if (subscrTooltip.Comments)
            {
                res += "<li>Комментарии (группа \"" + commentsGroup + "\")</li>";
            }
            if (subscrTooltip.FieldChange)
            {
                res += "<li>Изменение полей заявки (группа \"" + fieldChangeGroup + "\")</li>";
            }
            if (subscrTooltip.MoneyOperation)
            {
                res += "<li>Финансовые операции (группа \"" + moneyOperationGroup + "\")</li>";
            }

            res += "</ul>";
        }
        return res;
    }
}
