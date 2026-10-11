using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Предпросмотр слияния пользователей (ADR024): какие конфликты он находит. Логика конфликтов
/// целиком в SQL-запросах, поэтому проверяется на настоящей базе.
/// </summary>
public class UserMergePreviewScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task FreshUsers_NoConflicts()
    {
        var (a, b) = await CreateUsers();

        var preview = await GetPreview(a, b);

        preview.CanMerge.ShouldBeTrue();
        preview.DroppedLoginProviders.ShouldBeEmpty();
        preview.SecondaryStats.ShouldBe(new UserMergeSecondaryStats(0, 0, 0));
    }

    [Fact]
    public async Task SameUser_Conflict()
    {
        var (a, _) = await CreateUsers();

        var preview = await GetPreview(a, a);

        // Предпросмотр идёт от имени A, поэтому заодно это и «поглощаемый — вы сами».
        preview.Conflicts.ShouldBe([new UserMergeConflict.SameUser(), new UserMergeConflict.SecondaryIsCurrentUser()]);
    }

    [Fact]
    public async Task SecondaryIsCurrentAdmin_Conflict()
    {
        var (a, b) = await CreateUsers();

        var preview = await factory.Services.RunAsAsync(b, sp => sp.GetRequiredService<IUserMergeService>().GetPreview(a, b), isAdmin: true);

        preview.Conflicts.ShouldBe([new UserMergeConflict.SecondaryIsCurrentUser()]);
    }

    [Fact]
    public async Task RobotUser_Conflict()
    {
        var (a, _) = await CreateUsers();
        UserIdentification robot;
        using (var scope = factory.Services.CreateScope())
        {
            robot = scope.ServiceProvider.GetRequiredService<IVirtualUsersService>().RobotUserId;
        }

        var preview = await GetPreview(a, robot);

        preview.Conflicts.ShouldBe([new UserMergeConflict.VirtualUser(robot)]);
    }

    [Fact]
    public async Task NotAdmin_Forbidden()
    {
        var (a, b) = await CreateUsers();

        _ = await Should.ThrowAsync<MustBeAdminException>(
            factory.Services.RunAsAsync(a, sp => sp.GetRequiredService<IUserMergeService>().GetPreview(a, b)));
    }

    [Fact]
    public async Task BothMasters_Conflict()
    {
        var (a, b) = await CreateUsers();
        var projectId = await CreateProject(a);
        await factory.Services.RunAsAsync(a, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = b,
                Role = new("Мастер"),
                Permissions = [],
            }));

        var preview = await GetPreview(a, b);

        preview.Conflicts.ShouldBe([new UserMergeConflict.BothMasters(projectId)]);
        preview.SecondaryStats.MasterProjects.ShouldBe(1);
    }

    [Fact]
    public async Task BothClaimedCharacter_Conflict()
    {
        var (a, b) = await CreateUsers();
        var masterId = await CreateUser();
        var projectId = await CreateProject(masterId);
        var characterId = await AddCharacter(masterId, projectId);
        _ = await AddClaim(a, characterId);
        _ = await AddClaim(b, characterId);

        var preview = await GetPreview(a, b);

        preview.Conflicts.ShouldBe([new UserMergeConflict.BothClaimedCharacter(characterId)]);
        preview.SecondaryStats.Claims.ShouldBe(1);
    }

    [Fact]
    public async Task DeclinedClaimOnSameCharacter_NoConflict()
    {
        var (a, b) = await CreateUsers();
        var masterId = await CreateUser();
        var projectId = await CreateProject(masterId);
        var characterId = await AddCharacter(masterId, projectId);
        _ = await AddClaim(a, characterId);
        var declined = await AddClaim(b, characterId);
        await factory.Services.RunAsAsync(b, sp =>
            sp.GetRequiredService<IClaimService>().DeclineByPlayer(declined, "Передумал"));

        var preview = await GetPreview(a, b);

        preview.CanMerge.ShouldBeTrue();
    }

    [Fact]
    public async Task OnHoldClaimOnSameCharacter_NoConflict()
    {
        var (a, b) = await CreateUsers();
        var masterId = await CreateUser();
        var projectId = await CreateProject(masterId);
        var characterId = await AddCharacter(masterId, projectId);
        _ = await AddClaim(a, characterId);
        var onHold = await AddClaim(b, characterId);
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IClaimService>().OnHoldByMaster(onHold, "Подождём"));

        var preview = await GetPreview(a, b);

        preview.CanMerge.ShouldBeTrue();
    }

    [Theory]
    // Строгий проект: утверждённая у одного и любая активная у другого — конфликт.
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, true, true)]
    // Две активные без утверждённой — у одного игрока так бывает и в строгом проекте.
    [InlineData(true, false, false, false)]
    // Нестрогий проект: несколько утверждённых персонажей разрешены.
    [InlineData(false, true, true, false)]
    public async Task ClaimsInSingleCharacterProject(bool strictlyOneCharacter, bool approveA, bool approveB, bool expectConflict)
    {
        var (a, b) = await CreateUsers();
        var masterId = await CreateUser();
        var projectId = await CreateProject(masterId, strictlyOneCharacter);
        var claimA = await AddClaim(a, await AddCharacter(masterId, projectId));
        var claimB = await AddClaim(b, await AddCharacter(masterId, projectId));
        foreach (var claimId in new[] { approveA ? claimA : null, approveB ? claimB : null }.OfType<ClaimIdentification>())
        {
            await factory.Services.RunAsAsync(masterId, sp =>
                sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));
        }

        var preview = await GetPreview(a, b);

        preview.Conflicts.ShouldBe(expectConflict ? [new UserMergeConflict.ApprovedClaimInSingleCharacterProject(projectId)] : []);
    }

    [Fact]
    public async Task FormerMaster_StillConflict()
    {
        // Снятый мастер остаётся строкой ProjectAcls (ADR019) — уникальный индекс всё равно нарушится.
        var (a, b) = await CreateUsers();
        var projectId = await CreateProject(a);
        await factory.Services.RunAsAsync(a, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = b,
                Role = new("Мастер"),
                Permissions = [],
            }));
        await factory.Services.RunAsAsync(a, sp =>
            sp.GetRequiredService<IProjectAccessService>().RemoveAccess(projectId, b, newResponsibleMasterId: null));

        var preview = await GetPreview(a, b);

        preview.Conflicts.ShouldBe([new UserMergeConflict.BothMasters(projectId)]);
    }

    [Fact]
    public async Task BothHaveCash_Conflict()
    {
        var (a, b) = await CreateUsers();
        var projectId = await CreateProject(await CreateUser());
        await WithDb(db =>
        {
            foreach (var userId in new[] { a, b })
            {
                _ = db.Set<PaymentType>().Add(new PaymentType
                {
                    ProjectId = projectId.Value,
                    UserId = userId.Value,
                    Name = "Касса",
                    TypeKind = PaymentTypeKind.Cash,
                    IsActive = userId == a, // неактивная касса — тоже конфликт
                });
            }
        });

        var preview = await GetPreview(a, b);

        preview.Conflicts.ShouldBe([new UserMergeConflict.BothHaveCashPaymentType(projectId)]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MoneyTransferBetween_Conflict(bool fromSecondary)
    {
        var (a, b) = await CreateUsers();
        var (sender, receiver) = fromSecondary ? (b, a) : (a, b);
        var projectId = await CreateProject(await CreateUser());
        await WithDb(db => db.Set<MoneyTransfer>().Add(new MoneyTransfer
        {
            ProjectId = projectId.Value,
            SenderId = sender.Value,
            ReceiverId = receiver.Value,
            CreatedById = sender.Value,
            ChangedById = receiver.Value,
            Amount = 100,
            ResultState = MoneyTransferState.Approved,
            Created = DateTimeOffset.UtcNow,
            Changed = DateTimeOffset.UtcNow,
            OperationDate = DateTimeOffset.UtcNow,
            TransferText = new TransferText(),
        }));

        var preview = await GetPreview(a, b);

        preview.Conflicts.ShouldBe([new UserMergeConflict.MoneyTransfersBetween(projectId)]);
    }

    [Fact]
    public async Task BothCaptains_Conflict()
    {
        var (a, b) = await CreateUsers();
        var projectId = await CreateProject(await CreateUser());
        var groupId = 0;
        await WithDb(db =>
        {
            groupId = db.Set<CharacterGroup>().Single(g => g.ProjectId == projectId.Value && g.IsRoot).CharacterGroupId;
            foreach (var userId in new[] { a, b })
            {
                _ = db.Set<CaptainAccessRuleEntity>().Add(new CaptainAccessRuleEntity
                {
                    CaptainAccessRuleEntityId = 0,
                    ProjectId = projectId.Value,
                    CharacterGroupId = groupId,
                    CaptainUserId = userId.Value,
                    CanApprove = false,
                });
            }
        });

        var preview = await GetPreview(a, b);

        preview.Conflicts.ShouldBe([new UserMergeConflict.BothCaptains(new(projectId, groupId))]);
    }

    [Fact]
    public async Task SameLoginProvider_DroppedNotConflict()
    {
        var (a, b) = await CreateUsers();
        await WithDb(db =>
        {
            _ = db.Set<UserExternalLogin>().Add(new UserExternalLogin { UserId = a.Value, Provider = UserExternalLogin.VkProvider, Key = $"a{a.Value}" });
            _ = db.Set<UserExternalLogin>().Add(new UserExternalLogin { UserId = b.Value, Provider = UserExternalLogin.VkProvider, Key = $"b{b.Value}" });
            _ = db.Set<UserExternalLogin>().Add(new UserExternalLogin { UserId = b.Value, Provider = UserExternalLogin.TelegramProvider, Key = $"b{b.Value}" });
        });

        var preview = await GetPreview(a, b);

        preview.CanMerge.ShouldBeTrue();
        preview.DroppedLoginProviders.ShouldBe([UserExternalLogin.VkProvider]);
    }

    private async Task<UserMergePreview> GetPreview(UserIdentification a, UserIdentification b)
        => await factory.Services.RunAsAsync(a, sp => sp.GetRequiredService<IUserMergeService>().GetPreview(a, b), isAdmin: true);

    private async Task WithDb(Action<MyDbContext> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        action(db);
        _ = await db.SaveChangesAsync();
    }

    private async Task<(UserIdentification A, UserIdentification B)> CreateUsers()
        => (await CreateUser(), await CreateUser());

    private async Task<UserIdentification> CreateUser()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
    }

    private async Task<ProjectIdentification> CreateProject(UserIdentification ownerId, bool strictlyOneCharacter = false)
    {
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
        }
        await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with
                {
                    AutoAcceptClaims = false,
                    IsAcceptingClaims = true,
                    StrictlyOneCharacter = strictlyOneCharacter,
                });
        });
        return projectId;
    }

    private Task<CharacterIdentification> AddCharacter(UserIdentification masterId, ProjectIdentification projectId)
        => factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

    private Task<ClaimIdentification> AddClaim(UserIdentification playerId, CharacterIdentification characterId)
        => factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(characterId.ProjectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });
}
