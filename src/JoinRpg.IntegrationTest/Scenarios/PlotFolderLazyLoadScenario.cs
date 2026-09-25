using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Таргеты вводных должны грузиться вместе с папкой сюжета, а не по одной вводной за раз (#4788).
/// </summary>
/// <remarks>
/// Воспроизводит ровно тот N+1, который нашёлся в логах прода: страница редактирования сюжета
/// разворачивает <c>TargetCharacters</c> и <c>TargetGroups</c> на каждой вводной, и без eager loading
/// это два отдельных запроса на элемент — на больших папках выходило ~2400 запросов на страницу.
///
/// Тест повторяет то, что делает вью-модель (<c>PlotElementListItemViewModel</c> зовёт
/// <c>PlotExtensions.ToTarget</c>), а не открывает страницу по HTTP: обращение к коллекциям и есть
/// то, что вызывает ленивую загрузку, и его надо воспроизвести явно, иначе тест ничего не проверяет.
/// </remarks>
[Collection(PlotScenarioCollection.Name)]
public class PlotFolderLazyLoadScenario(JoinApplicationFactory factory)
{
    private const int SmallFolderElements = 3;
    private const int LargeFolderElements = 12;

    [Fact]
    public async Task GetPlotFolder_LoadsTargetsEagerly_NoQueryPerElement()
    {
        UserIdentification masterId;
        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, masterId, "Проект с большими сюжетами");
        }

        var smallFolder = await factory.Services.RunAsAsync(
            masterId,
            sp => TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, SmallFolderElements));
        var largeFolder = await factory.Services.RunAsAsync(
            masterId,
            sp => TestPlotHelpers.SeedPlotFolderAsync(sp, projectId, LargeFolderElements));

        var smallCount = await CountLazyTargetLoadsAsync(masterId, smallFolder.PlotFolderId);
        var largeCount = await CountLazyTargetLoadsAsync(masterId, largeFolder.PlotFolderId);

        // Ноль, а не «столько же»: таргеты должны приезжать вместе с папкой. Запросы самого фикса
        // (через Include) в счёт не попадают — считаются только ленивые, по подписи EntityKeyValue1.
        smallCount.ShouldBe(
            0,
            $"Папка из {SmallFolderElements} вводных дала {smallCount} ленивых загрузок таргетов");
        largeCount.ShouldBe(
            0,
            $"Папка из {LargeFolderElements} вводных дала {largeCount} ленивых загрузок таргетов — "
            + "запрос снова грузит таргеты по одной вводной за раз, см. #4788");
    }

    /// <summary>
    /// Грузит папку через репозиторий и обходит таргеты всех вводных так же, как это делает вью-модель,
    /// считая ленивые загрузки таблиц связи.
    /// </summary>
    private async Task<int> CountLazyTargetLoadsAsync(
        UserIdentification masterId,
        PlotFolderIdentification plotFolderId)
    {
        return await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var plotRepository = sp.GetRequiredService<IPlotRepository>();

            using var counter = LazyTargetLoadCounter.Start();

            var folder = await plotRepository.GetPlotFolderAsync(plotFolderId)
                ?? throw new InvalidOperationException($"Папка сюжета {plotFolderId} не найдена");

            foreach (var element in folder.Elements)
            {
                // Именно это обращение и вызывало ленивую загрузку на каждой вводной.
                _ = element.ToTarget();
            }

            return counter.Count;
        });
    }
}
