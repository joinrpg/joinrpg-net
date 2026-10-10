using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectAccess;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Бэкфилл бывших мастеров (ADR019, §6) на настоящей базе: SQL-запрос кандидатов и сама джоба под роботом.
/// </summary>
public class FormerMastersBackfillScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task MasterRemovedBeforeSoftDelete_IsRestoredAsFormerMaster()
    {
        UserIdentification ownerId, formerMasterId, playerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            ownerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
            formerMasterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            // Запись ACL в чужом проекте не должна прятать его из кандидатов этого проекта.
            _ = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, formerMasterId, "Свой проект бывшего мастера");
        }

        var characterId = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = formerMasterId,
                Role = "Мастер",
                Permissions = [Permission.CanManageClaims],
            });
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        // Мастер пишет в заявку (IsCommentByPlayer = false), игрок отвечает (true).
        await factory.Services.RunAsAsync(formerMasterId, sp =>
            sp.GetRequiredService<IClaimService>().AddComment(claimId, parentCommentId: null, isVisibleToPlayer: true, "Комментарий мастера"));
        await factory.Services.RunAsAsync(playerId, sp =>
            sp.GetRequiredService<IClaimService>().AddComment(claimId, parentCommentId: null, isVisibleToPlayer: true, "Ответ игрока"));

        // Так снимали мастеров до ADR019 — физическим удалением строки.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var set = db.Set<ProjectAcl>();
            _ = set.Remove(set.Single(a => a.ProjectId == projectId.Value && a.UserId == formerMasterId.Value));
            _ = await db.SaveChangesAsync();
        }

        var candidates = await GetCandidatesOfProject(projectId);
        candidates.ShouldBe([formerMasterId]); // не владелец (у него есть ACL) и не игрок (его комментарий — игрока)

        await RunJob();

        using (var scope = factory.Services.CreateScope())
        {
            var acl = scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<ProjectAcl>()
                .Single(a => a.ProjectId == projectId.Value && a.UserId == formerMasterId.Value);
            acl.Status.ShouldBe(ProjectAclStatus.Removed);
            acl.Role.ShouldBe("Мастер");
            acl.CanManageClaims.ShouldBeFalse();

            var projectInfo = await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            projectInfo.HasMasterAccess(formerMasterId).ShouldBeFalse();
            projectInfo.FormerMasters.ShouldContain(m => m.UserId == formerMasterId);
        }

        // Идемпотентность: после прогона кандидатов в проекте не остаётся, второй прогон ничего не ломает.
        (await GetCandidatesOfProject(projectId)).ShouldBeEmpty();
        await RunJob();

        // Бывшего мастера можно вернуть обычной выдачей доступа: реактивируется та же строка (ADR019, PR 3),
        // уникальный индекс (ProjectId, UserId) не мешает.
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
            {
                ProjectId = projectId,
                UserId = formerMasterId,
                Role = "Мастер по боёвке",
                Permissions = [Permission.CanManageClaims],
            }));
        using (var scope = factory.Services.CreateScope())
        {
            var projectInfo = await scope.ServiceProvider.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            projectInfo.HasMasterAccess(formerMasterId, Permission.CanManageClaims).ShouldBeTrue();
            projectInfo.FormerMasters.ShouldNotContain(m => m.UserId == formerMasterId);
        }
    }

    [Fact]
    public async Task FinanceTraces_AreCandidates_OnlinePaymentTypeIsNot()
    {
        // Следы мастерства в деньгах (касса, переводы) переживают физическое удаление ACL так же, как комментарии.
        UserIdentification ownerId, cashierId, senderId, onlineOwnerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            ownerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
            cashierId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            senderId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            onlineOwnerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);

            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            _ = db.Set<PaymentType>().Add(new PaymentType
            {
                ProjectId = projectId.Value,
                UserId = cashierId.Value,
                Name = "Касса бывшего мастера",
                TypeKind = PaymentTypeKind.Cash,
                IsActive = true,
            });
            // Онлайн-оплата оформлена на служебного пользователя — он не мастер.
            _ = db.Set<PaymentType>().Add(new PaymentType
            {
                ProjectId = projectId.Value,
                UserId = onlineOwnerId.Value,
                Name = "Онлайн",
                TypeKind = PaymentTypeKind.Online,
                IsActive = true,
            });
            _ = db.Set<MoneyTransfer>().Add(new MoneyTransfer
            {
                ProjectId = projectId.Value,
                SenderId = senderId.Value,
                ReceiverId = ownerId.Value,
                CreatedById = senderId.Value,
                ChangedById = ownerId.Value,
                Amount = 100,
                ResultState = MoneyTransferState.Approved,
                Created = DateTimeOffset.UtcNow,
                Changed = DateTimeOffset.UtcNow,
                OperationDate = DateTimeOffset.UtcNow,
                TransferText = new TransferText(),
            });
            _ = await db.SaveChangesAsync();
        }

        var candidates = await GetCandidatesOfProject(projectId);

        candidates.ShouldBe([cashierId, senderId], ignoreOrder: true);
    }

    [Fact]
    public async Task CharacterAndGroupAuthors_AreCandidates_PlayerFromSlotIsNot()
    {
        // Мастер мог только заводить персонажей и группы — без комментариев и денег. Но персонажа штатно
        // создаёт и игрок: при принятии заявки на слот автор нового персонажа — он.
        UserIdentification ownerId, characterAuthorId, groupAuthorId, playerId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            ownerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, ownerId);
            characterAuthorId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            groupAuthorId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        var slotId = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            foreach (var masterId in new[] { characterAuthorId, groupAuthorId })
            {
                await sp.GetRequiredService<IProjectAccessService>().GrantAccess(new GrantAccessRequest
                {
                    ProjectId = projectId,
                    UserId = masterId,
                    Role = "Мастер",
                    Permissions = [Permission.CanEditRoles],
                });
            }
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Slot, IsHot: false, SlotLimit: 3, SlotName: "Стражник", CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        _ = await factory.Services.RunAsAsync(characterAuthorId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.NonPlayer, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        _ = await factory.Services.RunAsAsync(groupAuthorId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<ICharacterGroupService>().AddCharacterGroup(
                projectId,
                "Группа бывшего мастера",
                isPublic: true,
                parentCharacterGroupIds: [projectInfo.GroupTree.RootGroupId],
                description: "");
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                slotId, "Хочу в стражники", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принят"));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            // Предусловие: персонаж из слота записан на игрока.
            db.ClaimSet.Where(c => c.ClaimId == claimId.ClaimId).Select(c => c.Character.CreatedById).Single()
                .ShouldBe(playerId.Value);

            // Так снимали мастеров до ADR019 — физическим удалением строки.
            var set = db.Set<ProjectAcl>();
            _ = set.RemoveRange(set.Where(a => a.ProjectId == projectId.Value
                && (a.UserId == characterAuthorId.Value || a.UserId == groupAuthorId.Value)));
            _ = await db.SaveChangesAsync();
        }

        var candidates = await GetCandidatesOfProject(projectId);

        candidates.ShouldBe([characterAuthorId, groupAuthorId], ignoreOrder: true);
    }

    private async Task<UserIdentification[]> GetCandidatesOfProject(ProjectIdentification projectId)
    {
        using var scope = factory.Services.CreateScope();
        var all = await scope.ServiceProvider.GetRequiredService<IProjectRepository>().GetFormerMasterCandidates();
        return [.. all.Where(c => c.ProjectId == projectId).Select(c => c.UserId)];
    }

    // Как админская страница «Джобы»: через IJobRunner, под роботом.
    private async Task RunJob()
    {
        using var scope = factory.Services.CreateScope();
        var runner = scope.ServiceProvider.GetServices<IJobRunner>().Single(r => r.Name == "FormerMastersBackfillJob");
        await runner.RunJob(scope, CancellationToken.None);
    }
}
