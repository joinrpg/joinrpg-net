using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;

namespace JoinRpg.Services.Impl.Test.Claims;

/// <summary>
/// Тесты на сам фейк write-репозитория: он обязан держать те же инварианты, что боевая реализация.
/// Иначе тесты сервисов пройдут там, где боевой код упадёт — ровно та ошибка, из-за которой в
/// ADR013 мок «врал» про заявки игрока.
/// </summary>
public class FakeCharacterAggregateWriteRepositoryTest : ClaimServiceTestBase
{
    private UserIdentification InitiatorId => new(mock.Master.UserId);

    [Fact]
    public async Task CharacterInfoSharesProjectInfoInstanceWithHandle()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId(), InitiatorId);

        // Конструктор CharacterInfo требует ровно тот же экземпляр ProjectInfo (ADR013).
        ReferenceEquals(handle.CharacterInfo.ProjectInfo, handle.ProjectInfo).ShouldBeTrue();
    }

    [Fact]
    public async Task ClaimInfoIsSameInstanceAsInCharacterInfo()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId(), InitiatorId);

        var fromAggregate = handle.CharacterInfo.Claims.Single(c => c.ClaimId == claim.GetId());
        ReferenceEquals(handle.ClaimInfo, fromAggregate).ShouldBeTrue();
    }

    [Fact]
    public async Task LoadOtherCharacterThrowsWhenCharacterMissing()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId(), InitiatorId);

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => handle.LoadOtherCharacter(new CharacterIdentification(ProjectId, 100500)));
    }

    [Fact]
    public async Task LoadOtherCharacterReturnsBothEntityAndInfo()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var other = mock.CreateCharacter("Other character");
        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId(), InitiatorId);

        var (entity, info) = await handle.LoadOtherCharacter(other.GetId());

        entity.ShouldBeSameAs(other);
        info.Id.ShouldBe(other.GetId());
        ReferenceEquals(info.ProjectInfo, handle.ProjectInfo).ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshProjectInfoReturnsNewInstance()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var handle = await WriteRepository.LoadClaimForUpdate(claim.GetId(), InitiatorId);
        var before = handle.ProjectInfo;

        var after = await handle.RefreshProjectInfo();

        ReferenceEquals(before, after).ShouldBeFalse();
        handle.ProjectInfo.ShouldBeSameAs(after);
    }

    [Fact]
    public async Task LoadClaimForUpdateThrowsWhenClaimMissing()
        => _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => WriteRepository.LoadClaimForUpdate(new ClaimIdentification(ProjectId, 100500), InitiatorId));
}
