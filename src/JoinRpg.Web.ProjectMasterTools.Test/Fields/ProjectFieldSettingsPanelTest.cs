using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Fields;
using JoinRpg.Web.ProjectMasterTools.Fields;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Fields;

/// <summary>
/// Предупреждение о поле, похожем на имя персонажа, висит, только пока поле-имя не выбрано.
/// </summary>
public class ProjectFieldSettingsPanelTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ProjectFieldIdentification NameFieldId = new(ProjectId, 10);

    private readonly SettingsClient client = new();

    public ProjectFieldSettingsPanelTest()
    {
        Services.AddSingleton<IProjectFieldOperationsClient>(client);
        Services.AddSingleton<IProjectFieldsClient>(new StubFieldsClient());
        Services.AddSingleton<IProjectFieldUriLocator>(new NotUsedLocator());
        // Non-interactive: TypedSelector не пытается импортировать JS-модуль bootstrap-select.
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
    }

    private IRenderedComponent<ProjectFieldSettingsPanel> Render(ProjectFieldIdentification? nameField)
    {
        client.Settings = new ProjectFieldSettingsViewModel
        {
            ProjectId = ProjectId,
            NameField = nameField,
            DescriptionField = null,
            NameFieldCandidates = ["Имя персонажа"],
        };
        return Render<ProjectFieldSettingsPanel>(p => p.Add(c => c.ProjectId, ProjectId));
    }

    [Fact]
    public void NameFieldNotSelected_Warns()
        => Render(nameField: null).Find(".alert-warning").TextContent.ShouldContain("«Имя персонажа»", Case.Sensitive);

    [Fact]
    public void NameFieldSelected_NoWarning()
        => Render(NameFieldId).FindAll(".alert-warning").ShouldBeEmpty();

    private sealed class StubFieldsClient : IProjectFieldsClient
    {
        public Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
            => Task.FromResult<List<ProjectFieldDto>>(
                [new ProjectFieldDto(NameFieldId, "Имя персонажа", ProjectFieldType.String, FieldBoundTo.Character)]);
    }

    private sealed class NotUsedLocator : IProjectFieldUriLocator
    {
        public Uri GetEditUri(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Uri GetCreateVariantUri(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Uri GetEditVariantUri(ProjectFieldVariantIdentification variantId) => throw new NotSupportedException();
        public Uri GetFieldSettingsUri(ProjectIdentification projectId) => throw new NotSupportedException();
    }

    private sealed class SettingsClient : IProjectFieldOperationsClient
    {
        public ProjectFieldSettingsViewModel Settings { get; set; } = null!;

        public Task Delete(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task DeleteVariant(ProjectFieldVariantIdentification variantId) => throw new NotSupportedException();
        public Task DeleteUnusedVariants(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task CreateTimeSlots(TimeSlotMassAddRequest request) => throw new NotSupportedException();
        public Task SortTimeSlotsByStartTime(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task SortVariantsByLabel(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Task CreateVariants(FieldValuesMassAddRequest request) => throw new NotSupportedException();
        public Task<ProjectFieldSettingsViewModel> GetFieldSettings(ProjectIdentification projectId) => Task.FromResult(Settings);
        public Task SaveFieldSettings(ProjectFieldSettingsViewModel model) => throw new NotSupportedException();
    }
}
