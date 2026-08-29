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
                FieldValues: null));
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
}
