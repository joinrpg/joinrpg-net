using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Projects.Metadata;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.Test.Projects;

public class FieldSetupServiceTest : ProjectMetadataServiceTestBase
{
    private FieldSetupServiceImpl CreateService(int? currentUserId = null, bool isAdmin = false)
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        return new FieldSetupServiceImpl(CreatePropsService(currentUser));
    }

    private CreateFieldRequest CreateFieldRequest(
        ProjectFieldType fieldType = ProjectFieldType.String,
        string name = "Новое поле")
        => new(
            ProjectId,
            fieldType,
            name,
            fieldHint: "",
            canPlayerEdit: true,
            canPlayerView: true,
            isPublic: false,
            FieldBoundTo.Character,
            MandatoryStatus.Optional,
            showForGroups: [],
            validForNpc: true,
            includeInPrint: true,
            showForUnapprovedClaims: true,
            price: 0,
            masterFieldHint: "",
            programmaticValue: null);

    [Fact]
    public async Task AddField_ByMaster_AddsFieldAndKeepsProjectInfoConsistent()
    {
        var service = CreateService(mock.Master.UserId);

        var result = await service.AddField(CreateFieldRequest(name: "Поле мастера"));

        result.ProjectId.ShouldBe(ProjectId);
        mock.Project.ProjectFields.ShouldContain(f => f.FieldName == "Поле мастера");
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        // Пересобранный ProjectInfo положен в кэш и содержит новое поле.
        Result.UnsortedFields.ShouldContain(f => f.Name == "Поле мастера");
    }

    [Fact]
    public async Task AddField_ByPlayerWithoutAccess_Throws_AndDoesNotSave()
    {
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.AddField(CreateFieldRequest()));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task AddField_OnInactiveProject_Throws()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<ProjectDeactivatedException>(
            () => service.AddField(CreateFieldRequest()));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Специальных полей расписания — время, место, ведущий (#4512) — в проекте не больше одного
    /// каждого типа.
    /// </summary>
    [Theory]
    [InlineData(ProjectFieldType.ScheduleTimeSlotField)]
    [InlineData(ProjectFieldType.ScheduleRoomField)]
    [InlineData(ProjectFieldType.ScheduleAuthorField)]
    public async Task AddField_DuplicateScheduleField_Throws(ProjectFieldType fieldType)
    {
        _ = mock.AddField(f => f.FieldType = fieldType);
        var service = CreateService(mock.Master.UserId);

        await Should.ThrowAsync<JoinFieldScheduleShouldBeUniqueException>(
            () => service.AddField(CreateFieldRequest(fieldType)));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Поле-ведущий не мешает завести время и место: уникальность считается по типу поля,
    /// а не по «специальности» вообще.
    /// </summary>
    [Fact]
    public async Task AddField_ScheduleFieldOfOtherType_Succeeds()
    {
        _ = mock.AddField(f => f.FieldType = ProjectFieldType.ScheduleAuthorField);
        var service = CreateService(mock.Master.UserId);

        var result = await service.AddField(CreateFieldRequest(ProjectFieldType.ScheduleTimeSlotField));

        Result.TimeSlotField.ShouldNotBeNull().Id.ShouldBe(result);
    }

    /// <summary>
    /// Поле-ссылка на пользователя настраивается как любое другое: право игрока его заполнять
    /// решает мастер проекта галочкой, отдельного запрета нет (ADR017 §3).
    /// </summary>
    [Theory]
    [InlineData(ProjectFieldType.UserLink)]
    [InlineData(ProjectFieldType.MultiUserLink)]
    [InlineData(ProjectFieldType.ScheduleAuthorField)]
    public async Task AddField_UserLink_RespectsCanPlayerEdit(ProjectFieldType fieldType)
    {
        var service = CreateService(mock.Master.UserId);

        var result = await service.AddField(
            CreateFieldRequest(fieldType, name: "Куратор роли"));

        Result.UnsortedFields.Single(f => f.Id == result).CanPlayerEdit.ShouldBeTrue();
    }

    [Fact]
    public async Task MoveFieldVariantAfter_MovesVariantAndReturnsNewOrder()
    {
        var dropdownField = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 100,
                    Label = "Вариант 1",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 101,
                    Label = "Вариант 2",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 102,
                    Label = "Вариант 3",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
            ];
        });
        var service = CreateService(mock.Master.UserId);

        var newOrder = await service.MoveFieldVariantAfter(
            new ProjectFieldVariantIdentification(dropdownField.Id, 102),
            new ProjectFieldVariantIdentification(dropdownField.Id, 100));

        newOrder.Select(id => id.ProjectFieldVariantId).ShouldBe([100, 102, 101]);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    private CharacterGroup AddSpecialGroup(ProjectField field, string name)
    {
        var group = mock.CreateCharacterGroup();
        group.IsSpecial = true;
        group.CharacterGroupName = name;
        group.ParentCharacterGroupIds = [mock.Project.RootGroup.CharacterGroupId];
        field.CharacterGroup = group;
        return group;
    }

    private CharacterGroup AddSpecialGroup(ProjectFieldDropdownValue variant, CharacterGroup parentGroup, string name)
    {
        var group = mock.CreateCharacterGroup();
        group.IsSpecial = true;
        group.CharacterGroupName = name;
        group.ParentCharacterGroupIds = [parentGroup.CharacterGroupId];
        variant.CharacterGroup = group;
        return group;
    }

    [Fact]
    public async Task MoveField_WithSpecialGroup_SyncsRootGroupOrdering()
    {
        var field1 = mock.AddField(f => f.FieldType = ProjectFieldType.Dropdown);
        var group1 = AddSpecialGroup(mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field1.Id.ProjectFieldId), "Поле 1");
        var field2 = mock.AddField(f => f.FieldType = ProjectFieldType.Dropdown);
        var group2 = AddSpecialGroup(mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field2.Id.ProjectFieldId), "Поле 2");

        mock.Project.Details.FieldsOrdering = $"{field1.Id.ProjectFieldId},{field2.Id.ProjectFieldId}";
        mock.Project.RootGroup.ChildGroupsOrdering = $"{group1.CharacterGroupId},{group2.CharacterGroupId}";

        var service = CreateService(mock.Master.UserId);

        await service.MoveField(ProjectId.Value, field1.Id.ProjectFieldId, direction: 1);

        mock.Project.RootGroup.GetCharacterGroupsContainer().OrderedItems
            .Select(g => g.CharacterGroupId)
            .ShouldBe([group2.CharacterGroupId, group1.CharacterGroupId]);
    }

    [Fact]
    public async Task MoveFieldAfter_WithSpecialGroup_SyncsRootGroupOrdering()
    {
        var field1 = mock.AddField(f => f.FieldType = ProjectFieldType.Dropdown);
        var group1 = AddSpecialGroup(mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field1.Id.ProjectFieldId), "Поле 1");
        var field2 = mock.AddField(f => f.FieldType = ProjectFieldType.Dropdown);
        var group2 = AddSpecialGroup(mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field2.Id.ProjectFieldId), "Поле 2");
        var field3 = mock.AddField(f => f.FieldType = ProjectFieldType.Dropdown);
        var group3 = AddSpecialGroup(mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field3.Id.ProjectFieldId), "Поле 3");

        mock.Project.Details.FieldsOrdering = $"{field1.Id.ProjectFieldId},{field2.Id.ProjectFieldId},{field3.Id.ProjectFieldId}";
        mock.Project.RootGroup.ChildGroupsOrdering = $"{group1.CharacterGroupId},{group2.CharacterGroupId},{group3.CharacterGroupId}";

        var service = CreateService(mock.Master.UserId);

        await service.MoveFieldAfter(ProjectId.Value, field3.Id.ProjectFieldId, afterFieldId: field1.Id.ProjectFieldId);

        mock.Project.RootGroup.GetCharacterGroupsContainer().OrderedItems
            .Select(g => g.CharacterGroupId)
            .ShouldBe([group1.CharacterGroupId, group3.CharacterGroupId, group2.CharacterGroupId]);
    }

    [Fact]
    public async Task MoveFieldVariantAfter_WithSpecialGroup_SyncsFieldGroupOrdering()
    {
        var fieldInfo = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 100,
                    Label = "Вариант 1",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 101,
                    Label = "Вариант 2",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
            ];
        });
        var field = mock.Project.ProjectFields.Single(f => f.ProjectFieldId == fieldInfo.Id.ProjectFieldId);
        var fieldGroup = AddSpecialGroup(field, "Поле");
        var variant1 = field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == 100);
        var variant2 = field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == 101);
        var variantGroup1 = AddSpecialGroup(variant1, fieldGroup, "Вариант 1");
        var variantGroup2 = AddSpecialGroup(variant2, fieldGroup, "Вариант 2");

        fieldGroup.ChildGroupsOrdering = $"{variantGroup1.CharacterGroupId},{variantGroup2.CharacterGroupId}";

        var service = CreateService(mock.Master.UserId);

        _ = await service.MoveFieldVariantAfter(
            new ProjectFieldVariantIdentification(fieldInfo.Id, 100),
            new ProjectFieldVariantIdentification(fieldInfo.Id, 101));

        fieldGroup.GetCharacterGroupsContainer().OrderedItems
            .Select(g => g.CharacterGroupId)
            .ShouldBe([variantGroup2.CharacterGroupId, variantGroup1.CharacterGroupId]);
    }

    [Fact]
    public async Task SortFieldVariants_WithSpecialGroups_SortsGroupsByName()
    {
        var fieldInfo = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.DropdownValues =
            [
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 100,
                    Label = "Zebra",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
                new ProjectFieldDropdownValue
                {
                    ProjectFieldDropdownValueId = 101,
                    Label = "Alpha",
                    IsActive = true,
                    Description = new MarkdownDbValue(),
                    MasterDescription = new MarkdownDbValue(),
                },
            ];
        });
        var field = mock.Project.ProjectFields.Single(f => f.ProjectFieldId == fieldInfo.Id.ProjectFieldId);
        var fieldGroup = AddSpecialGroup(field, "Поле");
        var variantZebra = field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == 100);
        var variantAlpha = field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == 101);
        var groupZebra = AddSpecialGroup(variantZebra, fieldGroup, "Zebra");
        var groupAlpha = AddSpecialGroup(variantAlpha, fieldGroup, "Alpha");

        field.ValuesOrdering = $"{variantZebra.ProjectFieldDropdownValueId},{variantAlpha.ProjectFieldDropdownValueId}";
        fieldGroup.ChildGroupsOrdering = $"{groupZebra.CharacterGroupId},{groupAlpha.CharacterGroupId}";

        var service = CreateService(mock.Master.UserId);

        await service.SortFieldVariants(ProjectId.Value, fieldInfo.Id.ProjectFieldId);

        fieldGroup.GetCharacterGroupsContainer().OrderedItems
            .Select(g => g.CharacterGroupId)
            .ShouldBe([groupAlpha.CharacterGroupId, groupZebra.CharacterGroupId]);
    }

    private static ProjectFieldDropdownValue CreateVariant(int id, string label, bool wasEverUsed, bool isActive = true)
        => new()
        {
            ProjectFieldDropdownValueId = id,
            Label = label,
            IsActive = isActive,
            WasEverUsed = wasEverUsed,
            Description = new MarkdownDbValue(),
            MasterDescription = new MarkdownDbValue(),
        };

    [Fact]
    public async Task DeleteUnusedFieldValueVariants_DeletesOnlyNeverUsed()
    {
        var fieldInfo = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.DropdownValues =
            [
                CreateVariant(100, "Неиспользованное 1", wasEverUsed: false),
                CreateVariant(101, "Использованное", wasEverUsed: true),
                CreateVariant(102, "Неиспользованное 2", wasEverUsed: false),
                CreateVariant(103, "Выключенное", wasEverUsed: true, isActive: false),
                // Выключенное неиспользованное — не «живое» значение, массовое удаление его не трогает.
                CreateVariant(104, "Выключенное неиспользованное", wasEverUsed: false, isActive: false),
            ];
        });
        var field = mock.Project.ProjectFields.Single(f => f.ProjectFieldId == fieldInfo.Id.ProjectFieldId);
        var fieldGroup = AddSpecialGroup(field, "Поле");
        var unusedGroup = AddSpecialGroup(field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == 100), fieldGroup, "Неиспользованное 1");
        var usedGroup = AddSpecialGroup(field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == 101), fieldGroup, "Использованное");
        var service = CreateService(mock.Master.UserId);

        var deleted = await service.DeleteUnusedFieldValueVariants(fieldInfo.Id);

        deleted.ShouldBe(2);
        field.DropdownValues.Select(v => v.ProjectFieldDropdownValueId).ShouldBe([101, 103, 104], ignoreOrder: true);
        field.DropdownValues.Single(v => v.ProjectFieldDropdownValueId == 101).IsActive.ShouldBeTrue();
        mock.Project.CharacterGroups.ShouldNotContain(unusedGroup);
        mock.Project.CharacterGroups.ShouldContain(usedGroup);
        usedGroup.IsActive.ShouldBeTrue();
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
        Result.UnsortedFields.Single(f => f.Id == fieldInfo.Id).Variants
            .Select(v => v.Id.ProjectFieldVariantId)
            .ShouldBe([101, 103, 104], ignoreOrder: true);
    }

    [Fact]
    public async Task DeleteUnusedFieldValueVariants_AllUsed_DeletesNothing()
    {
        var fieldInfo = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.DropdownValues = [CreateVariant(100, "Использованное", wasEverUsed: true)];
        });
        var field = mock.Project.ProjectFields.Single(f => f.ProjectFieldId == fieldInfo.Id.ProjectFieldId);
        var service = CreateService(mock.Master.UserId);

        var deleted = await service.DeleteUnusedFieldValueVariants(fieldInfo.Id);

        deleted.ShouldBe(0);
        field.DropdownValues.Single().IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteUnusedFieldValueVariants_ByPlayer_Throws_AndDoesNotSave()
    {
        var fieldInfo = mock.AddField(f =>
        {
            f.FieldType = ProjectFieldType.Dropdown;
            f.DropdownValues = [CreateVariant(100, "Неиспользованное", wasEverUsed: false)];
        });
        var service = CreateService(mock.Player.UserId);

        await Should.ThrowAsync<NoAccessToProjectException>(
            () => service.DeleteUnusedFieldValueVariants(fieldInfo.Id));

        unitOfWork.SaveChangesCallCount.ShouldBe(0);
        mock.Project.ProjectFields.Single(f => f.ProjectFieldId == fieldInfo.Id.ProjectFieldId)
            .DropdownValues.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task MoveField_WithoutSpecialGroup_DoesNotThrow()
    {
        var field1 = mock.AddField(f => f.FieldType = ProjectFieldType.String);
        var field2 = mock.AddField(f => f.FieldType = ProjectFieldType.String);
        mock.Project.Details.FieldsOrdering = $"{field1.Id.ProjectFieldId},{field2.Id.ProjectFieldId}";

        var service = CreateService(mock.Master.UserId);

        await Should.NotThrowAsync(() => service.MoveField(ProjectId.Value, field1.Id.ProjectFieldId, direction: 1));
    }
}
