using System.Data.Entity;
using System.Data.SqlClient;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Слияние пользователей (ADR024): всё, что ссылалось на поглощаемого, переходит к основному,
/// а сам он удаляется.
/// </summary>
public class UserMergeScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Новый FK на <c>Users</c> без правила слияния: <c>DELETE</c> поглощаемого упадёт в проде,
    /// а при каскаде — молча снесёт его данные. Поэтому список правил сверяется с реальной схемой.
    /// </summary>
    [Fact]
    public async Task EveryForeignKeyToUsers_HasMergeRule()
    {
        var actual = new List<string>();
        using (var connection = new SqlConnection(factory.ConnectionString))
        {
            await connection.OpenAsync();
            using var command = new SqlCommand("""
                SELECT OBJECT_NAME(fk.parent_object_id) + '.' + COL_NAME(fkc.parent_object_id, fkc.parent_column_id)
                FROM sys.foreign_keys fk
                JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                WHERE fk.referenced_object_id = OBJECT_ID('dbo.Users')
                """, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                actual.Add(reader.GetString(0));
            }
        }

        actual.ShouldBe(
            UserReferences.All.Select(r => r.ToString()),
            ignoreOrder: true,
            "Список FK на Users разошёлся с UserReferences: добавь правило слияния для новой ссылки (ADR024).");
    }

    [Fact]
    public async Task Merge_MovesEverythingAndDeletesSecondary()
    {
        var a = await CreateUser();
        var b = await CreateUser();
        var masterId = await CreateUser();
        var projectId = await CreateProject(masterId);
        var characterId = await AddCharacter(masterId, projectId);
        var claimId = await AddClaim(b, characterId); // заявка B и комментарий B
        var bMasterProject = await CreateProject(b);   // B — владелец своего проекта
        await factory.Services.RunAsAsync(masterId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = a,
                Role = new("Мастер"),
                Permissions = [],
            }));

        await WithDb(db =>
        {
            var rootGroupId = db.Set<CharacterGroup>().Single(g => g.ProjectId == projectId.Value && g.IsRoot).CharacterGroupId;
            _ = db.Set<UserExternalLogin>().Add(new UserExternalLogin { UserId = a.Value, Provider = UserExternalLogin.VkProvider, Key = $"vk-a{a.Value}" });
            _ = db.Set<UserExternalLogin>().Add(new UserExternalLogin { UserId = b.Value, Provider = UserExternalLogin.VkProvider, Key = $"vk-b{b.Value}" });
            _ = db.Set<UserExternalLogin>().Add(new UserExternalLogin { UserId = b.Value, Provider = UserExternalLogin.TelegramProvider, Key = $"tg-b{b.Value}" });
            foreach (var userId in new[] { a, b })
            {
                _ = db.Set<UserSubscription>().Add(new UserSubscription { UserId = userId.Value, ProjectId = projectId.Value, CharacterGroupId = rootGroupId, Comments = true });
            }
            _ = db.Set<UserSubscription>().Add(new UserSubscription { UserId = b.Value, ProjectId = projectId.Value, CharacterId = characterId.CharacterId, Comments = true });

            var bUser = db.Set<User>().Include(u => u.Extra).Single(u => u.UserId == b.Value);
            bUser.SurName = "Поглощаемый";
            bUser.Extra.Telegram = "secondary_tg";
        });

        await Merge(a, b);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        db.Set<User>().Any(u => u.UserId == b.Value).ShouldBeFalse();
        db.Set<UserAuthDetails>().Any(x => x.UserId == b.Value).ShouldBeFalse();
        db.Set<UserExtra>().Any(x => x.UserId == b.Value).ShouldBeFalse();

        var claim = db.ClaimSet.Single(c => c.ClaimId == claimId.ClaimId);
        claim.PlayerUserId.ShouldBe(a.Value);
        db.Set<Comment>().Any(c => c.AuthorUserId == b.Value).ShouldBeFalse();
        db.Set<Comment>().Any(c => c.AuthorUserId == a.Value).ShouldBeTrue();
        db.Set<ProjectAcl>().Single(acl => acl.ProjectId == bMasterProject.Value).UserId.ShouldBe(a.Value);

        db.Set<UserExternalLogin>().Where(l => l.UserId == a.Value).Select(l => l.Key).ToList()
            .ShouldBe([$"vk-a{a.Value}", $"tg-b{b.Value}"], ignoreOrder: true);
        db.Set<UserSubscription>().Count(s => s.UserId == a.Value).ShouldBe(2);

        var aUser = db.Set<User>().Include(u => u.Extra).Single(u => u.UserId == a.Value);
        aUser.SurName.ShouldBe("Поглощаемый");
        aUser.Extra.Telegram.ShouldBe("secondary_tg");

        // Агрегаты после слияния читаются: никаких висячих ссылок.
        var projectInfo = await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(bMasterProject);
        projectInfo.HasMasterAccess(a).ShouldBeTrue();
        (await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetUserInfo(a)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Merge_ProfileRowsAvatarsAndSubscriptionFlags()
    {
        var a = await CreateUser();
        var b = await CreateUser();
        var projectId = await CreateProject(await CreateUser());
        var avatarId = 0;
        await WithDb(db =>
        {
            // У основного нет ни Extra, ни Allrpg — строки создаются слиянием.
            db.Set<UserExtra>().RemoveRange(db.Set<UserExtra>().Where(x => x.UserId == a.Value));
            db.Set<AllrpgUserDetails>().RemoveRange(db.Set<AllrpgUserDetails>().Where(x => x.UserId == a.Value));
            db.Set<AllrpgUserDetails>().RemoveRange(db.Set<AllrpgUserDetails>().Where(x => x.UserId == b.Value));
            _ = db.Set<AllrpgUserDetails>().Add(new AllrpgUserDetails { UserId = b.Value, Sid = 100500, JsonProfile = "{}" });

            var rootGroupId = db.Set<CharacterGroup>().Single(g => g.ProjectId == projectId.Value && g.IsRoot).CharacterGroupId;
            _ = db.Set<UserSubscription>().Add(new UserSubscription { UserId = a.Value, ProjectId = projectId.Value, CharacterGroupId = rootGroupId, Comments = true });
            _ = db.Set<UserSubscription>().Add(new UserSubscription { UserId = b.Value, ProjectId = projectId.Value, CharacterGroupId = rootGroupId, MoneyOperation = true });
        });
        await WithDb(db =>
        {
            var avatar = db.Set<UserAvatar>().Add(new UserAvatar
            {
                UserId = b.Value,
                AvatarSource = AvatarSource.GrAvatar,
                OriginalUri = "https://example.com/b.png",
                IsActive = true,
            });
            _ = db.SaveChanges();
            avatarId = avatar.UserAvatarId;
            db.Set<User>().Single(u => u.UserId == b.Value).SelectedAvatarId = avatarId;
        });

        await Merge(a, b);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var aUser = db.Set<User>().Include(u => u.Extra).Include(u => u.Allrpg).Single(u => u.UserId == a.Value);
        aUser.Extra.ShouldNotBeNull();
        aUser.Allrpg.ShouldNotBeNull().Sid.ShouldBe(100500);
        db.Set<AllrpgUserDetails>().Any(x => x.UserId == b.Value).ShouldBeFalse();
        aUser.SelectedAvatarId.ShouldBe(avatarId);
        db.Set<UserAvatar>().Single(x => x.UserAvatarId == avatarId).UserId.ShouldBe(a.Value);

        var subscription = db.Set<UserSubscription>().Single(s => s.UserId == a.Value && s.ProjectId == projectId.Value);
        subscription.Comments.ShouldBeTrue();
        subscription.MoneyOperation.ShouldBeTrue();
    }

    [Fact]
    public async Task Merge_WithConflict_Refused_NothingChanged()
    {
        var a = await CreateUser();
        var b = await CreateUser();
        var projectId = await CreateProject(a);
        await factory.Services.RunAsAsync(a, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = b,
                Role = new("Мастер"),
                Permissions = [],
            }));

        var exception = await Should.ThrowAsync<UserMergeConflictException>(Merge(a, b));

        exception.Preview.Conflicts.ShouldBe([new UserMergeConflict.BothMasters(projectId)]);
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<User>().Any(u => u.UserId == b.Value).ShouldBeTrue();
    }

    [Fact]
    public async Task Merge_NotAdmin_Forbidden()
    {
        var a = await CreateUser();
        var b = await CreateUser();

        _ = await Should.ThrowAsync<MustBeAdminException>(
            factory.Services.RunAsAsync(a, sp => sp.GetRequiredService<IUserMergeService>().Merge(a, b)));
    }

    private Task Merge(UserIdentification a, UserIdentification b)
        => factory.Services.RunAsAsync(a, sp => sp.GetRequiredService<IUserMergeService>().Merge(a, b), isAdmin: true);

    private async Task WithDb(Action<MyDbContext> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        action(db);
        _ = await db.SaveChangesAsync();
    }

    private async Task<UserIdentification> CreateUser()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
    }

    private async Task<ProjectIdentification> CreateProject(UserIdentification ownerId)
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
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
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
