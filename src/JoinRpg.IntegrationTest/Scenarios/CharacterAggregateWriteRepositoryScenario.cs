using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Инварианты write-хэндла агрегата персонажа (ADR014): доменные снимки, которые он отдаёт,
/// должны быть согласованы между собой по ссылке, а не только по значению.
/// </summary>
public class CharacterAggregateWriteRepositoryScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task LoadClaimForUpdate_ReturnsConsistentSnapshots()
    {
        // 1. Мастер, проект, игрок
        UserIdentification masterId;
        ProjectIdentification projectId;
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        // 2. Мастер открывает приём заявок и создаёт персонажа
        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var characterService = sp.GetRequiredService<ICharacterService>();
            return await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        // 3. Игрок подаёт заявку
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await claimService.AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        // 3а. Мастер утверждает заявку — так в БД появляется непустая MasterAcceptedDate,
        // на которой видно, что статусные даты действительно доезжают до снимка.
        await factory.Services.RunAsAsync(masterId, async sp =>
            await sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));

        // 4. Грузим write-хэндл и проверяем инварианты
        using var checkScope = factory.Services.CreateScope();
        var unitOfWork = checkScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Хэндл обязан привезти весь граф агрегата сразу: дальше операции ходят по снимкам и
        // трекаемым сущностям, и каждая недогруженная связь обернулась бы ленивой загрузкой
        // посреди мутации. Счётчик тот же, что сторожит страницы (#4914), только блок здесь
        // задаётся руками — HTTP-запроса нет.
        using var lazyLoads = LazyLoadCounter.BeginScope();

        var handle = await unitOfWork.GetCharacterAggregateWriteRepository()
            .LoadClaimForUpdate(claimId, masterId);

        // ProjectInfo внутри CharacterInfo — ровно тот экземпляр, которым владеет хэндл
        // (этого требует конструктор CharacterInfo, ADR013).
        ReferenceEquals(handle.CharacterInfo.ProjectInfo, handle.ProjectInfo).ShouldBeTrue();

        // ClaimInfo — тот же экземпляр, что лежит в CharacterInfo.Claims, а не его копия.
        var claimInfoFromCharacter = handle.CharacterInfo.Claims.Single(c => c.ClaimId == claimId);
        ReferenceEquals(handle.ClaimInfo, claimInfoFromCharacter).ShouldBeTrue();

        // Трекаемые сущности соответствуют снимкам.
        handle.Claim.ClaimId.ShouldBe(claimId.ClaimId);
        handle.Character.CharacterId.ShouldBe(characterId.CharacterId);
        handle.CharacterInfo.Id.ShouldBe(characterId);
        handle.Project.ProjectId.ShouldBe(projectId.Value);
        handle.Initiator.UserId.ShouldBe(masterId.Value);

        // Статусные даты заявки доезжают из БД в снимок ровно те же, что лежат в сущности.
        handle.Claim.MasterAcceptedDate.ShouldNotBeNull();
        handle.ClaimInfo.MasterAcceptedDate.ShouldBe(handle.Claim.MasterAcceptedDate);
        handle.ClaimInfo.MasterDeclinedDate.ShouldBe(handle.Claim.MasterDeclinedDate);
        handle.ClaimInfo.PlayerDeclinedDate.ShouldBe(handle.Claim.PlayerDeclinedDate);
        handle.ClaimInfo.CheckInDate.ShouldBe(handle.Claim.CheckInDate);

        // Ни одно обращение выше не полезло в базу за недостающей связью.
        lazyLoads.Count.ShouldBe(
            0,
            $"Загрузка хэндла и обход его снимков дали {lazyLoads.Count} ленивых загрузок — "
            + "граф агрегата недогружен, см. #4670");
    }
}
