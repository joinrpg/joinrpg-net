using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;

namespace JoinRpg.Blazor.Client;

public static class UriLocatorExtensions
{
    private class UriLocator :
        IUriLocator<UserLinkViewModel>, IUriLocator<CharacterGroupLinkSlimViewModel>, IUriLocator<CharacterLinkSlimViewModel>,
        IUriLocator<ProjectIdentification>, IUriLocator<ClaimIdentification>, IUriLocator<CharacterIdentification>,
        ICharacterUriLocator, ICharacterGroupUriLocator
    {
        public Uri GetUri(ClaimIdentification target) => new Uri($"/{target.ProjectId.Value}/claim/{target.ClaimId}/edit", UriKind.Relative);

        public Uri GetUri(CharacterIdentification target) => new Uri($"/{target.ProjectId.Value}/character/{target.CharacterId}/details", UriKind.Relative);

        Uri ICharacterUriLocator.GetDetailsUri(CharacterIdentification characterId) => GetUri(characterId);
        Uri ICharacterUriLocator.GetAddClaimUri(CharacterIdentification characterId) => new Uri($"/{characterId.ProjectId.Value}/character/{characterId.CharacterId}/apply", UriKind.Relative);
        Uri ICharacterUriLocator.GetEditUri(CharacterIdentification characterId) => new Uri($"/{characterId.ProjectId.Value}/character/{characterId.CharacterId}/edit", UriKind.Relative);

        Uri IUriLocator<UserLinkViewModel>.GetUri(UserLinkViewModel target)
        {
            if (target.ViewMode == ViewMode.Hide)
            {
                throw new InvalidOperationException("Should not have url of hidden");
            }
            return new($"/user/{target.UserId}", UriKind.Relative);
        }

        Uri IUriLocator<CharacterGroupLinkSlimViewModel>.GetUri(CharacterGroupLinkSlimViewModel target)
            => new($"/{target.CharacterGroupId.ProjectId.Value}/roles/{target.CharacterGroupId.CharacterGroupId}", UriKind.Relative);
        Uri IUriLocator<CharacterLinkSlimViewModel>.GetUri(CharacterLinkSlimViewModel target) => GetUri(target.CharacterId);

        Uri IUriLocator<ProjectIdentification>.GetUri(ProjectIdentification target) => new($"/{target.Value}/home", UriKind.Relative);

        Uri ICharacterGroupUriLocator.GetClaimListUri(CharacterGroupIdentification id) =>
            new($"/{id.ProjectId.Value}/claims/roles/{id.CharacterGroupId}", UriKind.Relative);

        Uri ICharacterGroupUriLocator.GetCharacterListUri(CharacterGroupIdentification id) =>
            new($"/{id.ProjectId.Value}/characters/bygroup/{id.CharacterGroupId}", UriKind.Relative);

        Uri ICharacterGroupUriLocator.GetSubscribeUri(CharacterGroupIdentification id) =>
            new($"/{id.ProjectId.Value}/subscribe/forgroup/{id.CharacterGroupId}", UriKind.Relative);

        Uri ICharacterGroupUriLocator.GetEditUri(CharacterGroupIdentification id) =>
            new($"/{id.ProjectId.Value}/roles/{id.CharacterGroupId}/edit", UriKind.Relative);

        Uri ICharacterGroupUriLocator.GetDeleteUri(CharacterGroupIdentification id) =>
            new($"/{id.ProjectId.Value}/roles/{id.CharacterGroupId}/delete", UriKind.Relative);

        Uri ICharacterGroupUriLocator.GetCreateCharacterUri(CharacterGroupIdentification id) =>
            new($"/{id.ProjectId.Value}/character/create/{id.CharacterGroupId}", UriKind.Relative);

        Uri ICharacterGroupUriLocator.GetAddGroupUri(CharacterGroupIdentification id) =>
            new($"/{id.ProjectId.Value}/roles/{id.CharacterGroupId}/addgroup", UriKind.Relative);
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
            .AddSingleton<ICharacterUriLocator>(locator)
            .AddSingleton<ICharacterGroupUriLocator>(locator)
            ;
    }
}
