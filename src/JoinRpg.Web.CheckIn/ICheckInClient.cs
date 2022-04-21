using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.CheckIn;

public interface ICheckInClient
{
    Task<CheckInStatViewModel> GetCheckInStats(ProjectIdentification projectId);
}
