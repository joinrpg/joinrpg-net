using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace JoinRpg.Web.Helpers;

[UsedImplicitly]
internal class UriServiceImpl : IUriService, IUriLocator<UserLinkViewModel>
{
    private readonly Lazy<IUrlHelper> urlHelper;

    private string? CreateLink(LinkType linkType, string identification, int? projectId)
    {
        var urlHelper = this.urlHelper.Value;
        var urlScheme = urlHelper.ActionContext.HttpContext.Request.Scheme ?? "http";
        switch (linkType)
        {
            case LinkType.ResultUser:

                return urlHelper.Action("Details",
                    "User",
                    new { UserId = identification },
                    urlScheme);
            case LinkType.ResultCharacterGroup:
                return urlHelper.Action("Details",
                    "GameGroups",
                    new { CharacterGroupId = identification, ProjectId = projectId },
                    urlScheme);
            case LinkType.ResultCharacter:
                return urlHelper.Action("Details",
                    "Character",
                    new { CharacterId = identification, ProjectId = projectId },
                    urlScheme);
            case LinkType.Claim:
                return urlHelper.Action("Edit",
                    "Claim",
                    new { ProjectId = projectId, ClaimId = identification },
                    urlScheme);
            case LinkType.Plot:
                return urlHelper.Action("Edit",
                    "Plot",
                    new { PlotFolderId = identification, ProjectId = projectId },
                    urlScheme);
            case LinkType.Comment:
                return urlHelper.Action("ToComment",
                    "DiscussionRedirect",
                    new { ProjectId = projectId, CommentId = identification },
                    urlScheme);
            case LinkType.CommentDiscussion:
                return urlHelper.Action("ToDiscussion",
                    "DiscussionRedirect",
                    new { ProjectId = projectId, CommentDiscussionId = identification },
                    urlScheme);
            case LinkType.Project:
                return urlHelper.Action("Details", "Game", new { ProjectId = projectId }, urlScheme);
            case LinkType.PaymentSuccess:
                return urlHelper.Action(
                    "ClaimPaymentSuccess",
                    "Payments",
                    new { projectId = projectId, claimId = identification },
                    urlScheme);
            case LinkType.PaymentFail:
                return urlHelper.Action(
                    "ClaimPaymentFail",
                    "Payments",
                    new { projectId = projectId, claimId = identification },
                    urlScheme);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public UriServiceImpl(IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor)
        => urlHelper = new Lazy<IUrlHelper>(() => urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext));

    public string Get(ILinkable link) => GetUri(link).ToString();

    public Uri GetUri(ILinkable link)
    {
        if (link == null)
        {
            throw new ArgumentNullException(nameof(link));
        }

        return new Uri(CreateLink(link.LinkType, link.Identification, link.ProjectId));
    }
    Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target) =>
        new(CreateLink(LinkType.ResultUser, target.UserId.ToString(), projectId: null)!);
}
