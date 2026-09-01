using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AdvertisementLogScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task RecordAdvertisement_MarksRoleAsSentOnlyForItsOwnSchedule()
    {
        // 1. Мастер и проект, открытый для заявок (публичный по умолчанию)
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект для рекламы");
        }

        var scheduleId = new AdvertisementScheduleIdentification(1);
        var otherScheduleId = new AdvertisementScheduleIdentification(2);

        // 2. Открываем приём заявок и заводим горячую роль
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

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var logRepository = sp.GetRequiredService<IAdvertisementLogRepository>();

            // 3. До отправки — роль не рекламировалась ни разу
            var before = await logRepository.GetHotCharactersAdvertisementInfo(scheduleId, projectId);
            var beforeInfo = before.Single(c => c.Character.CharacterId == characterId);
            beforeInfo.AdvertisementCount.ShouldBe(0);
            beforeInfo.AlreadySentForSchedule.ShouldBeFalse();

            // 4. Записываем отправку по scheduleId
            await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
                scheduleId, AdvertisementMethod.SingleHotRole, projectId, characterId, AdvertisementLogStatus.Sent, DateTimeOffset.UtcNow));

            // 5. Для того же расписания — anti-repeat сработал, счётчик вырос
            var afterSameSchedule = await logRepository.GetHotCharactersAdvertisementInfo(scheduleId, projectId);
            var afterSameInfo = afterSameSchedule.Single(c => c.Character.CharacterId == characterId);
            afterSameInfo.AdvertisementCount.ShouldBe(1);
            afterSameInfo.AlreadySentForSchedule.ShouldBeTrue();

            // 6. Для другого расписания — anti-repeat не блокирует, но счётчик общий (по всем расписаниям)
            var afterOtherSchedule = await logRepository.GetHotCharactersAdvertisementInfo(otherScheduleId, projectId);
            var afterOtherInfo = afterOtherSchedule.Single(c => c.Character.CharacterId == characterId);
            afterOtherInfo.AdvertisementCount.ShouldBe(1);
            afterOtherInfo.AlreadySentForSchedule.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task WasProjectAdvertisedAmongLastN_ExpiresAfterEnoughOtherAdvertisements()
    {
        UserIdentification masterId;
        ProjectIdentification projectA;
        ProjectIdentification projectB;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectA = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект A для кулдауна");
            projectB = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId, "Проект B для кулдауна");
        }

        var scheduleId = new AdvertisementScheduleIdentification(1);

        async Task<CharacterIdentification> AddHotCharacter(ProjectIdentification projectId) =>
            await factory.Services.RunAsAsync(masterId, async sp =>
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

        var characterA = await AddHotCharacter(projectA);
        var characterB = await AddHotCharacter(projectB);

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var logRepository = sp.GetRequiredService<IAdvertisementLogRepository>();

            // 1. Ни один из проектов ещё не рекламировался — кулдаун не действует
            (await logRepository.WasProjectAdvertisedAmongLastN(scheduleId, projectA, 3)).ShouldBeFalse();

            // 2. Рекламируем проект A — он на кулдауне (последняя реклама — его собственная)
            await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
                scheduleId, AdvertisementMethod.SingleHotRole, projectA, characterA, AdvertisementLogStatus.Sent, DateTimeOffset.UtcNow));
            (await logRepository.WasProjectAdvertisedAmongLastN(scheduleId, projectA, 3)).ShouldBeTrue();

            // 3. Ещё две рекламы проекта B — с прошлого раза для A было 2 чужих рекламы, кулдаун ещё действует
            for (var i = 0; i < 2; i++)
            {
                await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
                    scheduleId, AdvertisementMethod.SingleHotRole, projectB, characterB, AdvertisementLogStatus.Sent, DateTimeOffset.UtcNow));
            }
            (await logRepository.WasProjectAdvertisedAmongLastN(scheduleId, projectA, 3)).ShouldBeTrue();

            // 4. Третья реклама проекта B — с прошлого раза для A было уже 3 чужих рекламы, кулдаун снят
            await logRepository.RecordAdvertisement(new AdvertisementLogEntryInfo(
                scheduleId, AdvertisementMethod.SingleHotRole, projectB, characterB, AdvertisementLogStatus.Sent, DateTimeOffset.UtcNow));
            (await logRepository.WasProjectAdvertisedAmongLastN(scheduleId, projectA, 3)).ShouldBeFalse();
            (await logRepository.WasProjectAdvertisedAmongLastN(scheduleId, projectB, 3)).ShouldBeTrue();
        });
    }
}
