using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Web.ProjectMasterTools.CaptainRules;

public interface ICaptainRuleClient
{
    Task<CaptainRuleListViewModel> GetList(ProjectIdentification projectId);
    Task Remove(CaptainAccessRule captainAccessRule);

    Task<CaptainRuleListViewModel> AddOrChange(CaptainAccessRule captainAccessRule);
}
