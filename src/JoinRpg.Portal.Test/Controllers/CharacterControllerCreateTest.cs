using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models.Characters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Регрессия: в POST <see cref="CharacterController.Create"/> не было проверки
/// <c>ModelState.IsValid</c>. Если <c>ProjectEntityIdModelBinder</c> отвергал идентификатор
/// группы (чужой проект, нераспознанная строка), элемент просто выбрасывался из коллекции —
/// роль создавалась не в тех группах, и пользователь об этом не узнавал.
/// </summary>
public class CharacterControllerCreateTest
{
    [Fact]
    public async Task InvalidModelState_DoesNotCreateCharacter()
    {
        var mock = new MockedProject();
        var characterService = new RecordingCharacterService();
        var controller = CreateController(mock, characterService);
        // Так ведёт себя биндер, отвергнувший идентификатор группы: ошибка в ModelState,
        // а сам идентификатор в коллекцию не попал.
        controller.ModelState.AddModelError(
            nameof(AddCharacterViewModel.ParentCharacterGroupIds),
            "Идентификатор CharacterGroupIdentification относится к другому проекту");

        _ = await controller.Create(CreateViewModel(mock));

        characterService.AddCharacterCalls.ShouldBe(0);
    }

    [Fact]
    public async Task InvalidModelState_ReturnsFormWithError()
    {
        var mock = new MockedProject();
        var controller = CreateController(mock, new RecordingCharacterService());
        controller.ModelState.AddModelError(
            nameof(AddCharacterViewModel.ParentCharacterGroupIds),
            "Идентификатор CharacterGroupIdentification относится к другому проекту");

        var result = await controller.Create(CreateViewModel(mock));

        result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<AddCharacterViewModel>();
        controller.ModelState.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidModelState_CreatesCharacter()
    {
        var mock = new MockedProject();
        var characterService = new RecordingCharacterService();
        var controller = CreateController(mock, characterService);

        _ = await controller.Create(CreateViewModel(mock));

        characterService.AddCharacterCalls.ShouldBe(1);
    }

    private static AddCharacterViewModel CreateViewModel(MockedProject mock)
        => new()
        {
            ProjectId = mock.Project.ProjectId,
            ProjectName = mock.Project.ProjectName,
            ParentCharacterGroupIds = [mock.Group.GetId()],
            CharacterTypeInfo = CharacterTypeInfo.Default(),
        };

    private static CharacterController CreateController(MockedProject mock, ICharacterService characterService)
        // Остальные зависимости на пути POST Create не используются.
        => new(
            projectRepository: new FakeProjectRepository(mock),
            characterRepository: null!,
            characterInfoRepository: null!,
            characterService: characterService,
            projectMetadataRepository: new FakeProjectMetadataRepository(mock),
            currentUser: new FakeCurrentUserAccessor(mock.Master.UserId),
            userRepository: null!,
            characterPlotViewService: null!,
            linkRendererFactory: null!)
        {
            ControllerContext = new ControllerContext { HttpContext = CreateHttpContextWithEmptyForm() },
        };

    /// <summary>Значения полей персонажа читаются прямо из формы — она должна быть, пусть и пустая.</summary>
    private static DefaultHttpContext CreateHttpContextWithEmptyForm()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        return httpContext;
    }

    private sealed class RecordingCharacterService : ICharacterService
    {
        public int AddCharacterCalls { get; private set; }

        public Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest)
        {
            AddCharacterCalls++;
            return Task.FromResult(new CharacterIdentification(addCharacterRequest.ProjectId, 100));
        }

        public Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest) => throw new NotSupportedException();

        public Task EditCharacter(EditCharacterRequest editCharacterRequest) => throw new NotSupportedException();

        public Task SetFields(CharacterIdentification characterId, FieldLayerContainer fieldsToSet) => throw new NotSupportedException();
    }

    /// <summary>Из всего репозитория проектов возврат формы использует только загрузку группы.</summary>
    private sealed class FakeProjectRepository(MockedProject mock) : IProjectRepository
    {
        public Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId)
            => Task.FromResult<CharacterGroup?>(
                mock.Project.CharacterGroups.SingleOrDefault(g => g.CharacterGroupId == characterGroupId.CharacterGroupId));

        public void Dispose() { }

        public Task<Project> GetProjectAsync(int project) => throw new NotSupportedException();

        public Task<Project?> GetProjectWithFieldsAsync(int project) => throw new NotSupportedException();

        public Task<Project> GetProjectForMarkdownRendering(ProjectIdentification projectId) => throw new NotSupportedException();

        public Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId = null) => throw new NotSupportedException();

        public Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds) => throw new NotSupportedException();

        public Task<Project> GetProjectWithFinances(int projectid) => throw new NotSupportedException();

        public Task<Project> GetProjectForFinanceSetup(int projectid) => throw new NotSupportedException();

        public Task<ICollection<Character>> GetCharacterByGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(DateTime inActiveSince) => throw new NotSupportedException();

        public Task<ProjectPersonalizedInfo[]> GetPersonalizedProjectsBySpecification(PersonalizedProjectListSpecification projectListSpecification) => throw new NotSupportedException();

        public Task<ProjectShortInfo[]> GetProjectsBySpecification(ProjectListSpecification projectListSpecification) => throw new NotSupportedException();

        public Task<ProjectPersonalizedInfo[]> GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenForHotRoleAdvertisement() => throw new NotSupportedException();

        public Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenedForClaimsInLastWeek() => throw new NotSupportedException();
    }
}
