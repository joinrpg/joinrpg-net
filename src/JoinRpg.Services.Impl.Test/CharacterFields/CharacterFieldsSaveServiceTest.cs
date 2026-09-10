using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.CharacterFields;
using JoinRpg.Services.Impl.Test.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.CharacterFields;

/// <summary>
/// Отметка полей как использованных (<see cref="ProjectField.WasEverUsed"/>) — изменение
/// метаданных проекта, поэтому идёт через <c>IProjectPropsService</c> (ADR009). Проверяем, что
/// отметка ставится, попадает в пересобранный <see cref="ProjectInfo"/>, доступна игроку без
/// мастерских прав и не приводит к записи, когда менять нечего.
/// </summary>
public class CharacterFieldsSaveServiceTest : ProjectMetadataServiceTestBase
{
    private const int VariantId = 100;

    private CharacterFieldsSaveService CreateService(int currentUserId)
        => new(
            new FieldSaveHelper(new FieldSaveHelperFakes.NoDefaults(), NullLogger<FieldSaveHelper>.Instance),
            CreatePropsService(CreateCurrentUser(currentUserId)));

    private ProjectFieldInfo AddPlayerEditableField()
        => mock.AddField(f =>
        {
            f.FieldName = "Поле игрока";
            f.FieldType = ProjectFieldType.String;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.CanPlayerEdit = true;
            f.CanPlayerView = true;
            f.ShowOnUnApprovedClaims = true;
            f.Description = new MarkdownDbValue();
            f.MasterDescription = new MarkdownDbValue();
        });

    private ProjectFieldInfo AddDropdownField()
        => mock.AddField(f =>
        {
            f.FieldName = "Поле с вариантами";
            f.FieldType = ProjectFieldType.Dropdown;
            f.FieldBoundTo = FieldBoundTo.Character;
            f.CanPlayerEdit = true;
            f.CanPlayerView = true;
            f.ShowOnUnApprovedClaims = true;
            f.Description = new MarkdownDbValue();
            f.MasterDescription = new MarkdownDbValue();
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = VariantId,
                    Label = "Вариант 1",
                    IsActive = true,
                    PlayerSelectable = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
            ];
        });

    private Task<IReadOnlyCollection<FieldWithPreviousAndNewValue>> Save(
        int currentUserId,
        ProjectFieldInfo field,
        string? value)
        => CreateService(currentUserId).SaveCharacterFields(
            currentUserId,
            mock.Character,
            new FieldLayerContainer(
                mock.ProjectInfo,
                new Dictionary<int, string?> { { field.Id.ProjectFieldId, value } }),
            mock.ProjectInfo);

    private ProjectField Entity(ProjectFieldInfo field)
        => mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field.Id.ProjectFieldId);

    [Fact]
    public async Task FirstFill_MarksFieldAsUsed()
    {
        var field = AddPlayerEditableField();
        Entity(field).WasEverUsed.ShouldBeFalse();

        _ = await Save(mock.Master.UserId, field, "какое-то значение");

        Entity(field).WasEverUsed.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task FirstFill_MarkIsVisibleInRebuiltProjectInfo()
    {
        var field = AddPlayerEditableField();

        _ = await Save(mock.Master.UserId, field, "какое-то значение");

        // Снимок, который ProjectPropsService положил в кэш, уже содержит отметку.
        Result.GetFieldById(field.Id).WasEverUsed.ShouldBeTrue();
    }

    [Fact]
    public async Task AlreadyMarkedField_DoesNotTouchMetadata()
    {
        var field = AddPlayerEditableField();
        Entity(field).WasEverUsed = true;
        mock.ReInitProjectInfo();

        _ = await Save(mock.Master.UserId, field, "какое-то значение");

        // Отмечать нечего — в БД не идём вовсе, это самый горячий путь записи.
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task PlayerWithoutMasterAccess_CanMarkFieldAsUsed()
    {
        var field = AddPlayerEditableField();
        _ = mock.CreateApprovedClaim(mock.Character, mock.Player);
        mock.ReInitProjectInfo();

        // Отметку ставит тот, кто заполнил поле. Мастерских прав у игрока нет, и требовать их
        // здесь нельзя — иначе сохранение своей заявки падало бы с NoAccessToProjectException.
        _ = await Save(mock.Player.UserId, field, "значение от игрока");

        Entity(field).WasEverUsed.ShouldBeTrue();
    }

    [Fact]
    public async Task FirstFill_MarksSelectedVariantAsUsed()
    {
        var field = AddDropdownField();
        var variant = Entity(field).DropdownValues.Single();
        variant.WasEverUsed.ShouldBeFalse();

        _ = await Save(mock.Master.UserId, field, VariantId.ToString());

        Entity(field).WasEverUsed.ShouldBeTrue();
        Entity(field).DropdownValues.Single().WasEverUsed.ShouldBeTrue();
    }
}

internal static class FieldSaveHelperFakes
{
    /// <summary>Генератор значений по умолчанию, который ничего не подставляет.</summary>
    internal sealed class NoDefaults : IFieldDefaultValueGenerator
    {
        public string? CreateDefaultValue(Claim? claim, FieldWithValue field) => null;
        public string? CreateDefaultValue(Character? character, FieldWithValue field) => null;
    }
}
