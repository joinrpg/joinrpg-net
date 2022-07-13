using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces;
public interface IRespMasterRuleService
{
    Task RemoveRule(ProjectIdentification projectId, int ruleId);
    Task ChangeRule(ProjectIdentification projectId, int ruleId, int masterId);

    Task AddRule(ProjectIdentification projectId, int ruleId, int masterId);
}
