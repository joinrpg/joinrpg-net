using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Accommodation;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Список «кого пригласить в номер» не должен делать ленивых загрузок (#4964).
/// </summary>
/// <remarks>
/// <c>GetInviteTargets</c> подписывает каждого кандидата именем персонажа, а репозиторий отдавал
/// заявки без <c>Include</c> персонажа — EF6 догружал <c>[dbo].[Characters]</c> по одному на
/// кандидата. На проде это давало до 54 запросов за одно открытие страницы заявки, и счёт растёт
/// линейно с числом утверждённых заявок в проекте.
///
/// Тот же метод вызывается при пререндере <c>AccommodationInviteControl</c> на странице заявки
/// (<c>WebAssemblyPrerendered</c> + серверная регистрация <c>IAccommodationInviteClient</c>),
/// поэтому фикс закрывает и N+1 на <c>GET /{projectId}/claim/{claimId}/edit</c> (#4960).
///
/// Проверку делает не сам тест, а <c>LazyLoadAssertingHandler</c> на клиенте фабрики: маршрута
/// нет в <c>lazy-loads-baseline.json</c>, значит ленивых загрузок должно быть ноль
/// (см. docs/lazy-loads-baseline.md). Поэтому здесь важно, чтобы список кандидатов реально
/// собрался — иначе N+1 не воспроизведётся, и тест станет пустым.
/// </remarks>
public class AccommodationInviteTargetsLazyLoadsScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>Игроков с утверждёнными заявками, кроме приглашающего.</summary>
    private const int OtherPlayers = 3;

    /// <summary>Мест в номере: хватает на приглашающего и всех остальных.</summary>
    private const int RoomCapacity = OtherPlayers + 1;

    [Fact]
    public async Task GetInviteTargets_WithCandidatesInProject_DoesNotLazyLoad()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        var players = new List<(UserIdentification UserId, string Email)>(OtherPlayers + 1);
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с поселением");
            for (var i = 0; i <= OtherPlayers; i++)
            {
                players.Add(await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider));
            }
        }

        var setup = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var accommodationService = sp.GetRequiredService<IAccommodationService>();
            var characterService = sp.GetRequiredService<ICharacterService>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();

            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetAccommodationSettings(projectId, enableAccommodation: true);

            var roomType = await accommodationService.SaveRoomTypeAsync(new ProjectAccommodationType
            {
                ProjectId = projectId.Value,
                Name = "Двухэтажная кровать",
                Cost = 1000,
                Capacity = RoomCapacity,
                IsPlayerSelectable = true,
                Description = new MarkdownDbValue(null),
            }) ?? throw new InvalidOperationException("Не удалось создать тип проживания");

            var rootGroupId = projectInfo.GroupTree.RootGroupId;
            var nameFieldId = (projectInfo.CharacterNameField
                    ?? throw new InvalidOperationException("В проекте нет поля имени персонажа"))
                .Id.ProjectFieldId;

            var characters = new List<CharacterIdentification>(OtherPlayers + 1);
            var characterNames = new List<string>(OtherPlayers + 1);
            for (var i = 0; i <= OtherPlayers; i++)
            {
                var name = $"Персонаж {i} {Guid.NewGuid().ToString("N")[..6]}";
                characterNames.Add(name);
                characters.Add(await characterService.AddCharacter(
                    new AddCharacterRequest(
                        projectId,
                        ParentCharacterGroupIds: [rootGroupId],
                        new CharacterTypeInfo(
                            CharacterType.Player,
                            IsHot: false,
                            SlotLimit: null,
                            SlotName: null,
                            CharacterVisibility.Public),
                        FieldValues: new FieldLayerContainer(
                            projectInfo,
                            new Dictionary<int, string?> { [nameFieldId] = name }))));
            }

            return (Characters: characters, CharacterNames: characterNames, RoomTypeId: roomType.Id);
        });

        // Кандидатами в соседи считаются только утверждённые заявки.
        var claimIds = new List<ClaimIdentification>(OtherPlayers + 1);
        for (var i = 0; i <= OtherPlayers; i++)
        {
            var characterId = setup.Characters[i];
            var claimId = await factory.Services.RunAsAsync(players[i].UserId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                    characterId,
                    "Хочу жить с кем-нибудь",
                    FieldLayerContainer.Empty(projectInfo),
                    sensitiveDataAllowed: false);
            });
            claimIds.Add(claimId);

            await factory.Services.RunAsAsync(masterId, async sp =>
            {
                await sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято");
                return claimId;
            });
        }

        // Приглашающий выбирает тип проживания — без принятой заявки на проживание
        // список кандидатов пуст и проверять было бы нечего.
        var senderClaimId = claimIds[0];
        await factory.Services.RunAsAsync(players[0].UserId, async sp =>
            await sp.GetRequiredService<IClaimService>().SetAccommodationType(
                projectId.Value, senderClaimId.ClaimId, setup.RoomTypeId));

        // Второй игрок выбирает тот же тип: он попадёт в список как сложившаяся группа,
        // а не как «ещё не выбрал тип проживания» — это другая ветка сборки строки.
        await factory.Services.RunAsAsync(players[1].UserId, async sp =>
            await sp.GetRequiredService<IClaimService>().SetAccommodationType(
                projectId.Value, claimIds[1].ClaimId, setup.RoomTypeId));

        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(), players[0].Email);

        var response = await client.GetAsync(
            $"webapi/AccommodationInvite/GetInviteTargets?claimId={senderClaimId}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var model = await response.Content.ReadFromJsonAsync<AccommodationInviteTargetsViewModel>()
            ?? throw new InvalidOperationException("Сервер не вернул модель");

        // Список должен реально собраться: иначе N+1 не воспроизводится и тест ничего не проверяет.
        model.SenderRequestId.ShouldNotBeNull();
        model.RoomFreeSpace.ShouldBe(RoomCapacity - 1);
        model.Targets.Count.ShouldBe(OtherPlayers);

        // Имена персонажей кандидатов — ровно то, из-за чего догружались Characters.
        var extraSearch = string.Join(" ", model.Targets.Select(target => target.ExtraSearch));
        for (var i = 1; i <= OtherPlayers; i++)
        {
            extraSearch.ShouldContain(
                setup.CharacterNames[i],
                customMessage: $"Кандидат {i} не попал в список, N+1 не воспроизводится");
        }
    }
}
