using JoinRpg.WebComponents;

namespace JoinRpg.Blazor.Client;

public static class UriLocatorExtensions
{
    private class UriLocator : IUriLocator<UserLinkViewModel>
    {
        Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target)
            => new($"/user/{target.UserId}");
    }
    public static IServiceCollection AddUriLocator(this IServiceCollection serviceCollection)
    {
        var locator = new UriLocator();
        serviceCollection.AddSingleton<IUriLocator<UserLinkViewModel>>(locator);
        return serviceCollection;
    }
}
