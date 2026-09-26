using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

/// <summary>
/// Подготовка «сыгранной роли» для x-api тестов: игрок, заявка, утверждение.
/// Лежит отдельно, потому что этот сценарий нужен больше одному тестовому классу.
/// </summary>
internal static class XApiClaimHelpers
{
    internal static Task<UserIdentification> CreatePlayer(this XApiMasterFixture fixture)
        => TestUserProjectHelpers.CreateTestUserAsync(fixture.Factory.Services);

    internal static Task<(UserIdentification userId, string email)> CreatePlayerWithEmail(this XApiMasterFixture fixture)
        => TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);

    /// <summary>Новый проект создаётся с закрытым приёмом заявок — открываем.</summary>
    internal static Task OpenClaims(this XApiMasterFixture fixture, ProjectIdentification projectId)
        => fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            sp => sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                new ProjectClaimSettings(
                    DefaultTemplate: null,
                    StrictlyOneCharacter: false,
                    AutoAcceptClaims: false,
                    IsAcceptingClaims: true,
                    IsPublicProject: true)));

    /// <summary>
    /// Заявку подаёт сам игрок: заявку, добавленную мастером, мастер же утвердить не может —
    /// её сначала должен принять игрок.
    /// </summary>
    internal static Task<ClaimIdentification> AddClaim(
        this XApiMasterFixture fixture,
        CharacterIdentification characterId,
        UserIdentification playerId)
        => fixture.Factory.Services.RunAsAsync(
            playerId,
            async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(characterId.ProjectId);
                return await sp.GetRequiredService<IClaimService>()
                    .AddClaimFromUser(
                        characterId, "Заявка от игрока", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
            });

    internal static Task ApproveClaim(this XApiMasterFixture fixture, ClaimIdentification claimId)
        => fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));

    /// <summary>Создаёт персонажа с утверждённой заявкой и возвращает его вместе с игроком.</summary>
    internal static async Task<CharacterWithPlayer> CreateCharacterWithPlayer(
        this XApiMasterFixture fixture,
        ProjectIdentification projectId)
    {
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var (playerId, playerEmail) = await fixture.CreatePlayerWithEmail();
        var claimId = await fixture.AddClaim(new CharacterIdentification(projectId, character.CharacterId), playerId);
        await fixture.ApproveClaim(claimId);
        return new CharacterWithPlayer(character.CharacterId, playerId, playerEmail);
    }
}

/// <summary>Персонаж с утверждённой заявкой и его игрок — чтобы в тесте было с чем сверять ответ.</summary>
internal record CharacterWithPlayer(int CharacterId, UserIdentification PlayerId, string PlayerEmail);
