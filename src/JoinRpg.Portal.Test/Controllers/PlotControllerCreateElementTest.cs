using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Portal.Controllers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Plots.Elements;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Сюжет для новой вводной могли не выбрать: биндер на пустое значение молча даёт null без ошибки
/// в ModelState. Это должно быть понятной ошибкой на форме, а не падением в сервисе.
/// </summary>
public class PlotControllerCreateElementTest
{
    [Fact]
    public async Task EmptyPlotFolder_ReturnsFormWithError_AndDoesNotCreateElement()
    {
        var mock = new MockedProject();
        var plotService = new RecordingPlotService();
        // Репозиторий сюжетов не передаём: без выбранного сюжета искать папку нечего.
        var controller = new PlotController(
            plotService,
            plotRepository: null!,
            uriService: null!,
            new FakeProjectMetadataRepository(mock),
            new FakeCurrentUserAccessor(mock.Master.UserId),
            logger: null!,
            linkRendererFactory: null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        var result = await controller.CreateElement(
            new ProjectIdentification(mock.Project.ProjectId),
            plotFolderId: null,
            content: "# Вводная",
            todoField: "",
            targetCharacters: [],
            targetGroups: [],
            PlotElementTypeView.RegularPlot,
            publishNow: false,
            isMasterOnly: false);

        var model = result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<PlotElementCreateViewModel>();
        model.Content.ShouldBe("# Вводная");
        controller.ModelState[""].ShouldNotBeNull()
            .Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Выберите сюжет");
        plotService.CreatePlotElementCalls.ShouldBe(0);
    }

    private sealed class RecordingPlotService : IPlotService
    {
        public int CreatePlotElementCalls { get; private set; }

        public Task<PlotVersionIdentification> CreatePlotElement(PlotFolderIdentification plotFolderId, string content, string todoField,
            IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars, PlotElementType elementType, bool isMasterOnly)
        {
            CreatePlotElementCalls++;
            throw new NotSupportedException();
        }

        public Task<PlotFolderIdentification> CreatePlotFolder(ProjectIdentification projectId, string masterTitle, string todo) => throw new NotSupportedException();
        public Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField) => throw new NotSupportedException();
        public Task DeleteFolder(int projectId, int plotFolderId) => throw new NotSupportedException();
        public Task DeleteElement(PlotElementIdentification plotElementId) => throw new NotSupportedException();
        public Task EditPlotElement(PlotElementIdentification plotelementid, string contents, string todoField,
            IReadOnlyCollection<CharacterGroupIdentification> targetGroups, IReadOnlyCollection<CharacterIdentification> targetChars, bool isMasterOnly) => throw new NotSupportedException();
        public Task PublishElementVersion(PlotVersionIdentification version, bool sendNotification, string? commentText) => throw new NotSupportedException();
        public Task UnPublishElement(PlotElementIdentification element) => throw new NotSupportedException();
        public Task EditPlotElementText(PlotElementIdentification plotelementid, string content, string todoField) => throw new NotSupportedException();
        public Task ReorderPlots(PlotFolderIdentification plotFolderId, PlotFolderIdentification? afterPlotFolderId) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlotElementIdentification>> ReorderPlotByChar(CharacterIdentification characterIdentification, PlotElementIdentification targetId, PlotElementIdentification? afterId) => throw new NotSupportedException();
        public Task ReorderPlotElements(PlotElementIdentification plotElementId, PlotElementIdentification? afterPlotElementId) => throw new NotSupportedException();
    }
}
