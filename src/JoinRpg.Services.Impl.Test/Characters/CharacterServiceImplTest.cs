using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Characters;

namespace JoinRpg.Services.Impl.Test.Characters;

/// <summary>
/// Первые юнит-тесты <see cref="CharacterServiceImpl"/> — до миграции на
/// <see cref="ICharacterPropsService"/> (ADR014) их не было вовсе, страховкой служили только
/// интеграционные сценарии на настоящей БД.
/// </summary>
public class CharacterServiceImplTest : Claims.ClaimServiceTestBase
{
    private CharacterServiceImpl CreateService(int? userId = null)
        => new(CreatePropsService(userId), CreateUserFieldValidator());

    private void ArchiveProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
    }

    private AddCharacterRequest AddRequest()
        => new(
            mock.ProjectInfo.ProjectId,
            ParentCharacterGroupIds: [mock.Group.GetId()],
            CharacterTypeInfo.Default(),
            FieldLayerContainer.Empty(mock.ProjectInfo));

    private EditCharacterRequest EditRequest(
        Character character,
        CharacterTypeInfo? typeInfo = null,
        Dictionary<int, string?>? fieldValues = null)
        => new(
            character.GetId(),
            ParentCharacterGroupIds: [mock.Group.GetId()],
            typeInfo ?? CharacterTypeInfo.Default(),
            fieldValues is null
                ? FieldLayerContainer.Empty(mock.ProjectInfo)
                : new FieldLayerContainer(mock.ProjectInfo, fieldValues));

    /// <summary>
    /// Конфигурация «поле B доступно тем, кто выбрал вариант X поля A»: персонаж выбрал вариант и
    /// потому лежит в его спецгруппе, а поле B ограничено этой спецгруппой и обязательно.
    /// </summary>
    private (Character Character, CharacterGroup SpecialGroup, ProjectFieldInfo RestrictedField) SetupCharacterInVariantSpecialGroup()
    {
        var specialGroup = mock.CreateSpecialGroup();
        var character = mock.CreateCharacter("Вася");
        MockedProject.AddCharToGroup(character, specialGroup);

        var (partyField, partyVariant) = mock.AddDropdownFieldWithSpecialGroup("Партия", specialGroup);
        var restrictedField = mock.AddField(f =>
        {
            f.FieldName = "Только для партии";
            f.MandatoryStatus = MandatoryStatus.Required;
            f.CanPlayerView = true;
            f.ValidForNpc = true;
            f.AvailableForCharacterGroupIds = [specialGroup.CharacterGroupId];
        });

        MockedProject.AssignFieldValues(
            character,
            new FieldWithValue(partyField, partyVariant.Id.ProjectFieldVariantId.ToString()));

        return (character, specialGroup, restrictedField);
    }

    [Fact]
    public async Task AddCharacter_SavesOnce_AndReturnsId()
    {
        var before = mock.Project.Characters.Count;

        var id = await CreateService().AddCharacter(AddRequest());

        mock.Project.Characters.Count.ShouldBe(before + 1);
        SaveChangesCallCount.ShouldBe(1);
        id.ProjectId.ShouldBe(mock.ProjectInfo.ProjectId);
    }

    [Fact]
    public async Task AddCharacter_WithoutRights_Throws_AndDoesNotSave()
    {
        await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).AddCharacter(AddRequest()));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddCharacter_OnArchivedProject_Throws()
    {
        ArchiveProject();

        await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().AddCharacter(AddRequest()));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task EditCharacter_SavesOnce_AndMarksChanged()
    {
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();

        await CreateService().EditCharacter(EditRequest(character));

        SaveChangesCallCount.ShouldBe(1);
        character.UpdatedById.ShouldBe(mock.Master.UserId);
    }

    /// <summary>
    /// Регрессия #4937: правка персонажа переписывала группы выбранными мастером — то есть только
    /// обычными, — и на время сохранения персонаж лишался спецгрупп. По ним считается доступность
    /// полей, поэтому обязательное поле, ограниченное спецгруппой варианта, в этом сохранении можно
    /// было молча очистить.
    /// </summary>
    [Fact]
    public async Task EditCharacter_ClearingMandatoryFieldRestrictedToSpecialGroup_Throws()
    {
        var (character, _, restrictedField) = SetupCharacterInVariantSpecialGroup();

        await Should.ThrowAsync<CharacterFieldRequiredException>(
            () => CreateService().EditCharacter(EditRequest(
                character,
                fieldValues: new() { { restrictedField.Id.ProjectFieldId, null } })));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Итоговый список групп собирает и пишет само сохранение полей: обычные — выбранные мастером,
    /// спецгруппы — по значениям полей. Сущность до сохранения не трогается, поэтому тест заодно
    /// держит то, что группы вообще доезжают до персонажа.
    /// </summary>
    [Fact]
    public async Task EditCharacter_WritesSelectedGroupsAndKeepsVariantSpecialGroup()
    {
        var (character, specialGroup, _) = SetupCharacterInVariantSpecialGroup();

        await CreateService().EditCharacter(EditRequest(character));

        character.ParentCharacterGroupIds.ShouldBe(
            [mock.Group.CharacterGroupId, specialGroup.CharacterGroupId],
            ignoreOrder: true);
    }

    [Fact]
    public async Task EditCharacter_WithoutRights_Throws_AndDoesNotSave()
    {
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).EditCharacter(EditRequest(character)));

        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task EditCharacter_OnArchivedProject_Throws()
    {
        var character = mock.CreateCharacter("Вася");
        ArchiveProject();

        await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().EditCharacter(EditRequest(character)));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Инвариант «нельзя менять тип персонажа с активными заявками» теперь проверяется по доменному
    /// снимку <see cref="CharacterInfo"/>, а не обходом EF-графа.
    /// </summary>
    [Fact]
    public async Task EditCharacter_ChangingTypeWithActiveClaims_Throws()
    {
        var character = mock.CreateCharacter("Вася");
        mock.CreateClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        var npc = new CharacterTypeInfo(
            CharacterType.NonPlayer, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public);

        await Should.ThrowAsync<Exception>(
            () => CreateService().EditCharacter(EditRequest(character, npc)));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Отметку аудита ставит сам props-сервис, а не вызывающий: операция над персонажем по
    /// определению его меняет. Тест держит это свойство — иначе его легко потерять, молча и
    /// незаметно, при добавлении очередной операции.
    /// </summary>
    [Fact]
    public async Task ChangeCharacter_StampsAudit_EvenWhenLambdaDoesNothing()
    {
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();
        character.UpdatedById = 0;

        await CreatePropsService().ChangeCharacter(
            character.GetId(),
            Permission.CanEditRoles,
            ProjectActiveRequirement.MustBeActive,
            0,
            ctx => { });

        character.UpdatedById.ShouldBe(mock.Master.UserId);
    }

    /// <summary>
    /// Группы новому персонажу проставляет сохранение полей, а не инициализатор сущности.
    /// </summary>
    [Fact]
    public async Task AddCharacter_WritesSelectedGroups()
    {
        var id = await CreateService().AddCharacter(AddRequest());

        var created = mock.Project.Characters.Single(c => c.CharacterId == id.CharacterId);
        created.ParentCharacterGroupIds.ShouldBe([mock.Group.CharacterGroupId]);
    }

    [Fact]
    public async Task AddCharacter_StampsCreationAudit()
    {
        var id = await CreateService().AddCharacter(AddRequest());

        var created = mock.Project.Characters.Single(c => c.CharacterId == id.CharacterId);
        created.CreatedById.ShouldBe(mock.Master.UserId);
        created.UpdatedById.ShouldBe(mock.Master.UserId);
    }

    [Fact]
    public async Task DeleteCharacter_StampsAudit()
    {
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();
        character.UpdatedById = 0;

        await CreateService().DeleteCharacter(new DeleteCharacterRequest(character.GetId()));

        character.UpdatedById.ShouldBe(mock.Master.UserId);
    }

    [Fact]
    public async Task DeleteCharacter_MarksInactive()
    {
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();

        await CreateService().DeleteCharacter(new DeleteCharacterRequest(character.GetId()));

        character.IsActive.ShouldBeFalse();
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Удаление мягкое (<c>IsActive = false</c>), поэтому связи с сюжетами сохраняются. Раньше
    /// здесь стояла очистка под <c>CanBePermanentlyDeleted</c>, но эта ветка была мертва.
    /// </summary>
    [Fact]
    public async Task DeleteCharacter_KeepsPlotLinks()
    {
        var character = mock.CreateCharacter("Вася");
        var plot = new PlotElement
        {
            PlotElementId = 1,
            ProjectId = mock.Project.ProjectId,
            Project = mock.Project,
            TargetCharacters = [character],
        };
        character.DirectlyRelatedPlotElements = [plot];
        mock.ReInitProjectInfo();

        await CreateService().DeleteCharacter(new DeleteCharacterRequest(character.GetId()));

        character.IsActive.ShouldBeFalse();
        character.DirectlyRelatedPlotElements.ShouldContain(plot);
    }

    [Fact]
    public async Task DeleteCharacter_WithActiveClaims_Throws()
    {
        var character = mock.CreateCharacter("Вася");
        mock.CreateClaim(character, mock.Player);
        mock.ReInitProjectInfo();

        await Should.ThrowAsync<Exception>(
            () => CreateService().DeleteCharacter(new DeleteCharacterRequest(character.GetId())));

        character.IsActive.ShouldBeTrue();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Персонажа-шаблон удалять нельзя, и признак берётся из снимка метаданных
    /// (<see cref="ProjectClaimSettings.DefaultTemplate"/>), а не через EF-навигацию (#4987).
    /// </summary>
    [Fact]
    public async Task DeleteCharacter_DefaultTemplate_Throws()
    {
        var character = mock.CreateCharacter("Шаблон");
        mock.Project.Details.DefaultTemplateCharacterId = character.CharacterId;
        mock.ReInitProjectInfo();

        await Should.ThrowAsync<DefaultTemplateCharacterCannotBeDeletedException>(
            () => CreateService().DeleteCharacter(new DeleteCharacterRequest(character.GetId())));

        character.IsActive.ShouldBeTrue();
        SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task SetFields_SavesOnce_AndMarksChanged()
    {
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();

        await CreateService().SetFields(character.GetId(), FieldLayerContainer.Empty(mock.ProjectInfo));

        SaveChangesCallCount.ShouldBe(1);
        character.UpdatedById.ShouldBe(mock.Master.UserId);
    }

    /// <summary>
    /// Кэш метаданных обновляется только тогда, когда операция их действительно тронула. Пустой
    /// слой полей ничего не отмечает, значит и пересобирать <c>ProjectInfo</c> незачем.
    /// </summary>
    [Fact]
    public async Task SetFields_WithoutNewFieldUsage_DoesNotPrimeCache()
    {
        var character = mock.CreateCharacter("Вася");
        mock.ReInitProjectInfo();

        await CreateService().SetFields(character.GetId(), FieldLayerContainer.Empty(mock.ProjectInfo));

        metadataRepository.LastPrimed.ShouldBeNull();
    }
}
