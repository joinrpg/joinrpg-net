using JoinRpg.DataModel;
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
    private CharacterServiceImpl CreateService(int? userId = null) => new(CreatePropsService(userId));

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

    private EditCharacterRequest EditRequest(Character character, CharacterTypeInfo? typeInfo = null)
        => new(
            character.GetId(),
            ParentCharacterGroupIds: [mock.Group.GetId()],
            typeInfo ?? CharacterTypeInfo.Default(),
            FieldLayerContainer.Empty(mock.ProjectInfo));

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
