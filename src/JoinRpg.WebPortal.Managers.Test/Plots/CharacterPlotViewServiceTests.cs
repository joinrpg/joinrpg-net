using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.WebPortal.Managers.Plots;

namespace JoinRpg.WebPortal.Managers.Test.Plots;

/// <summary>
/// Выборка вводных для страницы персонажа: обычные вводные и вводные-сетки (ADR029) приходят
/// одним запросом, раздатки им не смешиваются.
/// </summary>
public class CharacterPlotViewServiceTests
{
    private readonly MockedProject mock = new();

    [Fact]
    public async Task GetPlotsForCharacter_RequestsRegularPlotAndRoleGrid_PublishedVersion()
    {
        var repository = new SpecCapturingPlotRepository();

        var service = new CharacterPlotViewService(
            new FakeCharacterInfoRepository(mock), repository, new FakeCurrentUserAccessor(mock.Master.UserId));

        _ = await service.GetPlotsForCharacter(mock.Character.GetId());

        repository.CapturedSpecification.ShouldNotBeNull();
        repository.CapturedSpecification!.ElementTypes
            .ShouldBe([PlotElementType.RegularPlot, PlotElementType.RoleGrid]);
        repository.CapturedSpecification.VersionFilter.ShouldBe(PlotVersionFilter.PublishedVersion);
    }

    /// <summary>Запоминает спецификацию запроса — остальное интерфейсу не нужно.</summary>
    private sealed class SpecCapturingPlotRepository : IPlotRepository
    {
        public PlotSpecification? CapturedSpecification { get; private set; }

        public Task<IReadOnlyCollection<PlotTextDto>> GetPlotsBySpecification(PlotSpecification plotSpecification)
        {
            CapturedSpecification = plotSpecification;
            return Task.FromResult<IReadOnlyCollection<PlotTextDto>>([]);
        }

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
