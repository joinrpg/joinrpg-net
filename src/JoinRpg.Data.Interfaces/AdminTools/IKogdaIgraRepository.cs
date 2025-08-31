
using JoinRpg.DataModel.Projects;

namespace JoinRpg.Data.Interfaces.AdminTools;
public interface IKogdaIgraRepository
{
    Task<(KogdaIgraIdentification KogdaIgraId, string Name)[]> GetActive();
    Task<KogdaIgraGame> GetById(int kogdaIgraId);
    Task<ICollection<KogdaIgraGame>> GetByIds(KogdaIgraIdentification[] kogdaIgraIdentifications);
    Task<(KogdaIgraIdentification KogdaIgraId, string Name)[]> GetNotUpdated();
    Task<int> GetNotUpdatedCount();
    Task<KogdaIgraGame[]> GetNotUpdatedObjects();
}
