using System.Data;
using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel.Projects;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Integrations.KogdaIgra;
internal class KogdaIgraSyncService(IUnitOfWork unitOfWork, IKogdaIgraApiClient apiClient, ILogger<KogdaIgraSyncService> logger) : IKogdaIgraSyncService
{
    public async Task<SyncStatus> GetSyncStatus()
    {
        var count = await unitOfWork.GetDbSet<KogdaIgraGame>().CountAsync();
        var lastUpdated = await unitOfWork.GetDbSet<KogdaIgraGame>().MaxAsync(kig => (DateTimeOffset?)kig.LastUpdatedAt);
        return new(count, lastUpdated ?? DateTimeOffset.UnixEpoch);
    }
    public async Task PerformSync()
    {
        var status = await GetSyncStatus();
        logger.LogInformation("Sync status is {syncStatus}", status);
        var updated = await apiClient.GetChangedGamesSince(status.LastUpdated);
        logger.LogInformation("Received from kogda-igra {kogdaIgraCount} records updated since {lastUpdated}", updated.Length, status.LastUpdated);
        await MarkUpdateRequested(updated);

        var elementsToUpdate = await unitOfWork.GetDbSet<KogdaIgraGame>()
            .Where(e => e.LastUpdatedAt == null || e.LastUpdatedAt < e.UpdateRequestedAt)
            .OrderByDescending(e => e.UpdateRequestedAt)
            .Take(100)
            .ToListAsync();
        foreach (var item in elementsToUpdate)
        {
            var kiData = await apiClient.GetGameInfo(item.KogdaIgraGameId);
            logger.LogInformation("Received game record from kogda-igra {kogdaIgraGameRecord}", kiData);

            await UpsertItem(kiData);
        }
    }

    private async Task MarkUpdateRequested(KogdaIgraGameUpdateMarker[] updated)
    {
        foreach (var item in updated)
        {
            var dbRecord = await unitOfWork.GetDbSet<KogdaIgraGame>().FindAsync(item.Id);
            if (dbRecord == null)
            {
                dbRecord = new()
                {
                    KogdaIgraGameId = item.Id,
                    Name = "Загружается",
                    LastUpdatedAt = null,
                };
                logger.LogInformation("New record from kogda-igra {kogdaIgraId}", item.Id);
                dbRecord = unitOfWork.GetDbSet<KogdaIgraGame>().Add(dbRecord);
            }
            if (dbRecord.UpdateRequestedAt < item.UpdateDate)
            {
                dbRecord.UpdateRequestedAt = item.UpdateDate;
            }
        }
        await unitOfWork.SaveChangesAsync();
    }

    private async Task UpsertItem(KogdaIgraGameInfo kiData)
    {
        var dbRecord = await unitOfWork.GetDbSet<KogdaIgraGame>().FindAsync(kiData.Id);
        logger.LogInformation("Updated information from kogda-igra. Was {dbRecord}, will be {kiData}", dbRecord, kiData);
        dbRecord.Name = kiData.Name;
        dbRecord.JsonGameData = kiData.GameData;
        dbRecord.LastUpdatedAt = kiData.UpdateDate;
        dbRecord.UpdateRequestedAt = kiData.UpdateDate;

        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Saved kogda-igra data for id={kogdaIgraId}", dbRecord.KogdaIgraGameId);
    }
}
