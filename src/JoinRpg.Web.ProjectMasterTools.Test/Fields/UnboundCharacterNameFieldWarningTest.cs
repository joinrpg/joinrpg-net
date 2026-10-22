using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectMasterTools.Fields;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.Fields;

public class UnboundCharacterNameFieldWarningTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);

    public UnboundCharacterNameFieldWarningTest()
        => Services.AddSingleton<IProjectFieldUriLocator>(new FakeUriLocator());

    [Fact]
    public void NoCandidates_RendersNothing()
        => Render<UnboundCharacterNameFieldWarning>(p => p.Add(c => c.FieldNames, []))
            .Markup.Trim().ShouldBeEmpty();

    [Fact]
    public void OneCandidate_NamesFieldAndLinksToSettings()
    {
        var cut = Render<UnboundCharacterNameFieldWarning>(p => p
            .Add(c => c.FieldNames, ["Имя персонажа"])
            .Add(c => c.ProjectId, ProjectId));

        Text(cut).ShouldContain(
            "Поле «Имя персонажа» похоже на имя персонажа, но имя сейчас привязано к имени игрока. Перепроверьте настройки полей.",
            Case.Sensitive);
        cut.Find("a").GetAttribute("href").ShouldBe("https://localhost/1/fields/settings");
    }

    [Fact]
    public void SeveralCandidates_ListsAll()
        => Text(Render<UnboundCharacterNameFieldWarning>(p => p.Add(c => c.FieldNames, ["Имя", "Название мероприятия"])))
            .ShouldContain("Поля «Имя», «Название мероприятия» похожи на имя персонажа,", Case.Sensitive);

    [Fact]
    public void WithoutProject_NoLink()
        => Render<UnboundCharacterNameFieldWarning>(p => p.Add(c => c.FieldNames, ["Имя"]))
            .FindAll("a").ShouldBeEmpty();

    /// <summary>Текст предупреждения с пробелами, схлопнутыми как в браузере.</summary>
    private static string Text(IRenderedComponent<UnboundCharacterNameFieldWarning> cut)
        => string.Join(' ', cut.Find(".alert").TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private class FakeUriLocator : IProjectFieldUriLocator
    {
        public Uri GetEditUri(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Uri GetCreateVariantUri(ProjectFieldIdentification fieldId) => throw new NotSupportedException();
        public Uri GetEditVariantUri(ProjectFieldVariantIdentification variantId) => throw new NotSupportedException();
        public Uri GetFieldSettingsUri(ProjectIdentification projectId) => new($"https://localhost/{projectId.Value}/fields/settings");
    }
}
