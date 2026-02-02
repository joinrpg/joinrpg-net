
using JoinRpg.DataModel.Projects;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Data.Interfaces.AdminTools;

public interface IKogdaIgraRepository
{
    Task<(KogdaIgraIdentification KogdaIgraId, string Name)[]> GetActive();
    Task<ICollection<KogdaIgraGame>> GetByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications);

    Task<IReadOnlyCollection<KogdaIgraGameData>> GetDataByIds(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIdentifications);
    Task<(KogdaIgraIdentification KogdaIgraId, string Name)[]> GetNotUpdated();
    Task<int> GetNotUpdatedCount();
    Task<KogdaIgraGame[]> GetNotUpdatedObjects();
}
