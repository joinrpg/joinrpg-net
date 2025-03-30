using JoinRpg.DataModel.Projects;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using JoinRpg.Web.AdminTools.KogdaIgra;

namespace JoinRpg.WebPortal.Managers.AdminTools;

internal static class ViewModelBuilders
{
    public static SyncStatusViewModel ToViewModel(this SyncStatus status) => new(status.CountOfGames, status.LastUpdated);

    public static KogdaIgraCardViewModel ToViewModel(this KogdaIgraGame game) =>
        new KogdaIgraCardViewModel(
            KogdaIgraUri: new Uri($"https://kogda-igra.ru/game/{game.KogdaIgraGameId}/"),
            Name: game.Name,
            Begin: DateTimeOffset.UtcNow,
            End: DateTimeOffset.UtcNow,
            RegionName: "region",
            MasterGroupName: "mg",
            SiteUri: new Uri("http://bastilia.ru/")
            );
}
