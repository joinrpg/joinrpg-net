using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace JoinRpg.Web.Helpers
{
    [UsedImplicitly]
    internal class UriServiceImpl : IUriService
    {
        private readonly Lazy<IUrlHelper> urlHelper;

        private string GetRouteTarget([NotNull]
            ILinkable link, IUrlHelper urlHelper)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            var urlScheme = urlHelper.ActionContext.HttpContext.Request.Scheme ?? "http";
            switch (link.LinkType)
            {
                case LinkType.ResultUser:

                    return urlHelper.Action("Details",
                        "User",
                        new { UserId = link.Identification },
                        urlScheme);
                case LinkType.ResultCharacterGroup:
                    return urlHelper.Action("Details",
                        "GameGroups",
                        new { CharacterGroupId = link.Identification, link.ProjectId },
                        urlScheme);
                case LinkType.ResultCharacter:
                    return urlHelper.Action("Details",
                        "Character",
                        new { CharacterId = link.Identification, link.ProjectId },
                        urlScheme);
                case LinkType.Claim:
                    return urlHelper.Action("Edit",
                        "Claim",
                        new { link.ProjectId, ClaimId = link.Identification },
                        urlScheme);
                case LinkType.Plot:
                    return urlHelper.Action("Edit",
                        "Plot",
                        new { PlotFolderId = link.Identification, link.ProjectId },
                        urlScheme);
                case LinkType.Comment:
                    return urlHelper.Action("ToComment",
                        "DiscussionRedirect",
                        new { link.ProjectId, CommentId = link.Identification },
                        urlScheme);
                case LinkType.CommentDiscussion:
                    return urlHelper.Action("ToDiscussion",
                        "DiscussionRedirect",
                        new { link.ProjectId, CommentDiscussionId = link.Identification },
                        urlScheme);
                case LinkType.Project:
                    return urlHelper.Action("Details", "Game", new { link.ProjectId }, urlScheme);
                case LinkType.PaymentSuccess:
                    return urlHelper.Action(
                        "ClaimPaymentSuccess",
                        "Payments",
                        new { projectId = link.ProjectId, claimId = link.Identification },
                        urlScheme);
                case LinkType.PaymentFail:
                    return urlHelper.Action(
                        "ClaimPaymentFail",
                        "Payments",
                        new { projectId = link.ProjectId, claimId = link.Identification },
                        urlScheme);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public UriServiceImpl(IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor)
            => urlHelper = new Lazy<IUrlHelper>(() => urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext));

        public string Get(ILinkable link) => GetUri(link).ToString();

        public Uri GetUri(ILinkable link) => new(GetRouteTarget(link, urlHelper.Value));
    }
}
