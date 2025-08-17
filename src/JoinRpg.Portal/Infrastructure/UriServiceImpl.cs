using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
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
    IUriLocator<ProjectLinkViewModel>
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

        return new Uri(baseDomain, link);
    }

    public string Get(ILinkable link) => GetUri(link).AbsoluteUri;

    Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target) =>
        GetUri(new Linkable(LinkType.ResultUser, ProjectId: null, Identification: target.UserId.ToString()));
    Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target) =>
         GetUri(new Linkable(target.CharacterGroupId));
    Uri IUriLocator<CharacterLinkSlimViewModel>.GetUri(CharacterLinkSlimViewModel target)
        => GetUri(new Linkable(target.CharacterId));
    Uri IUriLocator<ProjectLinkViewModel>.GetUri(ProjectLinkViewModel target) => GetUri(new Linkable(target.ProjectId));

    private record Linkable(LinkType LinkType, int? ProjectId, string? Identification) : ILinkable
    {
        public Linkable(LinkType linkType, IProjectEntityId projectEntityId) : this(linkType, projectEntityId.ProjectId, projectEntityId.Id.ToString())
        {

        }

        public Linkable(CharacterIdentification id) : this(LinkType.ResultCharacter, id) { }

        public Linkable(ProjectIdentification id) : this(LinkType.Project, id.Value, Identification: null) { }

        public Linkable(CharacterGroupIdentification id) : this(LinkType.CharacterGroupRoles, id) { }
    }
}
