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
/// Мастер поселяет игроков вместе — объединяет группы проживания без приглашения (#5656).
/// </summary>
/// <remarks>
/// Переезд идёт по трекаемым сущностям, загруженным EF6-запросами хэндла агрегата (заявка цели с
/// группой и её составом), и заканчивается удалением опустевшей группы — это стоит проверить на
/// настоящем SQL Server, фейки порядок FK и удаления не ловят. Сид свой: сценарий мутирует данные.
/// </remarks>
public class AccommodationGroupMergeScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Все три вида цели по очереди: к гостю поселяют одиночку без типа; хозяину — гостя заявкой (с ним
    /// переезжает и его новый сосед); затем хозяину — группу четвёртого по её идентификатору.
    /// </summary>
    [Fact]
    public async Task MergeEveryKindOfTarget_MovesEveryoneAndRemovesEmptiedGroups()
    {
        var seed = await SeedAsync();

        using (var lazyLoads = LazyLoadCounter.BeginScope())
        {
            await MergeAsync(seed, seed.Guest, AccommodationGroupIdentification.From(seed.WithoutType));
            await MergeAsync(seed, seed.Host, AccommodationGroupIdentification.From(seed.Guest));
            await MergeAsync(seed, seed.Host, AccommodationGroupIdentification.From(seed.FourthGroup));
            lazyLoads.Count.ShouldBe(0, "Объединение и уведомление после него не должны делать ленивых загрузок");
        }

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        var claimIds = new[] { seed.Host.ClaimId, seed.Guest.ClaimId, seed.WithoutType.ClaimId, seed.Fourth.ClaimId };
        var groups = await db.Set<Claim>()
            .Where(claim => claimIds.Contains(claim.ClaimId))
            .Select(claim => claim.AccommodationRequest_Id)
            .ToListAsync();
        groups.Count.ShouldBe(4);
        groups.ShouldAllBe(groupId => groupId == seed.HostGroup.AccommodationRequestId);

        var emptied = new[] { seed.GuestGroup.AccommodationRequestId, seed.FourthGroup.AccommodationRequestId };
        (await db.Set<AccommodationRequest>().AnyAsync(group => emptied.Contains(group.Id)))
            .ShouldBeFalse("Опустевшие группы гостя и четвёртого должны быть удалены");
        (await db.Set<AccommodationInvite>().AnyAsync(invite => invite.ProjectId == seed.ProjectId.Value))
            .ShouldBeFalse();
    }

    private Task MergeAsync(Seed seed, ClaimIdentification host, AccommodationGroupIdentification target)
        => factory.Services.RunAsAsync(seed.OwnerId, sp =>
            sp.GetRequiredService<IAccommodationInviteService>().MergeAccommodationGroups(host, target));

    /// <summary>
    /// Проект с поселением, один тип на четверых и четыре заявки: хозяин, гость и четвёртый — каждый в
    /// своей группе, третья — без типа.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        var (ownerId, _) = await CreateUserAsync();

        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для объединения групп проживания");
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
                    "Домик",
                    new MarkdownString("Домик"),
                    Cost: 0,
                    Capacity: 4,
                    IsPlayerSelectable: true)));

        var claims = await CreateClaimsAsync(ownerId, projectId, count: 4);
        var (host, guest, withoutType, fourth) = (claims[0], claims[1], claims[2], claims[3]);

        var groupIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.SetAccommodationType(projectId.Value, host.ClaimId, typeId.AccommodationTypeId);
            await claimService.SetAccommodationType(projectId.Value, guest.ClaimId, typeId.AccommodationTypeId);
            await claimService.SetAccommodationType(projectId.Value, fourth.ClaimId, typeId.AccommodationTypeId);

            return await AccommodationTestHelpers.GetAccommodationGroupIdsAsync(sp, [host, guest, fourth]);
        });

        return new Seed(ownerId, projectId, host, groupIds[0], guest, groupIds[1], withoutType, fourth, groupIds[2]);
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

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }

    private sealed record Seed(
        UserIdentification OwnerId,
        ProjectIdentification ProjectId,
        ClaimIdentification Host,
        AccommodationRequestIdentification HostGroup,
        ClaimIdentification Guest,
        AccommodationRequestIdentification GuestGroup,
        ClaimIdentification WithoutType,
        ClaimIdentification Fourth,
        AccommodationRequestIdentification FourthGroup);
}
