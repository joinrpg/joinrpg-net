using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Projects;

/// <summary>
/// Write-репозиторий поверх <see cref="MockedProject"/>: отдаёт согласованную пару
/// <see cref="Project"/>/<see cref="ProjectInfo"/> и пересобирает снимок через тот же
/// <c>CreateInfoFromProject</c>, что и боевой код.
/// </summary>

/// <summary>
/// Write-репозиторий поверх <see cref="MockedProject"/>: отдаёт согласованную пару
/// <see cref="Project"/>/<see cref="ProjectInfo"/> и пересобирает снимок через тот же
/// <c>CreateInfoFromProject</c>, что и боевой код.
/// </summary>
internal sealed class FakeProjectMetadataWriteRepository(MockedProject mock) : IProjectMetadataWriteRepository
{
    public Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId)
        => Task.FromResult<IProjectMetadataUpdateHandle>(new Handle(mock));

    private sealed class Handle : IProjectMetadataUpdateHandle
    {
        private readonly MockedProject mock;

        public Handle(MockedProject mock)
        {
            this.mock = mock;
            // Снимок ДО, согласованный с текущим Project (как делает боевой репозиторий при загрузке).
            mock.ReInitProjectInfo();
        }

        public Project Project => mock.Project;

        public ProjectInfo ProjectInfo => mock.ProjectInfo;

        public Task<ProjectInfo> Refresh()
        {
            mock.ReInitProjectInfo();
            return Task.FromResult(mock.ProjectInfo);
        }

        public List<object> Removed { get; } = [];

        public void Remove(object entity)
        {
            Removed.Add(entity);
            // Имитация relationship fixup EF6: реальный DbContext синхронно убирает удалённую
            // сущность из уже загруженных navigation-коллекций того же контекста.
            if (entity is ProjectAcl acl)
            {
                _ = mock.Project.ProjectAcls.Remove(acl);
            }
            if (entity is DataModel.ProjectRolesList rolesList)
            {
                _ = mock.Project.ProjectRolesLists.Remove(rolesList);
            }
            if (entity is ProjectFeeSetting feeSetting)
            {
                _ = mock.Project.ProjectFeeSettings.Remove(feeSetting);
            }
        }
    }
}

/// <summary>Записывает вызовы <see cref="IClaimService.SetResponsible"/> вместо реального изменения заявки.</summary>
