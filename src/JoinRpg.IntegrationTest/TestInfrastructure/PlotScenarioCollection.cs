using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTests.TestInfrastructure;

/// <summary>
/// Сценарии страниц сюжетов выполняются последовательно.
/// </summary>
/// <remarks>
/// Нужно из-за <see cref="LazyTargetLoadCounter"/>: <c>DbInterception</c> в EF6 глобален на процесс,
/// поэтому запросы к таблицам связи из параллельного теста попали бы в чужой замер.
/// </remarks>
[CollectionDefinition(Name)]
public class PlotScenarioCollection : ICollectionFixture<JoinApplicationFactory>
{
    public const string Name = "Plots";
}
