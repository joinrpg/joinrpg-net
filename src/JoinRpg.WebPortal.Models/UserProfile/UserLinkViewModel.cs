using System;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Models.UserProfile
{
    public record UserLinkViewModel(
        int UserId,
        string DisplayName)
        : ILinkable
    {
        public UserLinkViewModel(User user) : this(user.UserId, user.GetDisplayName().Trim())
    {

    }

    public Uri GetUri(IUriService uriService) => uriService.GetUri(this);

    LinkType ILinkable.LinkType => LinkType.ResultUser;

    string ILinkable.Identification => UserId.ToString();

    int? ILinkable.ProjectId => null;

    public static UserLinkViewModel? FromOptional(User? user)
        => user is null ? null : new UserLinkViewModel(user);
}
}
