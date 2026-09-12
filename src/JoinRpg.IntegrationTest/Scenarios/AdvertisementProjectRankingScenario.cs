using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Projects;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AdvertisementProjectRankingScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    // Регрессия: GetPublicProjectsOpenForHotRoleAdvertisement() хардкодил AdvertisementCount: 0
    // (см. ADR010 §4), из-за чего формула ранжирования AdvertisementGameRanking всегда получала
    // одинаковый знаменатель и фактически ранжировала только по ActiveClaimsCount. Проверяем, что
    // счётчик считается по реальным записям AdvertisementLog (только Sent, Failed не учитывается).
    [Fact]
    public async Task GetPublicProjectsOpenForHotRoleAdvertisement_CountsOnlySentAdvertisements()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект для проверки подсчёта рекламы");
        }

        var scheduleId = new AdvertisementScheduleIdentification(1);

        // 1. Открываем приём заявок и заводим горячую роль — проект должен стать кандидатом
        // GetPublicProjectsOpenForHotRoleAdvertisement().
        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { IsAcceptingClaims = true });

            var characterService = sp.GetRequiredService<ICharacterService>();
            return await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: true, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        // 2. Проект создаётся с ShouldNotBeOnKogdaIgra — снимаем флаг и заводим будущую активную
        // привязку к КогдаИгра напрямую в БД (по образцу KogdaIgraMissingGamesScenario), иначе
        // ProjectPredicates.HasFutureKogdaIgraGame() исключит проект из кандидатов.
        using (var scope = factory.Services.CreateScope())
        {
            var myDb = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var project = await myDb.Set<Project>()
                .Include(p => p.KogdaIgraGames)
                .Include(p => p.Details)
                .SingleAsync(p => p.ProjectId == projectId.Value);

            project.Details.DisableKogdaIgraMapping = false;

            project.KogdaIgraGames.Add(new KogdaIgraGame
            {
                KogdaIgraGameId = projectId.Value + 1_000_000,
                Name = "Тестовая будущая игра КогдаИгра",
                JsonGameData = "{}",
                Active = true,
                Begin = DateTime.UtcNow.AddDays(30),
                End = DateTime.UtcNow.AddDays(33),
                UpdateRequestedAt = DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow,
            });
            await myDb.SaveChangesAsync();
        }

        // 3. Записываем в лог рекламы две отправленные (Sent) и одну неудачную (Failed) записи.
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var logRepository = sp.GetRequiredService<IAdvertisementLogRepository>();

            await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
                scheduleId, AdvertisementMethod.SingleHotRole, projectId, characterId, AdvertisementLogStatus.Sent, DateTimeOffset.UtcNow));
            await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
                scheduleId, AdvertisementMethod.SingleHotRole, projectId, characterId, AdvertisementLogStatus.Sent, DateTimeOffset.UtcNow));
            await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
                scheduleId, AdvertisementMethod.SingleHotRole, projectId, characterId, AdvertisementLogStatus.Failed, DateTimeOffset.UtcNow));
        });

        // 4. AdvertisementCount кандидата должен учитывать только две Sent-записи, Failed — не в счёт.
        using (var scope = factory.Services.CreateScope())
        {
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
            var candidates = await projectRepository.GetPublicProjectsOpenForHotRoleAdvertisement();

            var candidate = candidates.Single(c => c.ProjectId == projectId);
            candidate.AdvertisementCount.ShouldBe(2);
        }
    }
}
