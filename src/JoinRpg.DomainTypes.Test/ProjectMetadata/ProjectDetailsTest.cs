using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test.ProjectMetadata;

public class ProjectDetailsTest
{
    private static ProjectDetails MakeDetails(params KogdaIgraGameData[] cards)
        => new(ProjectInfoFixture.Build(), new MarkdownString(""), new MarkdownString(""),
               cards, false, new MarkdownString(""), []);

    private static KogdaIgraGameData Card(
        int id, int daysFromNow,
        KogdaIgraGameStatus? status = KogdaIgraGameStatus.Ok, bool isActive = true)
        => new(new KogdaIgraIdentification(id), $"Игра {id}",
               DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysFromNow),
               DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysFromNow + 1),
               "Регион", "МГ", SiteUri: null, IsActive: isActive, Status: status);

    [Fact]
    public void FutureOkCard_IsSelected()
        => MakeDetails(Card(1, 10)).NearestFutureOkKogdaIgraCard!.Id.Value.ShouldBe(1);

    [Fact]
    public void NearestCancelled_LaterOk_LaterOkSelected()
        => MakeDetails(Card(1, 10, KogdaIgraGameStatus.Cancelled), Card(2, 30))
            .NearestFutureOkKogdaIgraCard!.Id.Value.ShouldBe(2);

    [Fact]
    public void NullStatusCard_IsNotSelected()
        => MakeDetails(Card(1, 10, status: null)).NearestFutureOkKogdaIgraCard.ShouldBeNull();

    [Fact]
    public void PassedStatusCard_IsNotSelected()
        => MakeDetails(Card(1, 10, KogdaIgraGameStatus.Passed)).NearestFutureOkKogdaIgraCard.ShouldBeNull();

    [Fact]
    public void PastOkCard_IsNotSelected()
        => MakeDetails(Card(1, -30)).NearestFutureOkKogdaIgraCard.ShouldBeNull();

    [Fact]
    public void InactiveOkCard_IsNotSelected()
        => MakeDetails(Card(1, 10, isActive: false)).NearestFutureOkKogdaIgraCard.ShouldBeNull();
}
