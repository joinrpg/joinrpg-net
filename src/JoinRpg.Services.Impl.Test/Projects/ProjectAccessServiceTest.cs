using System.Data.Entity.Validation;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Impl.Projects.Metadata;
using JoinRpg.Services.Interfaces.ProjectAccess;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Projects;

public class ProjectAccessServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly FakeProjectMetadataRepository metadataRepository;
    private readonly FakeClaimsRepository claimsRepository = new();
    private readonly FakeClaimService claimService = new();
    private readonly FakeGameSubscribeService gameSubscribeService = new();

    public ProjectAccessServiceTest()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
    }

    private ProjectIdentification ProjectId => mock.ProjectInfo.ProjectId;

    /// <summary>Добавляет ещё одного мастера с заданным правом CanGrantRights (остальные права выключены).</summary>
    private ProjectAcl AddMaster(int userId, bool canGrantRights)
    {
        var acl = new ProjectAcl
        {
            ProjectId = mock.Project.ProjectId,
            UserId = userId,
            Project = mock.Project,
            User = new User { UserId = userId, PrefferedName = $"User{userId}", Email = $"u{userId}@example.com", Claims = [] },
            CanGrantRights = canGrantRights,
        };
        mock.Project.ProjectAcls.Add(acl);
        mock.ReInitProjectInfo();
        return acl;
    }

    private ProjectAccessService CreateService(int currentUserId, bool isAdmin = false)
    {
        var currentUser = new FakeCurrentUserAccessor(currentUserId, isAdmin);
        var propsService = new ProjectPropsService(unitOfWork, currentUser, metadataRepository, NullLogger<ProjectPropsService>.Instance);
        return new ProjectAccessService(
            propsService,
            claimsRepository,
            claimService,
            gameSubscribeService,
            metadataRepository,
            currentUser,
            NullLogger<ProjectAccessService>.Instance);
    }

    [Fact]
    public async Task GrantAccess_NewUser_CreatesAclWithGivenPermissions()
    {
        var service = CreateService(mock.Master.UserId);

        await service.GrantAccess(new GrantAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims, Permission.CanEditRoles],
        });

        var acl = mock.Project.ProjectAcls.Single(a => a.UserId == 50);
        acl.CanManageClaims.ShouldBeTrue();
        acl.CanEditRoles.ShouldBeTrue();
        acl.CanGrantRights.ShouldBeFalse();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        metadataRepository.LastPrimed.ShouldNotBeNull();
    }

    [Fact]
    public async Task GrantAccess_ExistingUser_ReplacesPermissions()
    {
        var acl = AddMaster(50, canGrantRights: false);
        acl.CanManageClaims = true;

        var service = CreateService(mock.Master.UserId);

        await service.GrantAccess(new GrantAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanGrantRights],
        });

        acl.CanGrantRights.ShouldBeTrue();
        acl.CanManageClaims.ShouldBeFalse();
    }

    [Fact]
    public async Task GrantAccess_WithoutCanGrantRights_Throws_AndDoesNotSave()
    {
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.GrantAccess(new GrantAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims],
        }));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task GrantAccess_Admin_BypassesRightsCheck()
    {
        var service = CreateService(mock.Player.UserId, isAdmin: true);

        await service.GrantAccess(new GrantAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims],
        });

        mock.Project.ProjectAcls.ShouldContain(a => a.UserId == 50);
    }

    [Fact]
    public async Task ChangeAccess_UpdatesPermissions()
    {
        var acl = AddMaster(50, canGrantRights: false);
        acl.CanManageClaims = true;

        var service = CreateService(mock.Master.UserId);

        await service.ChangeAccess(new ChangeAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanEditRoles],
        });

        acl.CanEditRoles.ShouldBeTrue();
        acl.CanManageClaims.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeAccess_LastCanGrantRights_ForcesRightBackOn()
    {
        // mock.Master — единственный с CanGrantRights, пытается снять его сам с себя
        var service = CreateService(mock.Master.UserId);

        await service.ChangeAccess(new ChangeAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(mock.Master.UserId),
            Permissions = [],
        });

        var acl = mock.Project.ProjectAcls.Single(a => a.UserId == mock.Master.UserId);
        acl.CanGrantRights.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAccess_SelfRemoval_WithoutCanGrantRights_Succeeds_AndCleansUpSubscriptions()
    {
        // mock.Master сохраняет CanGrantRights, так что «последний хранитель ключей» не пострадает
        AddMaster(50, canGrantRights: false);

        var service = CreateService(50);

        await service.RemoveAccess(ProjectId, new UserIdentification(50), null);

        mock.Project.ProjectAcls.ShouldNotContain(a => a.UserId == 50);
        gameSubscribeService.RemoveAllSubscriptionsCalls.ShouldContain((ProjectId, new UserIdentification(50)));
    }

    [Fact]
    public async Task RemoveAccess_OtherUser_WithoutCanGrantRights_Throws_AndDoesNotSave()
    {
        AddMaster(50, canGrantRights: false);

        var service = CreateService(50);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.RemoveAccess(ProjectId, new UserIdentification(mock.Master.UserId), null));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RemoveAccess_LastMasterWithCanGrantRights_SelfRemoval_Throws()
    {
        // mock.Master — единственный с CanGrantRights
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<DbEntityValidationException>(
            () => service.RemoveAccess(ProjectId, new UserIdentification(mock.Master.UserId), null));

        mock.Project.ProjectAcls.ShouldContain(a => a.UserId == mock.Master.UserId);
    }

    [Fact]
    public async Task RemoveAccess_HasResponsibleClaims_WithoutNewResponsible_ThrowsMasterHasResponsibleException()
    {
        AddMaster(50, canGrantRights: false);
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.ResponsibleMasterUserId = 50;
        claimsRepository.ClaimsByResponsibleMaster[(mock.Project.ProjectId, 50)] = [claim];

        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<MasterHasResponsibleException>(
            () => service.RemoveAccess(ProjectId, new UserIdentification(50), null));

        mock.Project.ProjectAcls.ShouldContain(a => a.UserId == 50);
        claimService.ResponsibleChanges.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveAccess_WithNewResponsible_ReassignsClaimsAndGroups_ThenRemovesAcl()
    {
        AddMaster(50, canGrantRights: false);
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        claim.ResponsibleMasterUserId = 50;
        claimsRepository.ClaimsByResponsibleMaster[(mock.Project.ProjectId, 50)] = [claim];

        var group = mock.CreateCharacterGroup();
        group.ResponsibleMasterUserId = 50;
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);
        var newResponsible = new UserIdentification(mock.Master.UserId);

        await service.RemoveAccess(ProjectId, new UserIdentification(50), newResponsible);

        claimService.ResponsibleChanges.ShouldContain((claim.GetId(), newResponsible));
        group.ResponsibleMasterUserId.ShouldBe(mock.Master.UserId);
        mock.Project.ProjectAcls.ShouldNotContain(a => a.UserId == 50);
        gameSubscribeService.RemoveAllSubscriptionsCalls.ShouldContain((ProjectId, new UserIdentification(50)));
    }

    [Fact]
    public async Task GrantFullAccess_AsAdmin_GrantsAllPermissionsToSelf()
    {
        var service = CreateService(99, isAdmin: true);

        await service.GrantFullAccess(ProjectId);

        var acl = mock.Project.ProjectAcls.Single(a => a.UserId == 99);
        acl.CanGrantRights.ShouldBeTrue();
        acl.CanManageClaims.ShouldBeTrue();
        acl.CanEditRoles.ShouldBeTrue();
        acl.CanManageMoney.ShouldBeTrue();
        acl.CanSendMassMails.ShouldBeTrue();
        acl.CanManagePlots.ShouldBeTrue();
        acl.CanChangeFields.ShouldBeTrue();
        acl.CanChangeProjectProperties.ShouldBeTrue();
    }

    [Fact]
    public async Task GrantFullAccess_NonAdminWithoutRights_Throws()
    {
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.GrantFullAccess(ProjectId));
    }
}
