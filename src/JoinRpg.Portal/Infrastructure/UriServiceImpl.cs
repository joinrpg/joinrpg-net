using JoinRpg.Interfaces;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebComponents;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure;

internal class UriServiceImpl(
    IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator,
    IOptions<NotificationsOptions> notificationOptions) : IUriService,
    IUriLocator<UserLinkViewModel>,
    IUriLocator<CharacterGroupLinkSlimViewModel>,
    IUriLocator<CharacterLinkSlimViewModel>,
    IUriLocator<ProjectLinkViewModel>,
    IProjectUriLocator,
    ICharacterUriLocator,
    IUriLocator<PlotFolderIdentification>,
    IUriLocator<ClaimIdentification>,
    INotificationUriLocator<ClaimIdentification>
{
    public Uri GetUri(ILinkable linkable)
    {
        ArgumentNullException.ThrowIfNull(linkable);

        var linkType = linkable.LinkType;
        var projectId = linkable.ProjectId;
        var identification = linkable.Identification;

        var link = linkType switch
        {
            LinkType.ResultUser => linkGenerator.GetPathByAction("Details",
                                "User",
                                new { UserId = identification }),
            LinkType.ResultCharacterGroup => linkGenerator.GetPathByAction("Details",
                                "GameGroups",
                                new { CharacterGroupId = identification, ProjectId = projectId }),
            LinkType.CharacterGroupRoles => linkGenerator.GetPathByAction("Index",
                                "GameGroups",
                                new { CharacterGroupId = identification, ProjectId = projectId }),
            LinkType.ResultCharacter => linkGenerator.GetPathByAction("Details",
                                "Character",
                                new { CharacterId = identification, ProjectId = projectId }),
            LinkType.Claim => linkGenerator.GetPathByAction("Edit",
                                "Claim",
                                new { ProjectId = projectId, ClaimId = identification }),
            LinkType.Plot => linkGenerator.GetPathByAction("Edit",
                                "Plot",
                                new { PlotFolderId = identification, ProjectId = projectId }),
            LinkType.Comment => linkGenerator.GetPathByAction("ToComment",
                                "DiscussionRedirect",
                                new { ProjectId = projectId, CommentId = identification }),
            LinkType.CommentDiscussion => linkGenerator.GetPathByAction("ToDiscussion",
                                "DiscussionRedirect",
                                new { ProjectId = projectId, CommentDiscussionId = identification }),
            LinkType.Project => linkGenerator.GetPathByAction("Details", "Game", new { ProjectId = projectId }),

            LinkType.PaymentSuccess when linkable is ILinkableClaim lc =>
                                linkGenerator.GetPathByAction("ClaimPaymentSuccess", "Payments", new { projectId, claimId = lc.ClaimId }),

            LinkType.PaymentFail when linkable is ILinkableClaim lc
                                => linkGenerator.GetPathByAction("ClaimPaymentFail", "Payments", new { projectId, claimId = lc.ClaimId }),

            LinkType.PaymentUpdate when linkable is ILinkablePayment lp
                                => linkGenerator.GetPathByAction("UpdateClaimPayment", "Payments", new { projectId = lp.ProjectId, claimId = lp.ClaimId, orderId = lp.OperationId }),
            _ => throw new ArgumentOutOfRangeException(nameof(linkType)),
        };

        if (link is null)
        {
            throw new InvalidOperationException($"Failed to create link to {linkable}");
        }
        Uri baseDomain = GetBaseDomain();

        return new Uri(baseDomain, link);
    }

    private Uri GetBaseDomain()
    {
        Uri baseDomain;
        if (httpContextAccessor.HttpContext?.Request is HttpRequest request)
        {
            // внутри веб реквеста, берем схему и хост из него
            baseDomain = new Uri($"{request.Scheme}://{request.Host}");
        }
        else
        {
            // Берем из настроек
            baseDomain = notificationOptions.Value.BaseDomain;
        }

        return baseDomain;
    }

    public string Get(ILinkable link) => GetUri(link).AbsoluteUri;

    Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target) =>
        GetUri(new Linkable(LinkType.ResultUser, ProjectId: null, Identification: target.UserId.ToString()));
    Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target) =>
         GetUri(new Linkable(target.CharacterGroupId));
    Uri IUriLocator<CharacterLinkSlimViewModel>.GetUri(CharacterLinkSlimViewModel target)
        => GetUri(new Linkable(target.CharacterId));
    Uri IUriLocator<ProjectLinkViewModel>.GetUri(ProjectLinkViewModel target) => GetUri(new Linkable(target.ProjectId));
    Uri IProjectUriLocator.GetMyClaimUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("MyClaim", "Claim", new { ProjectId = projectId.Value }));
    Uri IProjectUriLocator.GetAddClaimUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("AddForGroup", "Claim", new { ProjectId = projectId.Value }));
    public Uri GetDetailsUri(CharacterIdentification characterId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Details", "Character", new { CharacterId = characterId.CharacterId, ProjectId = characterId.ProjectId.Value }));
    public Uri GetAddClaimUri(CharacterIdentification characterId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("AddForCharacter", "Claim", new { CharacterId = characterId.CharacterId, ProjectId = characterId.ProjectId.Value }));
    Uri IProjectUriLocator.GetCreatePlotUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Create", "Plot", new { ProjectId = projectId.Value }));
    public Uri GetUri(PlotFolderIdentification target) => GetUri(new Linkable(target));
    Uri IProjectUriLocator.GetRolesListUri(ProjectIdentification projectId) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Index", "GameGroups", new { ProjectId = projectId.Value }));
    public Uri GetUri(ClaimIdentification target) => new Uri(GetBaseDomain(), linkGenerator.GetPathByAction("Edit", "Claim", new { ProjectId = target.ProjectId.Value, target.ClaimId }));
    Uri IProjectUriLocator.GetCaptainCabinetUri(ProjectIdentification projectId) => new(GetBaseDomain(), linkGenerator.GetPathByPage("/GamePages/CaptainCabinet", values: new { ProjectId = projectId.Value }));

    private record Linkable(LinkType LinkType, int? ProjectId, string? Identification) : ILinkable
    {
        public Linkable(LinkType linkType, IProjectEntityId projectEntityId) : this(linkType, projectEntityId.ProjectId, projectEntityId.Id.ToString())
        {

        }

        public Linkable(CharacterIdentification id) : this(LinkType.ResultCharacter, id) { }

        public Linkable(ProjectIdentification id) : this(LinkType.Project, id.Value, Identification: null) { }

        public Linkable(CharacterGroupIdentification id) : this(LinkType.CharacterGroupRoles, id) { }

        public Linkable(PlotFolderIdentification id) : this(LinkType.Plot, id) { }
    }
}
