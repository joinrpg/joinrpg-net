using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Web.ProjectCommon.Fields;
using JoinRpg.Web.ProjectMasterTools.Badges;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Badges;

public class BadgeTemplateEditFormTest : BunitContext
{
    private readonly ProjectIdentification projectId = new(123);
    private readonly RecordingClient client = new();
    private readonly ProjectFieldIdentification nameField;

    public BadgeTemplateEditFormTest()
    {
        nameField = new ProjectFieldIdentification(projectId, 7);
        Services.AddSingleton<IBadgeTemplateClient>(client);
        Services.AddSingleton<IBadgeTemplateUriLocator>(new UriLocator());
        Services.AddSingleton<IProjectFieldsClient>(new FieldsClient(new ProjectFieldDto(nameField, "Имя персонажа", ProjectFieldType.String, FieldBoundTo.Character)));
        _ = Services.AddJoinRpgWebComponents();
        _ = AddBunitPersistentComponentState();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private IRenderedComponent<BadgeTemplateEditForm> RenderForm(EditBadgeTemplateViewModel model)
        => Render<BadgeTemplateEditForm>(p => p
            .Add(x => x.ProjectId, projectId)
            .Add(x => x.Model, model));

    private EditBadgeTemplateViewModel ValidModel() => new()
    {
        Name = "Игрок",
        Lines =
        [
            new() { FieldId = nameField, OffsetMm = 10 },
            new() { StaticText = "Каэр Морхен", OffsetMm = 40 },
        ],
    };

    // В превью вместо значения поля — его название: мастер видит, что где окажется на бейдже.
    [Fact]
    public void Preview_ShowsFieldNameAndStaticText()
    {
        var cut = RenderForm(ValidModel());

        cut.WaitForAssertion(() =>
            cut.FindAll(".join-printable-badge-line").Select(l => l.TextContent)
                .ShouldBe(["Имя персонажа", "Каэр Морхен"]));
    }

    // Пример значения у строки с полем — в превью вместо названия поля.
    [Fact]
    public void Preview_UsesSampleTextForFieldLine()
    {
        var model = ValidModel();
        model.Lines[0].StaticText = "Цирилла Фиона Элен Рианнон";
        var cut = RenderForm(model);

        cut.WaitForAssertion(() =>
            cut.FindAll(".join-printable-badge-line").Select(l => l.TextContent).First()
                .ShouldBe("Цирилла Фиона Элен Рианнон"));
    }

    // На печати строка с удалённым полем пустая — превью не должно показывать вместо этого пример.
    [Fact]
    public void Preview_DeletedFieldWins_OverSampleText()
    {
        var model = ValidModel();
        model.Lines[0].FieldId = new ProjectFieldIdentification(projectId, 999);
        model.Lines[0].StaticText = "Пример";
        var cut = RenderForm(model);

        cut.WaitForAssertion(() =>
            cut.FindAll(".join-printable-badge-line").Select(l => l.TextContent).First().ShouldBe("(удалённое поле)"));
    }

    [Fact]
    public void ShouldShowServerRefusal()
    {
        client.FailWith = new ServerProblemException("Поле «Позывной» удалено");
        var cut = RenderForm(ValidModel());

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Поле «Позывной» удалено"));
    }

    // Форма проверяет те же правила, что и сервер, — и не отправляет заведомо отклонённое.
    [Fact]
    public void InvalidLayout_NotSent()
    {
        var model = ValidModel();
        model.WidthMm = BadgeLayoutRules.MaxWidthMm + 1;
        var cut = RenderForm(model);

        cut.Find("form").Submit();

        client.Created.ShouldBeEmpty();
        cut.Markup.ShouldContain("Ширина бейджа");
    }

    [Fact]
    public void ValidLayout_SentAsDomainLayout()
    {
        var cut = RenderForm(ValidModel());

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => client.Created.ShouldHaveSingleItem());
        var layout = client.Created.Single().ToLayout();
        layout.Lines.Select(l => l.ProjectFieldId).ShouldBe([7, null]);
        layout.Lines.Select(l => l.StaticText).ShouldBe([null, "Каэр Морхен"]);
    }

    private class UriLocator : IBadgeTemplateUriLocator
    {
        public Uri GetListUri(ProjectIdentification projectId) => new("/badges", UriKind.Relative);
        public Uri GetCreateUri(ProjectIdentification projectId) => new("/badges/edit", UriKind.Relative);
        public Uri GetEditUri(ProjectBadgeTemplateIdentification id) => new($"/badges/edit/{id.ProjectBadgeTemplateId}", UriKind.Relative);
    }

    private class FieldsClient(params ProjectFieldDto[] fields) : IProjectFieldsClient
    {
        public Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId) => Task.FromResult(fields.ToList());
    }

    private class RecordingClient : IBadgeTemplateClient
    {
        public Exception? FailWith { get; set; }
        public List<EditBadgeTemplateViewModel> Created { get; } = [];

        public Task<ProjectBadgeTemplateIdentification> Create(ProjectIdentification projectId, EditBadgeTemplateViewModel model)
        {
            if (FailWith is not null)
            {
                throw FailWith;
            }
            Created.Add(model);
            return Task.FromResult(new ProjectBadgeTemplateIdentification(projectId, 1));
        }

        public Task<BadgeTemplateListViewModel> GetList(ProjectIdentification projectId) => throw new NotImplementedException();
        public Task<EditBadgeTemplateViewModel> GetById(ProjectBadgeTemplateIdentification id) => throw new NotImplementedException();
        public Task Update(ProjectBadgeTemplateIdentification id, EditBadgeTemplateViewModel model) => throw new NotImplementedException();
        public Task Remove(ProjectBadgeTemplateIdentification id) => throw new NotImplementedException();
        public Task Restore(ProjectBadgeTemplateIdentification id) => throw new NotImplementedException();
    }
}
