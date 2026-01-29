using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using JoinRpg.Common.KogdaIgraClient;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel.Projects;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Integrations.KogdaIgra;

internal class KogdaIgraSyncService(
    IUnitOfWork unitOfWork,
    IKogdaIgraApiClient apiClient,
    ILogger<KogdaIgraSyncService> logger,
    ICurrentUserAccessor currentUserAccessor)
    : IKogdaIgraSyncService, IKogdaIgraBindService, IKogdaIgraInfoService
{
    public async Task<SyncStatus> GetSyncStatus()
    {
        if (!currentUserAccessor.IsAdmin)
        {
            throw new MustBeAdminException();
        }
        var count = await unitOfWork.GetDbSet<KogdaIgraGame>().CountAsync();
        var lastUpdated = await unitOfWork.GetDbSet<KogdaIgraGame>().MaxAsync(kig => (DateTimeOffset?)kig.UpdateRequestedAt);
        var pending = await unitOfWork.GetKogdaIgraRepository().GetNotUpdatedCount();
        return new(count, lastUpdated ?? DateTimeOffset.UnixEpoch, pending);
    }
    public async Task<SyncStatus> PerformSync()
    {
        if (!currentUserAccessor.IsAdmin)
        {
            throw new MustBeAdminException();
        }
        var status = await GetSyncStatus();
        logger.LogInformation("Sync status is {syncStatus}", status);
        var updated = await apiClient.GetChangedGamesSince(status.LastUpdated);
        logger.LogInformation("Received from kogda-igra {kogdaIgraCount} records updated since {lastUpdated}", updated.Length, status.LastUpdated);
        await MarkUpdateRequested(updated);

        var elementsToUpdate = await unitOfWork.GetKogdaIgraRepository().GetNotUpdatedObjects();
        foreach (var item in elementsToUpdate)
        {
            try
            {
                var kiData = await apiClient.GetGameInfo(item.KogdaIgraGameId);
                logger.LogInformation("Received game record from kogda-igra {kogdaIgraGameRecord}", kiData);

                if (kiData is null)
                {
                    await DeleteItem(item.KogdaIgraGameId);
                }
                else
                {
                    await UpsertItem(kiData);
                }
            }
            catch (KogdaIgraParseException e)
            {
                logger.LogError(e, "Ошибка при разборе ответа КогдаИгры, игра {kogdaIgraGameId}", item.KogdaIgraGameId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Неожиданная ошибка при обновлении с КогдаИгры, игра {kogdaIgraGameId}", item.KogdaIgraGameId);
            }
        }

        status = await GetSyncStatus();
        logger.LogInformation("Sync status is {syncStatus}", status);
        return status;
    }

    [return: NotNullIfNotNull(nameof(info))]
    private static KogdaIgraGameData? ToDomain(KogdaIgraGameInfo? info)
    {
        if (info is null)
        {
            return null;
        }
        return new KogdaIgraGameData(info.Id, info.Name, info.UpdateDate, info.Begin, info.End, info.RegionName, info.MasterGroupName, info.SiteUri);
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
                    Name = "Загружается " + item.Id,
                    LastUpdatedAt = null,
                    Active = true,
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
        dbRecord.Active = true;

        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Saved kogda-igra data for id={kogdaIgraId}", dbRecord.KogdaIgraGameId);
    }

    private async Task DeleteItem(int kogdaIgraGameId)
    {
        var dbRecord = await unitOfWork.GetDbSet<KogdaIgraGame>().FindAsync(kogdaIgraGameId);
        logger.LogInformation("Deleting record from kogda-igra {kogdaIgraId}. Was {dbRecord}, will be deleted.", kogdaIgraGameId, dbRecord);
        dbRecord.Name = "Удалено " + kogdaIgraGameId;
        dbRecord.JsonGameData = "{}";
        dbRecord.LastUpdatedAt = dbRecord.UpdateRequestedAt; // Когда игра не возвращает дату для удаления
        dbRecord.Active = false;

        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Saved kogda-igra data for id={kogdaIgraId}", dbRecord.KogdaIgraGameId);
    }

    public async Task UpdateKogdaIgraBindings(ProjectIdentification projectId, KogdaIgraIdentification[] kogdaIgraIdentifications, bool DisableKogdaIgraMapping)
    {
        if (!currentUserAccessor.IsAdmin)
        {
            throw new MustBeAdminException();
        }
        if (DisableKogdaIgraMapping)
        {
            kogdaIgraIdentifications = [];
        }
        var project = await unitOfWork.GetProjectRepository().GetProjectAsync(projectId);
        var games = await unitOfWork.GetKogdaIgraRepository().GetByIds(kogdaIgraIdentifications);
        project.KogdaIgraGames.AssignLinksList(games);
        project.Details.DisableKogdaIgraMapping = DisableKogdaIgraMapping;
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<KogdaIgraGameData[]> GetGames(IReadOnlyCollection<KogdaIgraIdentification> ids)
    {
        var games = await unitOfWork.GetKogdaIgraRepository().GetByIds(ids);
        return [.. games.Select(g => ToDomain(ResultParser.TryParseGameInfo(g.JsonGameData)!))];
    }
}
