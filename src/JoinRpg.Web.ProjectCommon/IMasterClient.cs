using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.ProjectCommon;

public interface IMasterClient
{
    Task<List<MasterViewModel>> GetMasters(int projectId);
}
