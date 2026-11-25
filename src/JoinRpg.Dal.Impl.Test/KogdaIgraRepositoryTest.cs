using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel.Projects;
using JoinRpg.DomainTypes.ProjectMetadata;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test;

public class KogdaIgraRepositoryTest
{
    private static KogdaIgraGame Game(int? status) => new()
    {
        KogdaIgraGameId = 1,
        Name = "Игра",
        Active = true,
        Begin = new DateTime(2026, 3, 1),
        End = new DateTime(2026, 3, 5),
        LastUpdatedAt = DateTimeOffset.UtcNow,
        Status = status,
    };

    [Fact]
    public void TryConvert_OkStatus_Mapped()
        => KogdaIgraRepository.TryConvert(Game(0))!.Status.ShouldBe(KogdaIgraGameStatus.Ok);

    [Fact]
    public void TryConvert_CancelledStatus_Mapped()
        => KogdaIgraRepository.TryConvert(Game(5))!.Status.ShouldBe(KogdaIgraGameStatus.Cancelled);

    [Fact]
    public void TryConvert_NullStatus_StaysNull()
        => KogdaIgraRepository.TryConvert(Game(null))!.Status.ShouldBeNull();
}
