using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.WebPortal.Managers.CharacterGroups;
using ProjectRolesList = JoinRpg.DataModel.ProjectRolesList;

namespace JoinRpg.WebPortal.Managers.Test.CharacterGroups;

/// <summary>Доступ к сетке, вшитой в вводную (ADR029), а не к публичной странице сетки.</summary>
public class ProjectRoleGridViewServiceAccessTests
{
    private readonly MockedProject mock = new();

    [Fact]
    public async Task PrivateGrid_PlayerWithApprovedTargetClaim_HasAccess()
    {
        mock.CreateApprovedClaim(mock.Character, mock.Player);
        var service = CreateService(publicMode: false, userId: mock.Player.UserId);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeTrue();
        result.NotFound.ShouldBeFalse();
        result.Grid.ShouldNotBeNull();
    }

    [Fact]
    public async Task PrivateGrid_PlayerWithClaimOnAnotherCharacter_NoAccess()
    {
        var another = mock.CreateCharacter("Другой персонаж");
        mock.CreateApprovedClaim(another, mock.Player);
        var service = CreateService(publicMode: false, userId: mock.Player.UserId);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task PrivateGrid_GroupTarget_MatchesPlayerCharacterGroups()
    {
        mock.Character.ParentCharacterGroupIds = [mock.Group.CharacterGroupId];
        mock.CreateApprovedClaim(mock.Character, mock.Player);
        var service = CreateService(publicMode: false, userId: mock.Player.UserId, groupTarget: true);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeTrue();
        result.NotFound.ShouldBeFalse();
        result.Grid.ShouldNotBeNull();
    }

    [Fact]
    public async Task PrivateGrid_Master_HasAccessEvenWhenUnpublished()
    {
        var service = CreateService(publicMode: false, published: false, userId: mock.Master.UserId);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeTrue();
        result.NotFound.ShouldBeFalse();
        result.Grid.ShouldNotBeNull();
    }

    [Fact]
    public async Task PublicGrid_PublishedPlot_AnonymousHasAccess()
    {
        var service = CreateService(publicMode: true, userId: null);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeTrue();
        result.NotFound.ShouldBeFalse();
        result.Grid.ShouldNotBeNull();
    }

    [Fact]
    public async Task PublicGrid_UnpublishedPlot_AnonymousHasNoAccess()
    {
        var service = CreateService(publicMode: true, published: false, userId: null);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task PublicGrid_MasterOnlyPlot_PlayerHasNoAccess()
    {
        mock.CreateApprovedClaim(mock.Character, mock.Player);
        var service = CreateService(publicMode: true, isMasterOnly: true, userId: mock.Player.UserId);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task PrivateGrid_PublishPlotEnabled_AnonymousHasAccess()
    {
        var service = CreateService(publicMode: false, publishPlot: true, userId: null);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeTrue();
        result.NotFound.ShouldBeFalse();
        result.Grid.ShouldNotBeNull();
    }

    [Fact]
    public async Task PrivateGrid_AnonymousWithoutClaims_NoAccess()
    {
        var service = CreateService(publicMode: false, userId: null);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.HasAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task NonRoleGridElement_NotFound()
    {
        var service = CreateService(publicMode: true, elementType: PlotElementType.RegularPlot, userId: mock.Master.UserId);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.NotFound.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletedGrid_NotFound()
    {
        var service = CreateService(publicMode: true, includeRolesList: false, userId: mock.Master.UserId);

        var result = await service.GetRoleGridForPlot(ElementId);

        result.NotFound.ShouldBeTrue();
    }

    private PlotElementIdentification ElementId => new(mock.ProjectInfo.ProjectId, 10, 20);

    private ProjectRoleGridViewService CreateService(
        bool publicMode,
        int? userId,
        bool published = true,
        bool publishPlot = false,
        bool groupTarget = false,
        bool includeRolesList = true,
        bool isMasterOnly = false,
        PlotElementType elementType = PlotElementType.RoleGrid)
    {
        if (includeRolesList)
        {
            mock.Project.ProjectRolesLists.Add(new ProjectRolesList
            {
                Project = mock.Project,
                ProjectId = mock.Project.ProjectId,
                ProjectRolesListId = 30,
                Name = "Сетка для вводной",
                PublicMode = publicMode,
                FieldIds = [],
            });
        }
        mock.Project.Details.PublishPlot = publishPlot;
        mock.ReInitProjectInfo();

        var element = CreateElement(elementType, published, groupTarget, includeRolesList, isMasterOnly);
        var currentUser = userId is { } id ? new FakeCurrentUserAccessor(id) : new FakeCurrentUserAccessor();

        return new ProjectRoleGridViewService(
            new FakeCharacterInfoRepository(mock),
            FakeUserRepository.MustNotBeCalled("поля-ссылки в тестовой сетке не выбраны"),
            new EmptyClaimInfoRepository(),
            new FakeProjectMetadataRepository(mock),
            characterGroupRepository: null!, // GroupsViewMode.None — описания групп не загружаются.
            currentUser,
            new FakeAvatarUriResolver(),
            new StubPlotRepository(element));
    }

    private PlotElementDetailsDto CreateElement(PlotElementType elementType, bool published, bool groupTarget, bool includeRolesList, bool isMasterOnly)
    {
        var target = groupTarget
            ? new TargetsInfo([], [new GroupTarget(mock.Group.GetId(), mock.Group.CharacterGroupName)])
            : new TargetsInfo([new CharacterTarget(mock.Character.GetId(), mock.Character.CharacterName)], []);

        return new PlotElementDetailsDto(
            ElementId,
            elementType,
            isMasterOnly,
            IsActive: true,
            IsCompleted: published,
            PublishedVersion: published ? 0 : null,
            target,
            "Сюжет",
            new PlotElementVersionDto(0, null, "", DateTime.UtcNow, null),
            LastVersionNumber: 0,
            LastVersionTodoField: "",
            PrevVersionModifiedAt: null,
            NextVersionModifiedAt: null,
            includeRolesList ? new ProjectRolesListIdentification(mock.ProjectInfo.ProjectId, 30) : null);
    }

    private sealed class StubPlotRepository(PlotElementDetailsDto element) : IPlotRepository
    {
        public Task<PlotElementDetailsDto?> GetPlotElementDetails(PlotElementIdentification elementId, int? version = null)
            => Task.FromResult<PlotElementDetailsDto?>(element);

        public void Dispose() { }
        public Task<IReadOnlyList<PlotFolder>> GetPlots(ProjectIdentification projectId) => throw new NotSupportedException();
        public Task<PlotFolderDetailsDto?> GetPlotFolderDetails(PlotFolderIdentification plotFolderId) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PlotElement>> GetDirectPlotsForCharacter(CharacterIdentification characterId) => throw new NotSupportedException();
        public Task<IReadOnlyList<PlotFolderDetailsDto>> GetActivePlotFolders(ProjectIdentification projectId) => throw new NotSupportedException();
        [Obsolete]
        public Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PlotTextDto>> GetPlotsBySpecification(PlotSpecification plotSpecification) => throw new NotSupportedException();
    }

    /// <summary>Контакты в тестовой сетке не показываются, поэтому заявки для ячеек не нужны.</summary>
    private sealed class EmptyClaimInfoRepository : IClaimInfoRepository
    {
        public Task<IReadOnlyDictionary<CharacterIdentification, ClaimInfo>> GetApprovedClaimInfos(IReadOnlyCollection<CharacterInfo> characters)
            => Task.FromResult<IReadOnlyDictionary<CharacterIdentification, ClaimInfo>>(new Dictionary<CharacterIdentification, ClaimInfo>());

        public Task<ClaimInfo?> GetClaimInfoOrDefault(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task<ClaimInCharacter?> GetClaimInCharacterOrDefault(ClaimIdentification claimId) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> GetClaimInfos(IReadOnlyCollection<ClaimIdentification> claimIds) => throw new NotSupportedException();
    }
}
