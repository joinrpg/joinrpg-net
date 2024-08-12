using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;
using Microsoft.Extensions.Options;

namespace JoinRpg.Web.Helpers;

internal class UriServiceImpl(
    IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator,
    IOptions<NotificationsOptions> notificationOptions) : IUriService, IUriLocator<UserLinkViewModel>, IUriLocator<CharacterGroupLinkSlimViewModel>
{
    private Uri? CreateLink(LinkType linkType, string identification, int? projectId)
    {
        var link = linkType switch
        {
            LinkType.ResultUser => linkGenerator.GetPathByAction("Details",
                                "User",
                                new { UserId = identification }),
            LinkType.ResultCharacterGroup => linkGenerator.GetPathByAction("Details",
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
            LinkType.PaymentSuccess => linkGenerator.GetPathByAction(
                                "ClaimPaymentSuccess",
                                "Payments",
                                new { projectId = projectId, claimId = identification }),
            LinkType.PaymentFail => linkGenerator.GetPathByAction(
                                "ClaimPaymentFail",
                                "Payments",
                                new { projectId = projectId, claimId = identification }),
            _ => throw new ArgumentOutOfRangeException(nameof(linkType)),
        };
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

        return link is null ? null : new Uri(baseDomain, link);
    }

    public string Get(ILinkable link) => GetUri(link).AbsoluteUri;

    public Uri GetUri(ILinkable link)
    {
        ArgumentNullException.ThrowIfNull(link);

        return CreateLink(link.LinkType, link.Identification, link.ProjectId) ?? throw new InvalidOperationException($"Failed to create link to {link}");
    }
    Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target) =>
        CreateLink(LinkType.ResultUser, target.UserId.ToString(), projectId: null) ?? throw new InvalidOperationException($"Failed to create link {target}");
    Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target) =>
         CreateLink(LinkType.ResultCharacterGroup, target.CharacterGroupId.ToString(), projectId: target.ProjectId.Value) ?? throw new InvalidOperationException($"Failed to create link {target}");
}
