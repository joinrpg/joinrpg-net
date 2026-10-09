using System.Data.Entity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Удаление типа проживания, у которого есть нерасселённые группы.
/// </summary>
/// <remarks>
/// Нужна настоящая БД: дефект жил в схеме, а не в коде. Удаляя тип, база каскадом удаляла его
/// группы проживания (<c>AccommodationRequests</c>), а <c>Claims.AccommodationRequest_Id</c>
/// ссылается на них без каскада — и удаление падало 500-й на внешнем ключе. Юнит-тест на фейках
/// внешних ключей не видит, он проверяет только, что группы расформировываются.
/// Сид свой: сценарий мутирует данные, а смоук-проект общий и только читается.
/// </remarks>
public class DeleteAccommodationTypeScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Тип с двумя нерасселёнными группами — на двоих и на одного, у одиночки висит неотвеченное
    /// приглашение. Тип удаляется без исключения, группы расформированы, приглашение отклонено.
    /// </summary>
    [Fact]
    public async Task DeleteType_WithUnplacedGroups_DisbandsGroups_AndDeletesType()
    {
        var (ownerId, _) = await CreateUserAsync();

        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для удаления типа проживания");
        }

        await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);

            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetAccommodationSettings(projectId, enableAccommodation: true);
        });

        var typeId = await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                projectId,
                new AccommodationTypeRequest(
                    "Палатка",
                    new MarkdownString("Палатка на троих"),
                    Cost: 0,
                    Capacity: 3,
                    IsPlayerSelectable: true)));

        var claims = await CreateClaimsAsync(ownerId, projectId, count: 3);

        // У каждой заявки — своя одноместная группа этого типа.
        var requestIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            foreach (var claimId in claims)
            {
                await claimService.SetAccommodationType(
                    projectId.Value, claimId.ClaimId, typeId.AccommodationTypeId);
            }

            return await AccommodationTestHelpers.GetAccommodationGroupIdsAsync(sp, claims);
        });

        // Первые двое съезжаются: приглашение и его приём. Третьему приглашение только отправлено.
        var acceptedInviteId = await InviteAsync(ownerId, claims[0], requestIds[0], claims[1]);
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationInviteService>().AcceptAccommodationInvite(acceptedInviteId));
        var pendingInviteId = await InviteAsync(ownerId, claims[0], requestIds[0], claims[2]);

        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().DeleteAccommodationType(typeId));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            var claimIntIds = claims.Select(claim => claim.ClaimId).ToList();

            var groupIds = await db.Set<Claim>()
                .Where(claim => claimIntIds.Contains(claim.ClaimId))
                .Select(claim => claim.AccommodationRequest_Id)
                .ToListAsync();
            groupIds.Count.ShouldBe(claims.Count);
            groupIds.ShouldAllBe(id => id == null, "Заявки должны выйти из групп удалённого типа");

            (await db.Set<AccommodationRequest>().AnyAsync(request => request.ProjectId == projectId.Value))
                .ShouldBeFalse("Опустевшие группы должны быть удалены");

            (await db.Set<ProjectAccommodationType>().AnyAsync(type => type.Id == typeId.AccommodationTypeId))
                .ShouldBeFalse("Тип проживания должен быть удалён");

            var pending = await db.Set<AccommodationInvite>()
                .SingleAsync(invite => invite.Id == pendingInviteId.AccommodationInviteId);
            pending.IsAccepted.ShouldBe(InviteState.Declined);
            pending.ResolveDescription.ShouldBe(ResolveDescription.DeclinedAuto);

            var accepted = await db.Set<AccommodationInvite>()
                .SingleAsync(invite => invite.Id == acceptedInviteId.AccommodationInviteId);
            accepted.IsAccepted.ShouldBe(InviteState.Accepted, "Отвеченное приглашение — история, его не трогаем");
        }

        var metadata = await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId));
        metadata.AccommodationSettings.Types.ShouldBeEmpty();
    }

    /// <summary>Заявки игроков на новых персонажей — по одному игроку на заявку.</summary>
    private async Task<IReadOnlyList<ClaimIdentification>> CreateClaimsAsync(
        UserIdentification ownerId,
        ProjectIdentification projectId,
        int count)
    {
        var characters = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);
            var characterService = sp.GetRequiredService<ICharacterService>();
            var result = new List<CharacterIdentification>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [],
                    new CharacterTypeInfo(
                        CharacterType.Player,
                        IsHot: false,
                        SlotLimit: null,
                        SlotName: null,
                        CharacterVisibility.Public),
                    FieldValues: FieldLayerContainer.Empty(projectInfo))));
            }

            return result;
        });

        var claims = new List<ClaimIdentification>(count);
        foreach (var characterId in characters)
        {
            var (playerId, _) = await CreateUserAsync();
            claims.Add(await factory.Services.RunAsAsync(playerId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                    characterId,
                    "Хочу играть эту роль",
                    FieldLayerContainer.Empty(projectInfo),
                    sensitiveDataAllowed: true);
            }));
        }

        return claims;
    }

    /// <summary>
    /// Приглашение от мастера за заявку-отправителя. Идентификатор сервис наружу не отдаёт —
    /// берём его из базы.
    /// </summary>
    private async Task<AccommodationInviteIdentification> InviteAsync(
        UserIdentification ownerId,
        ClaimIdentification sender,
        AccommodationRequestIdentification senderRequest,
        ClaimIdentification receiver)
    {
        await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationInviteService>().CreateAccommodationInvite(
                sender,
                senderRequest,
                AccommodationGroupIdentification.From(receiver)));

        using var scope = factory.Services.CreateScope();
        var inviteId = await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<AccommodationInvite>()
            .Where(invite => invite.FromClaimId == sender.ClaimId && invite.ToClaimId == receiver.ClaimId)
            .Select(invite => invite.Id)
            .SingleAsync();
        return new AccommodationInviteIdentification(sender.ProjectId, inviteId);
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }
}
