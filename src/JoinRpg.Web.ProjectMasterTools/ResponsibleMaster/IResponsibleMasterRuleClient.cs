using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;
public interface IResponsibleMasterRuleClient
{
    Task<ResponsibleMasterRuleListViewModel> GetResponsibleMasterRuleList(ProjectIdentification projectId);
    Task RemoveResponsibleMasterRule(ProjectIdentification projectId, int ruleId);

    Task AddResponsibleMasterRule(ProjectIdentification projectIdentification, int groupId, int masterId);
}
