using System;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Helpers
{
    [UsedImplicitly]
    internal class UriServiceImpl : IUriService
    {
        private readonly HttpContextBase _current;

        private string GetRouteTarget([NotNull] ILinkable link, UrlHelper urlHelper)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));
            switch (link.LinkType)
            {
                case LinkType.ResultUser:
                    return urlHelper.Action("Details", "User", new {UserId = link.Identification});
                case LinkType.ResultCharacterGroup:
                    return urlHelper.Action("Index",
                        "GameGroups",
                        new {CharacterGroupId = link.Identification, link.ProjectId});
                case LinkType.ResultCharacter:
                    return urlHelper.Action("Details",
                        "Character",
                        new {CharacterId = link.Identification, link.ProjectId});
                case LinkType.Claim:
                    return urlHelper.Action("Edit",
                        "Claim",
                        new {link.ProjectId, ClaimId = link.Identification});
                case LinkType.Plot:
                    return urlHelper.Action("Edit",
                        "Plot",
                        new {PlotFolderId = link.Identification, link.ProjectId});
                case LinkType.Comment:
                    return urlHelper.Action("RedirectToDiscussion",
                        "Forum",
                        new {link.ProjectId, CommentId = link.Identification});
                case LinkType.CommentDiscussion:
                    return urlHelper.Action("RedirectToDiscussion",
                        "Forum",
                        new {link.ProjectId, CommentDiscussionId = link.Identification});
                case LinkType.Project:
                    return urlHelper.Action("Details", "Game", new {link.ProjectId});
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public UriServiceImpl(HttpContextBase current)
        {
            _current = current;
        }

        public string Get(ILinkable link) => GetUri(link).ToString();

        public Uri GetUri(ILinkable link)
        {
            var urlHelper = new UrlHelper(_current.Request.RequestContext);
            return new Uri(GetRouteTarget(link, urlHelper));
        }
    }
}
