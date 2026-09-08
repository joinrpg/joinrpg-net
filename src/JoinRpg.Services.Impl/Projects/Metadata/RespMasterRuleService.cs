using JoinRpg.Domain;

namespace JoinRpg.Services.Impl.Projects.Metadata;

internal class RespMasterRuleService(IProjectPropsService projectPropsService) : IRespMasterRuleService
{
    public Task AddRule(ProjectIdentification projectId, int ruleId, int masterId)
        => ModifyRule(projectId, ruleId, masterId);

    public Task ChangeRule(ProjectIdentification projectId, int ruleId, int masterId)
        => ModifyRule(projectId, ruleId, masterId);

    public Task RemoveRule(ProjectIdentification projectId, int ruleId)
        => ModifyRule(projectId, ruleId, null);

    private Task ModifyRule(ProjectIdentification projectId, int ruleId, int? masterId)
        => projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanManageClaims,
            ProjectActiveRequirement.MustBeActive,
            (ruleId, masterId),
            ctx =>
            {
                // Правило ответственного мастера допустимо на любой группе, включая корневую и
                // специальные, — поэтому тип группы здесь не проверяется.
                var (characterGroup, _) = ctx.GetAnyCharacterGroupForChange(
                    new CharacterGroupIdentification(projectId, ctx.Request.ruleId));

                if (ctx.Request.masterId is not null)
                {
                    _ = ctx.ProjectInfo.RequestMasterAccess(UserIdentification.FromOptional(ctx.Request.masterId));
                }

                characterGroup.ResponsibleMasterUserId = ctx.Request.masterId;
            });
}
