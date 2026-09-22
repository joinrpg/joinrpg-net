using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.Web.ProjectCommon;

public interface IMasterClient
{
    Task<List<UserInfoHeader>> GetMasters(int projectId);
}
