using Bunit;
using JoinRpg.Common.WebComponents;
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
        _ = Services.AddJoinRpgWebComponents();
        Services.AddSingleton<IProjectFieldUriLocator>(new NotUsedLocator());
        // TypedSelector читает RendererInfo — без него bUnit падает при рендере.
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
    }

    private const string NameFieldWarning = "похоже на имя персонажа";

    private IRenderedComponent<ProjectFieldSettingsPanel> Render(ProjectFieldIdentification? nameField, bool hasEditAccess = true)
    {
        client.Settings = new ProjectFieldSettingsViewModel
        {
            ProjectId = ProjectId,
            HasEditAccess = hasEditAccess,
            NameField = nameField,
            DescriptionField = null,
            NameFieldCandidates = ["Имя персонажа"],
        };
        return Render<ProjectFieldSettingsPanel>(p => p.Add(c => c.ProjectId, ProjectId));
    }

    private static string Text(IRenderedComponent<ProjectFieldSettingsPanel> cut)
        => string.Concat(cut.Nodes.Select(n => n.TextContent));

    [Fact]
    public void NameFieldNotSelected_Warns()
        => Text(Render(nameField: null)).ShouldContain($"Поле «Имя персонажа» {NameFieldWarning}", Case.Sensitive);

    [Fact]
    public void NameFieldSelected_NoWarning()
        => Text(Render(NameFieldId)).ShouldNotContain(NameFieldWarning);

    // Панель стоит на странице настроек проекта, которую видит любой мастер; менять поля можно только с правом.
    [Fact]
    public void WithoutEditAccess_ReadOnly()
    {
        var cut = Render(NameFieldId, hasEditAccess: false);

        cut.FindAll("button[type=submit]").ShouldBeEmpty();
        cut.Find("fieldset").HasAttribute("disabled").ShouldBeTrue();
        Text(cut).ShouldContain("«Настраивать поля персонажа»");
    }

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
