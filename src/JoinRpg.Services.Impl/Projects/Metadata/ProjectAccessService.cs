using System.Data.Entity.Validation;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
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
                    acl = new ProjectAcl { ProjectId = ctx.Project.ProjectId, UserId = ctx.Request.UserId, Project = ctx.Project };
                    ctx.Project.ProjectAcls.Add(acl);
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
                var acl = ctx.Project.ProjectAcls.Single(a => a.UserId == ctx.Request.UserId);
                acl.SetPermissions(ctx.Request.Permissions);
                if (ctx.Project.ProjectAcls.All(a => !a.CanGrantRights))
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
        var hasResponsibleGroups = projectInfo.ResponsibleMasterRules.Any(g => g.ResponsibleMasterId == userId);

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
                if (!ctx.Project.ProjectAcls.Any(a => a.CanGrantRights && a.UserId != ctx.Request.UserId.Value))
                {
                    throw new DbEntityValidationException();
                }

                var acl = ctx.Project.ProjectAcls.Single(a => a.UserId == ctx.Request.UserId.Value);

                foreach (var group in ctx.Project.CharacterGroups.Where(g => g.ResponsibleMasterUserId == ctx.Request.UserId.Value))
                {
                    group.ResponsibleMasterUserId = ctx.Request.NewResponsible?.Value;
                }

                if (acl.IsOwner)
                {
                    if (acl.UserId == ctx.CurrentUser.UserId)
                    {
                        ctx.Project.ProjectAcls.OrderBy(a => a.UserId).First().IsOwner = true;
                    }
                    else
                    {
                        ctx.Project.ProjectAcls.Single(a => a.UserId == ctx.CurrentUser.UserId).IsOwner = true;
                    }
                }

                ctx.RemovePermanently(acl);
            });

        await gameSubscribeService.RemoveAllSubscriptions(projectId, userId);
    }

    public Task GrantFullAccess(ProjectIdentification projectId)
    {
        logger.LogInformation("Администратор {UserId} запрашивает полный доступ к проекту {ProjectId}", currentUserAccessor.UserId, projectId);
        return GrantAccess(new GrantAccessRequest
        {
            ProjectId = projectId,
            UserId = currentUserAccessor.UserIdentification,
            Permissions = [.. Enum.GetValues<Permission>().Where(p => p != Permission.None)],
        });
    }
}
