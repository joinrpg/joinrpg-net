using System.Data.Entity.Validation;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.ProjectAccess;

namespace JoinRpg.Services.Impl;
internal class ProjectAccessService(IUnitOfWork unitOfWork, ICurrentUserAccessor currentUserAccessor, IResponsibleMasterRulesRepository responsibleMasterRulesRepository)
    : DbServiceImplBase(unitOfWork, currentUserAccessor), IProjectAccessService
{
    public async Task GrantAccess(GrantAccessRequest grantAccessRequest)
    {
        var project = await ProjectRepository.GetProjectAsync(grantAccessRequest.ProjectId);
        if (!IsCurrentUserAdmin)
        {
            _ = project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);
        }

        _ = project.EnsureProjectActive();

        var acl = project.ProjectAcls.SingleOrDefault(a => a.UserId == grantAccessRequest.UserId);
        if (acl == null)
        {
            acl = new ProjectAcl
            {
                ProjectId = project.ProjectId,
                UserId = grantAccessRequest.UserId,
            };
            project.ProjectAcls.Add(acl);
        }

        SetRightsFromRequest(grantAccessRequest, acl);

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterIdOrDefault)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        if (userId != CurrentUserId)
        {
            _ = project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);
        }

        if (!project.ProjectAcls.Any(a => a.CanGrantRights && a.UserId != userId))
        {
            throw new DbEntityValidationException();
        }

        var acl = project.ProjectAcls.Single(a => a.UserId == userId);

        var respFor = await responsibleMasterRulesRepository.GetResponsibleMasterRulesForMaster(new(projectId), new(CurrentUserId));
        if (respFor.Any(item => item.ResponsibleMasterUserId == userId))
        {
            throw new MasterHasResponsibleException(acl);
        }

        var claims =
            await ClaimsRepository.GetClaimsForMaster(projectId,
                acl.UserId,
                ClaimStatusSpec.Any);

        if (claims.Count != 0)
        {
            if (newResponsibleMasterIdOrDefault is int newResponsible)
            {
                _ = project.RequestMasterAccess(newResponsible);

                foreach (var claim in claims)
                {
                    claim.ResponsibleMasterUserId = newResponsible;
                }
            }
            else
            {
                throw new MasterHasResponsibleException(acl);
            }
        }

        if (acl.IsOwner)
        {
            if (acl.UserId == CurrentUserId)
            {
                // if owner removing himself, assign "random" owner
                project.ProjectAcls.OrderBy(a => a.UserId).First().IsOwner = true;
            }
            else
            {
                //who kills the king, becomes one
                project.ProjectAcls.Single(a => a.UserId == CurrentUserId).IsOwner = true;
            }
        }

        _ = UnitOfWork.GetDbSet<ProjectAcl>().Remove(acl);
        _ = UnitOfWork.GetDbSet<UserSubscription>()
            .RemoveRange(UnitOfWork.GetDbSet<UserSubscription>()
                         .Where(x => x.UserId == userId && x.ProjectId == projectId));

        await UnitOfWork.SaveChangesAsync();
    }

    public async Task ChangeAccess(ChangeAccessRequest changeAccessRequest)
    {
        var project = await ProjectRepository.GetProjectAsync(changeAccessRequest.ProjectId);
        _ = project.RequestMasterAccess(CurrentUserId, a => a.CanGrantRights);

        var acl = project.ProjectAcls.Single(
            a => a.ProjectId == changeAccessRequest.ProjectId && a.UserId == changeAccessRequest.UserId);
        SetRightsFromRequest(changeAccessRequest, acl);

        await UnitOfWork.SaveChangesAsync();
    }

    private static void SetRightsFromRequest(AccessRequestBase grantAccessRequest, ProjectAcl acl)
    {
        acl.CanGrantRights = grantAccessRequest.CanGrantRights;
        acl.CanChangeFields = grantAccessRequest.CanChangeFields;
        acl.CanChangeProjectProperties = grantAccessRequest.CanChangeProjectProperties;
        acl.CanManageClaims = grantAccessRequest.CanManageClaims;
        acl.CanEditRoles = grantAccessRequest.CanEditRoles;
        acl.CanManageMoney = grantAccessRequest.CanManageMoney;
        acl.CanSendMassMails = grantAccessRequest.CanSendMassMails;
        acl.CanManagePlots = grantAccessRequest.CanManagePlots;
        acl.CanManageAccommodation = grantAccessRequest.CanManageAccommodation &&
                                     acl.Project.Details.EnableAccommodation;
        acl.CanSetPlayersAccommodations = grantAccessRequest.CanSetPlayersAccommodations &&
                                          acl.Project.Details.EnableAccommodation;
    }
}
