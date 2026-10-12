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
internal sealed class FakeProjectMetadataWriteRepository(MockedProject mock) : IProjectMetadataWriteRepository
{
    public Task<IProjectMetadataUpdateHandle> LoadProjectForUpdate(ProjectIdentification projectId)
        => Task.FromResult<IProjectMetadataUpdateHandle>(new Handle(mock));

    /// <summary>
    /// Счётчик IDENTITY групп в моке: id не ниже него — «не потерянный» (как <c>IDENT_CURRENT</c> в бою).
    /// </summary>
    internal const int GroupIdentityCurrent = 100_000;

    /// <summary>Оболочка группы так же, как в бою. Мок — один проект, «занят другим проектом» не бывает.</summary>
    public Task<RestoredShellInsertResult> InsertRestoredCharacterGroupShell(RestoredCharacterGroupShell shell)
    {
        if (mock.Project.CharacterGroups.Any(g => g.CharacterGroupId == shell.CharacterGroupId.CharacterGroupId))
        {
            return Task.FromResult(RestoredShellInsertResult.AlreadyExists);
        }
        if (shell.CharacterGroupId.CharacterGroupId >= GroupIdentityCurrent)
        {
            return Task.FromResult(RestoredShellInsertResult.NotLostId);
        }

        var group = mock.CreateCharacterGroup();
        group.CharacterGroupId = shell.CharacterGroupId.CharacterGroupId;
        group.CharacterGroupName = shell.Name;
        group.IsPublic = false;
        group.Description = new MarkdownDbValue("");
        group.ParentCharacterGroupIds = [shell.RootGroupId.CharacterGroupId];
        group.CreatedAt = group.UpdatedAt = shell.CreatedAt;
        group.CreatedById = group.UpdatedById = shell.CreatedBy.Value;
        // Строка уже в «базе»: следующее чтение метаданных её видит.
        mock.ReInitProjectInfo();
        return Task.FromResult(RestoredShellInsertResult.Inserted);
    }

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

        public List<object> Added { get; } = [];

        public void Add(object entity)
        {
            Added.Add(entity);
            // Имитация relationship fixup EF6: реальный DbContext синхронно связывает добавленную
            // сущность с уже загруженными navigation-коллекциями того же контекста.
            if (entity is Character character && !mock.Project.Characters.Contains(character))
            {
                mock.Project.Characters.Add(character);
            }
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
            if (entity is ProjectAccommodationType accommodationType)
            {
                _ = mock.Project.ProjectAccommodationTypes.Remove(accommodationType);
                _ = accommodationType.RoomCategory?.AccommodationTypes.Remove(accommodationType);
            }
            if (entity is ProjectRoomCategory roomCategory)
            {
                _ = mock.Project.ProjectRoomCategories.Remove(roomCategory);
            }
            if (entity is ProjectFieldDropdownValue variant)
            {
                foreach (var field in mock.Project.ProjectFields)
                {
                    _ = field.DropdownValues.Remove(variant);
                }
            }
            if (entity is CharacterGroup group)
            {
                _ = mock.Project.CharacterGroups.Remove(group);
            }
        }
    }
}
