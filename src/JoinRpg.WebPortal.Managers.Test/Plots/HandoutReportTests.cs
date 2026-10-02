using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.WebPortal.Managers.Plots;

namespace JoinRpg.WebPortal.Managers.Test.Plots;

/// <summary>
/// Отчёт по раздаткам: строки идут в порядке сюжетов (папки — в заданном мастерами порядке, внутри
/// папки — её элементы), а не в порядке, в котором раздатки встретились при обходе персонажей.
/// </summary>
public class HandoutReportTests
{
    private readonly MockedProject mock = new();

    [Fact]
    public async Task HandoutReport_RowsFollowPlotOrder_NotCharacterTraversalOrder()
    {
        var firstCharacter = mock.Character;
        var secondCharacter = mock.CreateCharacter("Второй персонаж");
        mock.ReInitProjectInfo();

        // Репозиторий отдаёт раздатки в порядке сюжетов: сначала папка 20, потом папка 10,
        // внутри папки 20 — элемент 105, затем 103. Ни по id папок, ни по id элементов это не возрастание.
        var inFolder20First = Handout(folderId: 20, elementId: 105, "Письмо", secondCharacter);
        var inFolder20Second = Handout(folderId: 20, elementId: 103, "Карта", firstCharacter, secondCharacter);
        var inFolder10 = Handout(folderId: 10, elementId: 200, "Амулет", firstCharacter);

        var service = CreateService([inFolder20First, inFolder20Second, inFolder10]);

        var report = await service.GetHandoutReport(mock.ProjectInfo.ProjectId, PlotVersionFilter.LatestVersion);

        report.Handouts.Select(x => x.Text).ShouldBe(["Письмо", "Карта", "Амулет"]);
        report.Handouts.Select(x => x.Count).ShouldBe([1, 2, 1]);
    }

    private CharacterPlotViewService CreateService(IReadOnlyCollection<PlotTextDto> handouts)
        => new(new FakeCharacterInfoRepository(mock), new FakePlotRepository(handouts), new FakeCurrentUserAccessor(mock.Master.UserId));

    private PlotTextDto Handout(int folderId, int elementId, string text, params Character[] targets)
        => new()
        {
            Id = new PlotVersionIdentification(mock.ProjectInfo.ProjectId, folderId, elementId, 1),
            Content = new MarkdownDbValue(text),
            TodoField = "",
            Latest = true,
            Published = true,
            HasPublished = true,
            Completed = true,
            IsActive = true,
            Target = new TargetsInfo([.. targets.Select(c => new CharacterTarget(c.GetId(), c.CharacterName))], []),
        };

    /// <summary>
    /// Отдаёт заранее заданный список раздаток — ровно так же упорядоченный, как его отдаёт
    /// настоящий репозиторий (см. <c>PlotRepositoryImpl.GetPlotsBySpecification</c>).
    /// </summary>
    private sealed class FakePlotRepository(IReadOnlyCollection<PlotTextDto> handouts) : IPlotRepository
    {
        public Task<IReadOnlyCollection<PlotTextDto>> GetPlotsBySpecification(PlotSpecification plotSpecification)
            => Task.FromResult(handouts);

        public void Dispose() { }

        public Task<IReadOnlyList<PlotFolder>> GetPlots(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<PlotFolderDetailsDto?> GetPlotFolderDetails(PlotFolderIdentification plotFolderId) => throw new NotSupportedException();
        public Task<PlotElementDetailsDto?> GetPlotElementDetails(PlotElementIdentification elementId, int? version = null) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PlotElement>> GetDirectPlotsForCharacter(CharacterIdentification characterId) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlotFolderDetailsDto>> GetActivePlotFolders(ProjectIdentification projectId) => throw new NotSupportedException();
        [Obsolete]
        public Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname) => throw new NotSupportedException();
    }
}
