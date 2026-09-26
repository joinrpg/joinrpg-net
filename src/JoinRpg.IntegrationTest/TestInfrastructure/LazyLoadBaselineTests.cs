namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Проверки самого механизма сверки со снапшотом (#4914): механизм, который молча пропускает
/// регрессии, хуже, чем его отсутствие.
/// </summary>
public class LazyLoadBaselineTests
{
    private const string Route = "GET /{projectId}/plots/Edit";

    private static LazyLoadBaseline Baseline(int allowed)
        => new(new Dictionary<string, int> { [Route] = allowed }, updateMode: false);

    [Fact]
    public void Growth_Fails()
        => Should.Throw<LazyLoadBaselineException>(
            () => Baseline(2).Check(new LazyLoadObservation(Route, 3)));

    [Fact]
    public void SameCount_Passes()
        => Baseline(2).Check(new LazyLoadObservation(Route, 2));

    [Fact]
    public void Decrease_Passes()
        => Baseline(2).Check(new LazyLoadObservation(Route, 0));

    /// <summary>Маршрут не в снапшоте — значит долга за ним нет и ленивые загрузки запрещены.</summary>
    [Fact]
    public void UnknownRoute_MustBeZero()
        => Should.Throw<LazyLoadBaselineException>(
            () => Baseline(2).Check(new LazyLoadObservation("GET /{projectId}/roles/hot", 1)));

    [Fact]
    public void UnknownRoute_WithoutLazyLoads_Passes()
        => Baseline(2).Check(new LazyLoadObservation("GET /{projectId}/roles/hot", 0));
}
