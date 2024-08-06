using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace JoinRpg.Web.Helpers;

internal class UriServiceImpl(IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor) : IUriService, IUriLocator<UserLinkViewModel>, IUriLocator<CharacterGroupLinkSlimViewModel>
{
    private readonly Lazy<IUrlHelper> urlHelper = CreateUrlHelper(urlHelperFactory, actionContextAccessor);

    private string? CreateLink(LinkType linkType, string identification, int? projectId)
    {
        var urlHelper = this.urlHelper.Value;
        var urlScheme = urlHelper.ActionContext.HttpContext.Request.Scheme ?? "http";
        return linkType switch
        {
            LinkType.ResultUser => urlHelper.Action("Details",
                                "User",
                                new { UserId = identification },
                                urlScheme),
            LinkType.ResultCharacterGroup => urlHelper.Action("Details",
                                "GameGroups",
                                new { CharacterGroupId = identification, ProjectId = projectId },
                                urlScheme),
            LinkType.ResultCharacter => urlHelper.Action("Details",
                                "Character",
                                new { CharacterId = identification, ProjectId = projectId },
                                urlScheme),
            LinkType.Claim => urlHelper.Action("Edit",
                                "Claim",
                                new { ProjectId = projectId, ClaimId = identification },
                                urlScheme),
            LinkType.Plot => urlHelper.Action("Edit",
                                "Plot",
                                new { PlotFolderId = identification, ProjectId = projectId },
                                urlScheme),
            LinkType.Comment => urlHelper.Action("ToComment",
                                "DiscussionRedirect",
                                new { ProjectId = projectId, CommentId = identification },
                                urlScheme),
            LinkType.CommentDiscussion => urlHelper.Action("ToDiscussion",
                                "DiscussionRedirect",
                                new { ProjectId = projectId, CommentDiscussionId = identification },
                                urlScheme),
            LinkType.Project => urlHelper.Action("Details", "Game", new { ProjectId = projectId }, urlScheme),
            LinkType.PaymentSuccess => urlHelper.Action(
                                "ClaimPaymentSuccess",
                                "Payments",
                                new { projectId = projectId, claimId = identification },
                                urlScheme),
            LinkType.PaymentFail => urlHelper.Action(
                                "ClaimPaymentFail",
                                "Payments",
                                new { projectId = projectId, claimId = identification },
                                urlScheme),
            _ => throw new ArgumentOutOfRangeException(nameof(linkType)),
        };
    }

    private static Lazy<IUrlHelper> CreateUrlHelper(IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor) => new Lazy<IUrlHelper>(() => urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext ?? throw new InvalidOperationException("Action context is null. Possible reason: trying to construct UriService outside of web request")));

    public string Get(ILinkable link) => GetUri(link).ToString();

    public Uri GetUri(ILinkable link)
    {
        ArgumentNullException.ThrowIfNull(link);

        return new Uri(CreateLink(link.LinkType, link.Identification, link.ProjectId) ?? throw new InvalidOperationException($"Failed to create link to {link}"));
    }
    Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target) =>
        new(CreateLink(LinkType.ResultUser, target.UserId.ToString(), projectId: null)!);
    Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target) =>
         new(CreateLink(LinkType.ResultCharacterGroup, target.CharacterGroupId.ToString(), projectId: target.ProjectId.Value)!);
}
