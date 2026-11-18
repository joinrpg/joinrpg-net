using JoinRpg.Data.Interfaces.ProjectMetadata;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.WebPortal.Managers.ProjectMasterTools.Badges;

namespace JoinRpg.WebPortal.Managers.Test.ProjectMasterTools;

/// <summary>
/// Кнопки правки шаблонов бейджей видны только там, где их можно сохранить: сервис шаблонов меняет
/// их лишь в активном проекте, и в архивном или заблокированном кнопки вели бы на отказ.
/// </summary>
public class BadgeTemplateViewServiceTests
{
    private readonly MockedProject mock = new();

    private BadgeTemplateViewService CreateService()
        => new(new EmptyTemplateRepository(), null!, new FakeCurrentUserAccessor(mock.Master.UserId), new FakeProjectMetadataRepository(mock));

    [Fact]
    public async Task ActiveProject_MasterCanEdit()
        => (await CreateService().GetList(mock.ProjectInfo.ProjectId)).HasEditAccess.ShouldBeTrue();

    [Fact]
    public async Task ArchivedProject_NoEditing()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
        mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Archived);

        (await CreateService().GetList(mock.ProjectInfo.ProjectId)).HasEditAccess.ShouldBeFalse();
    }

    [Fact]
    public async Task BlockedProject_NoEditing()
    {
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
        mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Blocked);

        (await CreateService().GetList(mock.ProjectInfo.ProjectId)).HasEditAccess.ShouldBeFalse();
    }

    private class EmptyTemplateRepository : IProjectBadgeTemplateRepository
    {
        public Task<IReadOnlyCollection<ProjectBadgeTemplate>> GetForProjectAsync(ProjectIdentification projectId)
            => Task.FromResult<IReadOnlyCollection<ProjectBadgeTemplate>>([]);

        public Task<ProjectBadgeTemplate?> GetByIdAsync(ProjectBadgeTemplateIdentification id)
            => Task.FromResult<ProjectBadgeTemplate?>(null);
    }
}
