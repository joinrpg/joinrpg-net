using System.Text.Json;
using JoinRpg.DataModel.Projects;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using JoinRpg.Web.AdminTools.KogdaIgra;

namespace JoinRpg.WebPortal.Managers.AdminTools;

internal static class ViewModelBuilders
{
    public static SyncStatusViewModel ToViewModel(this SyncStatus status) => new(status.CountOfGames, status.LastUpdated, status.PendingGamesCount);

    public static KogdaIgraCardViewModel ToViewModel(this KogdaIgraGame game)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(game.JsonGameData);

        return new KogdaIgraCardViewModel(
            KogdaIgraUri: new Uri($"https://kogda-igra.ru/game/{game.KogdaIgraGameId}/"),
            Name: game.Name,
            Begin: DateTimeOffset.UtcNow,
            End: DateTimeOffset.UtcNow,
            RegionName: dict["sub_region_disp_name"],
            MasterGroupName: dict["mg"],
            SiteUri: new Uri(dict["uri"])
            );
    }
}
