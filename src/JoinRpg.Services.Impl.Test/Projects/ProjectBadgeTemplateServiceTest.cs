using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Services.Impl.Projects.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Projects;

public class ProjectBadgeTemplateServiceTest : ProjectMetadataServiceTestBase
{
    private ProjectBadgeTemplateService CreateService()
        => new(CreatePropsService(CreateCurrentUser()), NullLogger<ProjectBadgeTemplateService>.Instance);

    private static BadgeLine FieldLine(int fieldId) => new(fieldId, null, null, TopMm: 10, MinFontPt: 8, MaxFontPt: 20, Bold: false);

    private static BadgeLayout Layout(params BadgeLine[] lines) => new(90, 55, "https://example.com/bg.png", lines);

    private DataModel.ProjectField FieldEntity(ProjectFieldInfo field)
        => mock.Project.ProjectFields.Single(f => f.ProjectFieldId == field.Id.ProjectFieldId);

    [Fact]
    public async Task Create_StoresTemplateAndMarksFieldsUsed()
    {
        var field = AddStringField();
        FieldEntity(field).WasEverUsed = false;
        var layout = Layout(FieldLine(field.Id.ProjectFieldId), new BadgeLine(null, "Каэр Морхен", null, 40, 8, 12, true));

        await CreateService().CreateAsync(ProjectId, "Игрок", layout);

        var entity = mock.Project.ProjectBadgeTemplates.ShouldHaveSingleItem();
        entity.Name.ShouldBe("Игрок");
        entity.IsActive.ShouldBeTrue();
        entity.CreatedById.ShouldBe(mock.Master.UserId);
        BadgeLinesJson.Deserialize(entity.LinesJson).ShouldBe(layout.Lines);
        FieldEntity(field).WasEverUsed.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_WithUnknownField_Throws()
    {
        await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().CreateAsync(ProjectId, "Игрок", Layout(FieldLine(100500))));

        mock.Project.ProjectBadgeTemplates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_WithInvalidLayout_Throws()
    {
        var tooWide = Layout() with { WidthMm = BadgeLayoutRules.MaxWidthMm + 1 };

        await Should.ThrowAsync<BadgeTemplateInvalidException>(() => CreateService().CreateAsync(ProjectId, "Игрок", tooWide));
    }

    [Fact]
    public async Task Create_EmptyBackground_StoredAsNull()
    {
        await CreateService().CreateAsync(ProjectId, "Игрок", Layout() with { BackgroundUrl = " " });

        mock.Project.ProjectBadgeTemplates.ShouldHaveSingleItem().BackgroundUrl.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAndRestore_ToggleIsActive()
    {
        var id = await CreateService().CreateAsync(ProjectId, "Игрок", Layout());
        var entity = mock.Project.ProjectBadgeTemplates.Single();

        await CreateService().RemoveAsync(id);
        entity.IsActive.ShouldBeFalse();

        await CreateService().RestoreAsync(id);
        entity.IsActive.ShouldBeTrue();
    }

    private ProjectFieldInfo AddStringField(bool isActive = true) => mock.AddField(f =>
    {
        f.FieldName = "Имя на бейдже";
        f.FieldType = ProjectFieldType.String;
        f.FieldBoundTo = FieldBoundTo.Character;
        f.IsActive = isActive;
    });

    [Fact]
    public async Task Create_WithInactiveField_Throws()
    {
        var field = AddStringField(isActive: false);

        await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().CreateAsync(ProjectId, "Игрок", Layout(FieldLine(field.Id.ProjectFieldId))));
    }

    // Поле выключили, когда оно уже стояло в бейдже, — шаблон всё равно можно пересохранить.
    [Fact]
    public async Task Update_KeepsLineWithFieldDeactivatedLater()
    {
        var field = AddStringField();
        var layout = Layout(FieldLine(field.Id.ProjectFieldId));
        var id = await CreateService().CreateAsync(ProjectId, "Игрок", layout);
        FieldEntity(field).IsActive = false;

        await CreateService().UpdateAsync(id, "Игрок (новый)", layout);

        mock.Project.ProjectBadgeTemplates.ShouldHaveSingleItem().Name.ShouldBe("Игрок (новый)");
    }

    [Fact]
    public async Task Update_MarksNewFieldsUsedAndAudits()
    {
        var id = await CreateService().CreateAsync(ProjectId, "Игрок", Layout());
        var entity = mock.Project.ProjectBadgeTemplates.Single();
        entity.UpdatedAt = DateTime.MinValue;
        var field = AddStringField();
        FieldEntity(field).WasEverUsed = false;

        await CreateService().UpdateAsync(id, "Игрок", Layout(FieldLine(field.Id.ProjectFieldId)));

        FieldEntity(field).WasEverUsed.ShouldBeTrue();
        entity.UpdatedAt.ShouldNotBe(DateTime.MinValue);
        entity.UpdatedById.ShouldBe(mock.Master.UserId);
    }

    [Fact]
    public async Task Update_UnknownTemplate_Throws()
        => await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().UpdateAsync(new ProjectBadgeTemplateIdentification(ProjectId, 100500), "Игрок", Layout()));

    [Fact]
    public async Task Create_TrimsNameAndBackground()
    {
        var longName = new string('я', BadgeLayoutRules.MaxNameLength);

        await CreateService().CreateAsync(ProjectId, $"  {longName}  ", Layout() with { BackgroundUrl = " https://example.com/bg.png " });

        var entity = mock.Project.ProjectBadgeTemplates.ShouldHaveSingleItem();
        entity.Name.ShouldBe(longName);
        entity.BackgroundUrl.ShouldBe("https://example.com/bg.png");
    }

    [Fact]
    public async Task Update_ChangesLayout()
    {
        var id = await CreateService().CreateAsync(ProjectId, "Игрок", Layout());

        await CreateService().UpdateAsync(id, "Мастер", Layout() with { WidthMm = 100 });

        var entity = mock.Project.ProjectBadgeTemplates.ShouldHaveSingleItem();
        entity.Name.ShouldBe("Мастер");
        entity.WidthMm.ShouldBe(100);
    }
}
