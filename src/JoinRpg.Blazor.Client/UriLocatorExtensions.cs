using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Blazor.Client;

public static class UriLocatorExtensions
{
    private class UriLocator :
        IUriLocator<UserLinkViewModel>, IUriLocator<CharacterGroupLinkSlimViewModel>, IUriLocator<CharacterLinkSlimViewModel>,
        IUriLocator<ProjectIdentification>, IUriLocator<ClaimIdentification>, IUriLocator<CharacterIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new Uri($"/{target.ProjectId.Value}/claim/{target.ClaimId}/edit", UriKind.Relative);

        public Uri GetUri(CharacterIdentification target) => new Uri($"/{target.ProjectId.Value}/character/{target.CharacterId}/details", UriKind.Relative);

        Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target)
        {
            if (target.ViewMode == ViewMode.Hide)
            {
                throw new InvalidOperationException("Should not have url of hidden");
            }
            return new($"/user/{target.UserId}", UriKind.Relative);
        }

        // TODO implement for Blazor. Added so we will have nice exception instead of bla-bla not resolved.
        Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target)
            => throw new NotImplementedException();
        Uri IUriLocator<CharacterLinkSlimViewModel>.GetUri(CharacterLinkSlimViewModel target) => GetUri(target.CharacterId);

        //end of TODO

        Uri IUriLocator<ProjectIdentification>.GetUri(ProjectIdentification target) => new($"/{target.Value}/home", UriKind.Relative);



    }
    public static IServiceCollection AddUriLocator(this IServiceCollection serviceCollection)
    {
        var locator = new UriLocator();
        return serviceCollection
            .AddSingleton<IUriLocator<UserLinkViewModel>>(locator)
            .AddSingleton<IUriLocator<ProjectIdentification>>(locator)
            .AddSingleton<IUriLocator<CharacterGroupLinkSlimViewModel>>(locator)
            .AddSingleton<IUriLocator<ClaimIdentification>>(locator)
            .AddSingleton<IUriLocator<CharacterLinkSlimViewModel>>(locator)
            ;
    }
}
