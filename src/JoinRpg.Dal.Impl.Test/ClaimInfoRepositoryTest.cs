using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories.Characters;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Mocks.Fakes;
using JoinRpg.DomainTypes;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

/// <summary>
/// Батчевые загрузки <see cref="ClaimInfoRepository"/> поверх фейков персонажей и профилей:
/// строгая <c>GetClaimInfos</c> требует все заявки, null-safe <c>GetClaimInfosOrDefault</c>
/// пропускает отсутствующие.
/// </summary>
public class ClaimInfoRepositoryTest
{
    private readonly MockedProject mock = new();

    private ClaimInfoRepository CreateRepository()
        => new(new FakeCharacterInfoRepository(mock), new FakeUserRepository(mock.TryGetUserInfo));

    private ClaimIdentification MissingClaim => new(mock.ProjectInfo.ProjectId, 9999);

    [Fact]
    public async Task GetClaimInfos_LoadsAllRequestedClaims()
    {
        var claim1 = mock.CreateClaim(mock.Character, mock.Player);
        var claim2 = mock.CreateClaim(mock.Character, mock.Player);

        var loaded = await CreateRepository().GetClaimInfos([claim1.GetId(), claim2.GetId()]);

        loaded.Keys.ShouldBe([claim1.GetId(), claim2.GetId()], ignoreOrder: true);
        loaded[claim1.GetId()].Character.CharacterName.ShouldBe(mock.Character.CharacterName);
        loaded[claim1.GetId()].Player.UserId.ShouldBe(new UserIdentification(mock.Player.UserId));
    }

    [Fact]
    public async Task GetClaimInfos_ThrowsOnMissingClaim()
        => _ = await Should.ThrowAsync<InvalidOperationException>(
            () => CreateRepository().GetClaimInfos([MissingClaim]));

    [Fact]
    public async Task GetClaimInfosOrDefault_SkipsMissingClaims()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var loaded = await CreateRepository().GetClaimInfosOrDefault([claim.GetId(), MissingClaim]);

        loaded.Keys.ShouldBe([claim.GetId()]);
        loaded[claim.GetId()].Character.CharacterName.ShouldBe(mock.Character.CharacterName);
    }

    [Fact]
    public async Task GetClaimInfosOrDefault_EmptyInput_LoadsNothing()
        => (await CreateRepository().GetClaimInfosOrDefault([])).ShouldBeEmpty();
}
