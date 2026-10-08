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
    private readonly FakeClaimsRepository claimsRepository;
    private readonly FakeClaimService claimService = new();
    private readonly FakeGameSubscribeService gameSubscribeService = new();

    public ProjectAccessServiceTest()
    {
        unitOfWork = new FakeUnitOfWork(mock);
        metadataRepository = new FakeProjectMetadataRepository(mock);
        claimsRepository = new FakeClaimsRepository(mock);
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
            Role = "Мастер",
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
            Role = new("Мастер"),
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims, Permission.CanEditRoles],
        });

        var acl = mock.Project.ProjectAcls.Single(a => a.UserId == 50);
        acl.CanManageClaims.ShouldBeTrue();
        acl.CanEditRoles.ShouldBeTrue();
        acl.CanGrantRights.ShouldBeFalse();
        acl.Role.ShouldBe("Мастер");
        acl.IsPublic.ShouldBeTrue();
        acl.Status.ShouldBe(ProjectAclStatus.Active);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        metadataRepository.LastPrimed.ShouldNotBeNull();
    }

    [Fact]
    public async Task GrantAccess_NewUser_PopulatesAclUser_AndProjectInfoRefreshesWithoutError()
    {
        // Регрессия: ProjectAcl для нового мастера создаётся через `new`, а не через прокси-фабрику
        // EF6, поэтому lazy loading для acl.User не сработает. Правильное поведение — перечитать
        // Project из БД после SaveChanges (см. ProjectMetadataWriteRepository.Refresh), а не
        // пересобирать ProjectInfo по уже загруженному в памяти графу: иначе CreateInfoFromProject
        // падает с NullReferenceException (см. CreateMasterList).
        var service = CreateService(mock.Master.UserId);

        await service.GrantAccess(new GrantAccessRequest
        {
            ProjectId = ProjectId,
            Role = new("Мастер"),
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims],
        });

        var acl = mock.Project.ProjectAcls.Single(a => a.UserId == 50);
        acl.User.ShouldNotBeNull();
        acl.User.UserId.ShouldBe(50);

        mock.ProjectInfo.Masters.ShouldContain(m => m.UserId == new UserIdentification(50));
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
            Role = new("Мастер"),
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
            Role = new("Мастер"),
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
            Role = new("Мастер"),
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

        ShouldBeRemoved(50);
        gameSubscribeService.RemoveAllSubscriptionsCalls.ShouldContain((ProjectId, new UserIdentification(50)));
    }

    /// <summary>
    /// Снятый мастер (ADR019, §1): строка осталась, статус Removed, прав и владения нет,
    /// в пересобранном ProjectInfo он среди бывших, а не действующих.
    /// </summary>
    private void ShouldBeRemoved(int userId)
    {
        var acl = mock.Project.ProjectAcls.Single(a => a.UserId == userId);
        acl.Status.ShouldBe(ProjectAclStatus.Removed);
        acl.IsOwner.ShouldBeFalse();
        new[]
        {
            acl.CanChangeFields, acl.CanChangeProjectProperties, acl.CanGrantRights, acl.CanManageClaims,
            acl.CanEditRoles, acl.CanManageMoney, acl.CanSendMassMails, acl.CanManagePlots,
            acl.CanManageAccommodation, acl.CanSetPlayersAccommodations,
        }.ShouldAllBe(flag => !flag);

        var projectInfo = metadataRepository.LastPrimed.ShouldNotBeNull();
        projectInfo.Masters.ShouldNotContain(m => m.UserId == new UserIdentification(userId));
        projectInfo.FormerMasters.ShouldContain(m => m.UserId == new UserIdentification(userId));
    }

    [Fact]
    public async Task RemoveAccess_OwnerRemovesSelf_WithLowestUserId_TransfersOwnershipToRemainingMaster()
    {
        // Регрессия: владелец (mock.Master, UserId = 2 — наименьший) уходит сам. Владение передавалось
        // мастеру с наименьшим UserId без исключения удаляемого — то есть ему же, и проект оставался без владельца.
        var remaining = AddMaster(50, canGrantRights: true);

        var service = CreateService(mock.Master.UserId);

        await service.RemoveAccess(ProjectId, new UserIdentification(mock.Master.UserId), null);

        ShouldBeRemoved(mock.Master.UserId);
        remaining.IsOwner.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAccess_OwnerRemovesSelf_DoesNotTransferOwnershipToRemovedMaster()
    {
        // Снятый мастер с меньшим UserId, чем у действующего, — владение должно уйти действующему.
        var former = AddMaster(3, canGrantRights: false);
        former.Status = ProjectAclStatus.Removed;
        var remaining = AddMaster(50, canGrantRights: true);
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        await service.RemoveAccess(ProjectId, new UserIdentification(mock.Master.UserId), null);

        former.IsOwner.ShouldBeFalse();
        remaining.IsOwner.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAccess_LastActiveWithCanGrantRights_IgnoresRemovedMastersWithStaleRights()
    {
        // У снятого мастера права «застряли» (как у строк до сброса прав) — он не считается хранителем ключей.
        var former = AddMaster(50, canGrantRights: true);
        former.Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<LastMasterWithGrantRightsException>(
            () => service.RemoveAccess(ProjectId, new UserIdentification(mock.Master.UserId), null));
    }

    [Fact]
    public async Task RemoveAccess_AdminNotMasterRemovesOwner_TransfersOwnershipToActiveMaster()
    {
        // Раньше владение передавалось снимающему через Single(текущий пользователь) — для админа сайта,
        // который не мастер проекта, это падало.
        var remaining = AddMaster(50, canGrantRights: true);

        var service = CreateService(99, isAdmin: true);

        await service.RemoveAccess(ProjectId, new UserIdentification(mock.Master.UserId), null);

        ShouldBeRemoved(mock.Master.UserId);
        remaining.IsOwner.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAccess_MasterRemovesOwner_TransfersOwnershipToCurrentUser()
    {
        var lowerId = AddMaster(3, canGrantRights: false);
        var current = AddMaster(50, canGrantRights: true);

        var service = CreateService(50);

        await service.RemoveAccess(ProjectId, new UserIdentification(mock.Master.UserId), null);

        current.IsOwner.ShouldBeTrue();
        lowerId.IsOwner.ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveAccess_AlreadyRemovedMaster_Throws()
    {
        var former = AddMaster(50, canGrantRights: false);
        former.Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();

        var service = CreateService(99, isAdmin: true);

        await Should.ThrowAsync<InvalidOperationException>(
            () => service.RemoveAccess(ProjectId, new UserIdentification(50), null));
    }

    [Fact]
    public async Task ChangeAccess_RemovedMaster_ReturnsWithFormerProfile()
    {
        // Бывшего мастера возвращают со страницы правки прав (ADR019, §1): та же строка, прежний профиль.
        var former = AddMaster(50, canGrantRights: false);
        former.Status = ProjectAclStatus.Removed;
        former.Role = "Мастер по боёвке";
        former.IsPublic = false;
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        await service.ChangeAccess(new ChangeAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims],
        });

        mock.Project.ProjectAcls.Count(a => a.UserId == 50).ShouldBe(1);
        former.Status.ShouldBe(ProjectAclStatus.Active);
        former.CanManageClaims.ShouldBeTrue();
        former.Role.ShouldBe("Мастер по боёвке");
        former.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeAccess_NotMaster_Throws()
    {
        // Нового мастера добавляют через GrantAccess — с ролью; правка прав строку не создаёт.
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<InvalidOperationException>(() => service.ChangeAccess(new ChangeAccessRequest
        {
            ProjectId = ProjectId,
            UserId = new UserIdentification(60),
            Permissions = [Permission.CanManageClaims],
        }));
        mock.Project.ProjectAcls.ShouldNotContain(a => a.UserId == 60);
    }

    [Fact]
    public async Task GrantFullAccess_AdminWithRemovedRow_ReactivatesSameRow()
    {
        var former = AddMaster(99, canGrantRights: false);
        former.Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();

        var service = CreateService(99, isAdmin: true);

        await service.GrantFullAccess(ProjectId);

        mock.Project.ProjectAcls.Count(a => a.UserId == 99).ShouldBe(1);
        former.Status.ShouldBe(ProjectAclStatus.Active);
        former.CanGrantRights.ShouldBeTrue();
        former.Role.ShouldBe("Техподдержка joinrpg.ru");
    }

    [Fact]
    public async Task GrantAccess_RemovedMaster_ReactivatesSameRow()
    {
        var acl = AddMaster(50, canGrantRights: false);
        acl.Status = ProjectAclStatus.Removed;
        acl.Role = "Мастер по боёвке";
        acl.IsPublic = false;
        acl.Description = new MarkdownDbValue("Боёвка и полигон");
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        // Профиль при повторной выдаче — из запроса (так возвращает админа GrantFullAccess).
        await service.GrantAccess(new GrantAccessRequest
        {
            ProjectId = ProjectId,
            Role = new("Мастер по экономике"),
            Description = new MarkdownString("Экономика и полигон"),
            IsPublic = false,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims],
        });

        mock.Project.ProjectAcls.Count(a => a.UserId == 50).ShouldBe(1);
        acl.Status.ShouldBe(ProjectAclStatus.Active);
        acl.CanManageClaims.ShouldBeTrue();
        // Профиль — из запроса.
        acl.Role.ShouldBe("Мастер по экономике");
        acl.IsPublic.ShouldBeFalse();
        acl.Description.Contents.ShouldBe("Экономика и полигон");
        metadataRepository.LastPrimed.ShouldNotBeNull().Masters.ShouldContain(m => m.UserId == new UserIdentification(50));
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

        await Should.ThrowAsync<LastMasterWithGrantRightsException>(
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
        ShouldBeRemoved(50);
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
        // ADR019, §4: админ сайта — не член команды, игрокам его не показываем.
        acl.Role.ShouldBe("Техподдержка joinrpg.ru");
        acl.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public async Task GrantAccess_ExistingMaster_KeepsRoleAndPublicity()
    {
        // Роль и публичность из запроса — только для нового мастера: admin-access на действующего мастера
        // не должен переименовать его в «Техподдержку» и спрятать от игроков.
        var acl = AddMaster(50, canGrantRights: false);
        acl.Role = "Мастер по боёвке";

        var service = CreateService(mock.Master.UserId);

        await service.GrantAccess(new GrantAccessRequest
        {
            ProjectId = ProjectId,
            Role = new("Техподдержка joinrpg.ru"),
            IsPublic = false,
            UserId = new UserIdentification(50),
            Permissions = [Permission.CanManageClaims],
        });

        acl.Role.ShouldBe("Мастер по боёвке");
        acl.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public async Task GrantFullAccess_NonAdminWithoutRights_Throws()
    {
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.GrantFullAccess(ProjectId));
    }

    private ChangeMasterProfileRequest ProfileRequest(int userId) => new()
    {
        ProjectId = ProjectId,
        UserId = new UserIdentification(userId),
        Role = new("Мастер по боёвке"),
        Description = new MarkdownString("Боёвка и полигон"),
        IsPublic = false,
    };

    [Fact]
    public async Task ChangeMasterProfile_Self_WithoutCanGrantRights_Succeeds()
    {
        var acl = AddMaster(50, canGrantRights: false);

        var service = CreateService(50);

        await service.ChangeMasterProfile(ProfileRequest(50));

        acl.Role.ShouldBe("Мастер по боёвке");
        acl.Description.Contents.ShouldBe("Боёвка и полигон");
        acl.IsPublic.ShouldBeFalse();
        metadataRepository.LastPrimed.ShouldNotBeNull().Masters.Single(m => m.UserId == new UserIdentification(50)).IsPublic.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangeMasterProfile_Other_WithoutCanGrantRights_Throws()
    {
        AddMaster(50, canGrantRights: false);
        var other = AddMaster(51, canGrantRights: false);

        var service = CreateService(50);

        await Should.ThrowAsync<NoAccessToProjectException>(() => service.ChangeMasterProfile(ProfileRequest(51)));
        other.Role.ShouldBe("Мастер");
    }

    [Fact]
    public async Task ChangeMasterProfile_Other_WithCanGrantRights_Succeeds()
    {
        var other = AddMaster(51, canGrantRights: false);

        var service = CreateService(mock.Master.UserId);

        await service.ChangeMasterProfile(ProfileRequest(51));

        other.Role.ShouldBe("Мастер по боёвке");
    }

    [Fact]
    public async Task ChangeMasterProfile_RemovedMaster_Throws()
    {
        var former = AddMaster(51, canGrantRights: false);
        former.Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();

        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<InvalidOperationException>(() => service.ChangeMasterProfile(ProfileRequest(51)));
    }

    /// <summary>Мастера 2 (владелец), 50, 60 и снятый 61; по умолчанию идут по ProjectAclId — в этом порядке.</summary>
    private void SetUpMastersForOrdering()
    {
        mock.Project.ProjectAcls.Single(a => a.UserId == mock.Master.UserId).ProjectAclId = 1;
        AddMaster(50, canGrantRights: false).ProjectAclId = 2;
        AddMaster(60, canGrantRights: false).ProjectAclId = 3;
        var former = AddMaster(61, canGrantRights: false);
        former.ProjectAclId = 4;
        former.Status = ProjectAclStatus.Removed;
        mock.ReInitProjectInfo();
    }

    private UserIdentification[] OrderAfterSave() => [.. metadataRepository.LastPrimed.ShouldNotBeNull().Masters.Select(m => m.UserId)];

    [Fact]
    public async Task MoveMasterAfter_PutsMasterAfterGivenOne()
    {
        SetUpMastersForOrdering();
        var service = CreateService(mock.Master.UserId);

        await service.MoveMasterAfter(ProjectId, new UserIdentification(mock.Master.UserId), new UserIdentification(60));

        OrderAfterSave().ShouldBe([new(50), new(60), new(mock.Master.UserId)]);
    }

    [Fact]
    public async Task MoveMasterAfter_Null_PutsMasterFirst()
    {
        SetUpMastersForOrdering();
        var service = CreateService(mock.Master.UserId);

        await service.MoveMasterAfter(ProjectId, new UserIdentification(60), afterUserId: null);

        OrderAfterSave().ShouldBe([new(60), new(mock.Master.UserId), new(50)]);
    }

    [Theory]
    [InlineData(61, 50)] // снятого мастера двигать нельзя — страница устарела
    [InlineData(50, 61)] // «после» снятого — тоже
    public async Task MoveMasterAfter_WithRemovedMaster_IsNoOp(int userId, int afterUserId)
    {
        SetUpMastersForOrdering();
        var before = mock.ProjectInfo.Masters.Select(m => m.UserId).ToArray();
        var service = CreateService(mock.Master.UserId);

        await service.MoveMasterAfter(ProjectId, new UserIdentification(userId), new UserIdentification(afterUserId));

        OrderAfterSave().ShouldBe(before);
    }

    [Fact]
    public async Task MoveMasterAfter_WithoutCanGrantRights_Throws()
    {
        AddMaster(50, canGrantRights: false);

        var service = CreateService(50);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.MoveMasterAfter(ProjectId, new UserIdentification(50), afterUserId: null));
    }
}
