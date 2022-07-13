using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl;
internal class RespMasterRuleService : DbServiceImplBase, IRespMasterRuleService
{
    public RespMasterRuleService(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor) : base(unitOfWork, currentUserAccessor)
    {
    }

    Task IRespMasterRuleService.AddRule(ProjectIdentification projectId, int ruleId, int masterId)
        => ModifyRule(projectId, ruleId, masterId);
    Task IRespMasterRuleService.ChangeRule(ProjectIdentification projectId, int ruleId, int masterId)
        => ModifyRule(projectId, ruleId, masterId);
    Task IRespMasterRuleService.RemoveRule(ProjectIdentification projectId, int ruleId)
        => ModifyRule(projectId, ruleId, null);

    private async Task ModifyRule(ProjectIdentification projectId, int ruleId, int? masterId)
    {
        var characterGroup =
            (await ProjectRepository.GetGroupAsync(projectId, ruleId))
            .RequestMasterAccess(CurrentUserId, acl => acl.CanEditRoles)
            .EnsureProjectActive();

        if (masterId is not null)
        {
            characterGroup.RequestMasterAccess(masterId);
        }

        characterGroup.ResponsibleMasterUserId = masterId;

        MarkChanged(characterGroup);
        await UnitOfWork.SaveChangesAsync();
    }
}
