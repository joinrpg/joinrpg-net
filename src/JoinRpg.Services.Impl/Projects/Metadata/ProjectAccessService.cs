using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Subscribe;

namespace JoinRpg.Services.Impl.Projects.Metadata;

internal class ProjectAccessService(
    IProjectPropsService projectPropsService,
    IClaimsRepository claimsRepository,
    IClaimService claimService,
    IGameSubscribeService gameSubscribeService,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<ProjectAccessService> logger) : IProjectAccessService
{
    public Task GrantAccess(GrantAccessRequest request)
        => projectPropsService.ChangeProjectProperties(
            request.ProjectId,
            Permission.CanGrantRights,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx =>
            {
                var acl = ctx.Project.ProjectAcls.SingleOrDefault(a => a.UserId == ctx.Request.UserId);
                if (acl is null)
                {
                    acl = new ProjectAcl
                    {
                        ProjectId = ctx.Project.ProjectId,
                        UserId = ctx.Request.UserId,
                        Project = ctx.Project,
                        Role = ctx.Request.Role,
                        IsPublic = ctx.Request.IsPublic,
                    };
                    ctx.Project.ProjectAcls.Add(acl);
                }
                else if (!acl.IsActive)
                {
                    // Повторная выдача снятому мастеру — та же строка (ADR019, §1): описание и публичность
                    // возвращаются вместе с ним, роль задаёт заново тот, кто выдаёт доступ.
                    acl.Status = ProjectAclStatus.Active;
                    acl.Role = ctx.Request.Role;
                }
                acl.SetPermissions(ctx.Request.Permissions);
            });

    public Task ChangeAccess(ChangeAccessRequest request)
        => projectPropsService.ChangeProjectProperties(
            request.ProjectId,
            Permission.CanGrantRights,
            ProjectActiveRequirement.MustBeActive,
            request,
            ctx =>
            {
                var acl = ctx.Project.ProjectAcls.Single(a => a.UserId == ctx.Request.UserId && a.IsActive);
                acl.SetPermissions(ctx.Request.Permissions);
                if (ctx.Project.ProjectAcls.Where(a => a.IsActive).All(a => !a.CanGrantRights))
                {
                    acl.CanGrantRights = true; // последний с CanGrantRights не может снять его сам с себя
                }
            });

    public async Task RemoveAccess(ProjectIdentification projectId, UserIdentification userId, UserIdentification? newResponsibleMasterIdOrDefault)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        var requiredPermission = userId == currentUserAccessor.UserIdentification ? Permission.None : Permission.CanGrantRights;
        if (!currentUserAccessor.IsAdmin)
        {
            _ = projectInfo.RequestMasterAccess(currentUserAccessor, requiredPermission);
        }

        var claims = await claimsRepository.GetClaimsForMaster(projectId, userId, ClaimStatusSpec.Any);
        var hasResponsibleGroups = projectInfo.GroupTree.ResponsibleMasterRules.Any(g => g.ResponsibleMasterId == userId);

        if (claims.Count > 0 || hasResponsibleGroups)
        {
            if (newResponsibleMasterIdOrDefault is null || newResponsibleMasterIdOrDefault == userId)
            {
                throw new MasterHasResponsibleException(projectId, userId);
            }

            _ = projectInfo.RequestMasterAccess(newResponsibleMasterIdOrDefault); // newResponsible must actually be a project master

            foreach (var claim in claims)
            {
                await claimService.SetResponsible(claim.GetId(), newResponsibleMasterIdOrDefault);
            }
        }

        await projectPropsService.ChangeProjectProperties(
            projectId,
            requiredPermission,
            ProjectActiveRequirement.AllowInactive,
            (UserId: userId, NewResponsible: newResponsibleMasterIdOrDefault),
            ctx =>
            {
                // Заодно держит инвариант «у проекта остаётся хотя бы один действующий мастер» —
                // на нём стоят выбор ответственного по умолчанию и передача владения ниже.
                if (!ctx.Project.ProjectAcls.Any(a => a.IsActive && a.CanGrantRights && a.UserId != ctx.Request.UserId.Value))
                {
                    throw new LastMasterWithGrantRightsException(ctx.ProjectInfo.ProjectId, ctx.Request.UserId);
                }

                var acl = ctx.Project.ProjectAcls.Single(a => a.UserId == ctx.Request.UserId.Value && a.IsActive);

                foreach (var group in ctx.Project.CharacterGroups.Where(g => g.ResponsibleMasterUserId == ctx.Request.UserId.Value))
                {
                    group.ResponsibleMasterUserId = ctx.Request.NewResponsible?.Value;
                }

                if (acl.IsOwner)
                {
                    // Владение уходит тому, кто снимает (если это не сам владелец), иначе — действующему мастеру
                    // с наименьшим UserId. Снятым мастерам владение не передаётся. Админ сайта, не будучи
                    // мастером проекта, тоже попадает во вторую ветку.
                    var newOwner = ctx.Project.ProjectAcls.SingleOrDefault(a => a != acl && a.IsActive && a.UserId == ctx.CurrentUser.UserId)
                        ?? ctx.Project.ProjectAcls.Where(a => a != acl && a.IsActive).OrderBy(a => a.UserId).First();
                    newOwner.IsOwner = true;
                }

                // Мягкое удаление (ADR019, §1): строка остаётся историей. Права сбрасываются — защита в глубину
                // на случай проверки, которая забыла про статус, но спрашивает конкретное право.
                acl.Status = ProjectAclStatus.Removed;
                acl.IsOwner = false;
                acl.SetPermissions([]);
            });

        await gameSubscribeService.RemoveAllSubscriptions(projectId, userId);
    }

    public Task RegisterFormerMasters(ProjectIdentification projectId, IReadOnlyCollection<UserIdentification> userIds)
        => projectPropsService.ChangeProjectProperties(
            projectId,
            Permission.CanGrantRights, // джоба работает под роботом-админом, admin-bypass срабатывает сам
            ProjectActiveRequirement.AllowInactive, // почти все такие проекты в архиве
            userIds,
            ctx =>
            {
                foreach (var userId in ctx.Request.Where(u => !ctx.Project.ProjectAcls.Any(a => a.UserId == u.Value)).Distinct())
                {
                    ctx.Project.ProjectAcls.Add(new ProjectAcl
                    {
                        ProjectId = ctx.Project.ProjectId,
                        UserId = userId.Value,
                        Project = ctx.Project,
                        Status = ProjectAclStatus.Removed,
                        Role = "Мастер", // ADR019, §4. TODO[Localize]
                    });
                }
            });

    public Task GrantFullAccess(ProjectIdentification projectId)
    {
        logger.LogInformation("Администратор {UserId} запрашивает полный доступ к проекту {ProjectId}", currentUserAccessor.UserId, projectId);
        return GrantAccess(new GrantAccessRequest
        {
            ProjectId = projectId,
            UserId = currentUserAccessor.UserIdentification,
            Permissions = [.. Enum.GetValues<Permission>().Where(p => p != Permission.None)],
            // Админ сайта, зашедший помочь, — не член команды проекта (ADR019, §4). TODO[Localize]
            Role = "Техподдержка joinrpg.ru",
            IsPublic = false,
        });
    }
}
