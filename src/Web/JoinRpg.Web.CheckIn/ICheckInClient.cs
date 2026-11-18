using JoinRpg.DomainTypes;

namespace JoinRpg.Web.CheckIn;

public interface ICheckInClient
{
    Task<CheckInStatViewModel> GetCheckInStats(ProjectIdentification projectId);
}
