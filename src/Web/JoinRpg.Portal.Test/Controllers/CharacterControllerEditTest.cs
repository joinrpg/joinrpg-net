using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Portal.Controllers;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Web.Models.Characters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Регрессия #5323: при попытке сделать персонажа публичным (частый случай — скрытый персонаж
/// без обычных групп) POST приходил с пустым списком групп. Проверка IValidatableObject в модели
/// не срабатывала (AllowToSetGroups при POST не заполняется, только в Fill на GET), сервис
/// бросал JoinValidationException, и пользователь видел сырой текст исключения вместо сохранения.
/// </summary>
public class CharacterControllerEditTest
{
    [Fact]
    public async Task EmptyGroups_WhenProjectRequiresGroups_DoesNotCallService()
    {
        var mock = new MockedProject();
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();
        var characterService = new RecordingCharacterService();
        var controller = CreateController(mock, characterService);

        _ = await controller.Edit(CreateViewModel(mock, character, groups: []));

        characterService.EditCharacterCalls.ShouldBe(0);
    }

    [Fact]
    public async Task EmptyGroups_WhenProjectRequiresGroups_ReturnsFormWithFriendlyError()
    {
        var mock = new MockedProject();
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();
        var controller = CreateController(mock, new RecordingCharacterService());

        var result = await controller.Edit(CreateViewModel(mock, character, groups: []));

        result.ShouldBeOfType<ViewResult>().Model.ShouldBeOfType<EditCharacterViewModel>();
        controller.ModelState.IsValid.ShouldBeFalse();
        controller.ModelState[nameof(EditCharacterViewModel.ParentCharacterGroupIds)]
            .ShouldNotBeNull()
            .Errors.ShouldContain(e => e.ErrorMessage == CharacterViewModelBase.GroupsRequiredErrorMessage);
    }

    [Fact]
    public async Task ValidGroups_CallsService()
    {
        var mock = new MockedProject();
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();
        var characterService = new RecordingCharacterService();
        var controller = CreateController(mock, characterService);

        _ = await controller.Edit(CreateViewModel(mock, character, groups: [mock.Group.GetId()]));

        characterService.EditCharacterCalls.ShouldBe(1);
    }

    private static EditCharacterViewModel CreateViewModel(
        MockedProject mock, Character character, CharacterGroupIdentification[] groups)
        => new()
        {
            ProjectId = mock.Project.ProjectId,
            CharacterId = character.CharacterId,
            CharacterTypeInfo = CharacterTypeInfo.Default(),
            ParentCharacterGroupIds = groups,
        };

    private static CharacterController CreateController(MockedProject mock, ICharacterService characterService)
        // Остальные зависимости на пути POST Edit не используются.
        => new(
            projectRepository: null!,
            characterRepository: new FakeCharacterRepository(mock),
            characterInfoRepository: new FakeCharacterInfoRepository(mock),
            characterService: characterService,
            projectMetadataRepository: new FakeProjectMetadataRepository(mock),
            currentUser: new FakeCurrentUserAccessor(mock.Master.UserId),
            userRepository: new FakeUserRepository(mock),
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
        public int EditCharacterCalls { get; private set; }

        public Task EditCharacter(EditCharacterRequest editCharacterRequest)
        {
            EditCharacterCalls++;
            return Task.CompletedTask;
        }

        public Task<CharacterIdentification> AddCharacter(AddCharacterRequest addCharacterRequest) => throw new NotSupportedException();

        public Task DeleteCharacter(DeleteCharacterRequest deleteCharacterRequest) => throw new NotSupportedException();

        public Task SetFields(CharacterIdentification characterId, FieldLayerContainer fieldsToSet) => throw new NotSupportedException();
    }

    /// <summary>Из всего репозитория персонажей возврат формы использует только загрузку персонажа.</summary>
    private sealed class FakeCharacterRepository(MockedProject mock) : ICharacterRepository
    {
        public Task<Character> GetCharacterAsync(int projectId, int characterId)
            => Task.FromResult(mock.Project.Characters.Single(c => c.ProjectId == projectId && c.CharacterId == characterId));

        public void Dispose() { }

        public Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(int projectId, DateTime? modifiedSince) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<Character>> GetCharacters(IReadOnlyCollection<CharacterIdentification> characterIds) => throw new NotSupportedException();

        public Task<Character> GetCharacterAsync(CharacterIdentification characterId) => throw new NotSupportedException();

        public Task<Character> GetCharacterWithGroups(int projectId, int characterId) => throw new NotSupportedException();

        public Task<Character> GetCharacterWithDetails(int projectId, int characterId) => throw new NotSupportedException();

        public Task<IEnumerable<Character>> GetAllCharacters(int projectId) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(ProjectIdentification projectId) => throw new NotSupportedException();
    }
}
