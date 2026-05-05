
using JoinRpg.DataModel.Projects;

namespace JoinRpg.Data.Interfaces.AdminTools;

public record KogdaIgraListItem(KogdaIgraIdentification KogdaIgraId, string Name, int? Year);

public interface IKogdaIgraRepository
{
    Task<KogdaIgraListItem[]> GetActive();
    Task<ICollection<KogdaIgraGame>> GetByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications);

    Task<IReadOnlyCollection<KogdaIgraGameData>> GetDataByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications);
    Task<KogdaIgraListItem[]> GetNotUpdated();
    Task<int> GetNotUpdatedCount();
    Task<KogdaIgraGame[]> GetNotUpdatedObjects();
}
