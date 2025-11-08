using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using JoinRpg.Web.AdminTools.KogdaIgra;
using JoinRpg.Web.Games.Projects;

namespace JoinRpg.WebPortal.Managers.AdminTools;

internal static class ViewModelBuilders
{
    public static SyncStatusViewModel ToViewModel(this SyncStatus status) => new(status.CountOfGames, status.LastUpdated, status.PendingGamesCount);

    public static KogdaIgraCardViewModel ToViewModel(this KogdaIgraGameInfo game, KogdaIgraOptions options)
    {
        return new KogdaIgraCardViewModel(
            new(game.Id),
            KogdaIgraUri: new Uri(options.HostName, $"/game/{game.Id}/"),
            Name: game.Name,
            Begin: game.Begin,
            End: game.End,
            RegionName: game.RegionName,
            MasterGroupName: game.MasterGroupName,
            SiteUri: string.IsNullOrWhiteSpace(game.SiteUri) ? null : new Uri(game.SiteUri)
            );
    }
}
