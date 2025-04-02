
using JoinRpg.DataModel.Projects;

namespace JoinRpg.Data.Interfaces.AdminTools;
public interface IKogdaIgraRepository
{
    Task<(int KogdaIgraId, string Name)[]> GetAll();
    Task<KogdaIgraGame> GetById(int kogdaIgraId);
}
