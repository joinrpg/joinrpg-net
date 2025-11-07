using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl;
internal class RespMasterRuleService(IUnitOfWork unitOfWork,
                                     ICurrentUserAccessor currentUserAccessor,
                                     IProjectMetadataRepository projectMetadataRepository)
    : DbServiceImplBase(unitOfWork, currentUserAccessor), IRespMasterRuleService
{
    Task IRespMasterRuleService.AddRule(ProjectIdentification projectId, int ruleId, int masterId)
        => ModifyRule(projectId, ruleId, masterId);
    Task IRespMasterRuleService.ChangeRule(ProjectIdentification projectId, int ruleId, int masterId)
        => ModifyRule(projectId, ruleId, masterId);
    Task IRespMasterRuleService.RemoveRule(ProjectIdentification projectId, int ruleId)
        => ModifyRule(projectId, ruleId, null);

    private async Task ModifyRule(ProjectIdentification projectId, int ruleId, int? masterId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        projectInfo
            .RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims)
            .EnsureProjectActive();
        var characterGroup = await ProjectRepository.GetGroupAsync(new CharacterGroupIdentification(projectId, ruleId));

        if (characterGroup is null)
        {
            throw new JoinRpgEntityNotFoundException(ruleId, "CharacterGroup");
        }

        if (masterId is not null)
        {
            projectInfo.RequestMasterAccess(UserIdentification.FromOptional(masterId));
        }

        characterGroup.ResponsibleMasterUserId = masterId;

        MarkChanged(characterGroup);
        await UnitOfWork.SaveChangesAsync();
    }
}
