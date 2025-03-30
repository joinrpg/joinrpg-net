using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Blazor.Client;

public static class UriLocatorExtensions
{
    private class UriLocator : IUriLocator<UserLinkViewModel>, IUriLocator<CharacterGroupLinkSlimViewModel>, IUriLocator<CharacterLinkSlimViewModel>
    {
        Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target)
        {
            if (target.ViewMode == ViewMode.Hide)
            {
                throw new InvalidOperationException("Should not have url of hidden");
            }
            return new($"/user/{target.UserId}");
        }

        // TODO implement for Blazor. Added so we will have nice exception instead of bla-bla not resolved.
        Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target)
            => throw new NotImplementedException();
        Uri IUriLocator<CharacterLinkSlimViewModel>.GetUri(CharacterLinkSlimViewModel target) => throw new NotImplementedException();

        //end of TODO

    }
    public static IServiceCollection AddUriLocator(this IServiceCollection serviceCollection)
    {
        var locator = new UriLocator();
        serviceCollection.AddSingleton<IUriLocator<UserLinkViewModel>>(locator);
        return serviceCollection;
    }
}
