using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebComponents;

namespace JoinRpg.Blazor.Client;

public static class UriLocatorExtensions
{
    private class UriLocator :
        IUriLocator<UserLinkViewModel>, IUriLocator<CharacterGroupLinkSlimViewModel>, IUriLocator<CharacterLinkSlimViewModel>,
        IUriLocator<ProjectLinkViewModel>
    {
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
        Uri IUriLocator<CharacterLinkSlimViewModel>.GetUri(CharacterLinkSlimViewModel target) => throw new NotImplementedException();

        //end of TODO

        Uri IUriLocator<ProjectLinkViewModel>.GetUri(ProjectLinkViewModel target) => new($"/{target.ProjectId.Value}/home", UriKind.Relative);



    }
    public static IServiceCollection AddUriLocator(this IServiceCollection serviceCollection)
    {
        var locator = new UriLocator();
        serviceCollection.AddSingleton<IUriLocator<UserLinkViewModel>>(locator);
        serviceCollection.AddSingleton<IUriLocator<ProjectLinkViewModel>>(locator);
        serviceCollection.AddSingleton<IUriLocator<CharacterGroupLinkSlimViewModel>>(locator);
        serviceCollection.AddSingleton<IUriLocator<CharacterLinkSlimViewModel>>(locator);
        return serviceCollection;
    }
}
