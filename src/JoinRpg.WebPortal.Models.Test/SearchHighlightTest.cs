using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Search;

namespace JoinRpg.WebPortal.Models.Test;

// Тесты для подсветки результатов поиска в GetFormattedDescription (ADR007, пункт 2).
//
// Архитектура: описание сначала декодируется в сырой plain text, затем ищется/обрезается,
// а каждый сегмент HTML-кодируется явно при сборке итоговой строки.
// Это исправляет UX-баг (спецсимволы не находились) и security-риск
// (защита теперь явная, не зависящая от побочного эффекта санитайзера).
public class SearchHighlightTest
{
    private sealed class StubUriService : IUriService
    {
        public string Get(ILinkable link) => "/stub";
        public Uri GetUri(ILinkable link) => new("/stub", UriKind.Relative);
    }

    private static TargetedSearchResultViewModel CreateVm(string descriptionMarkdown, string searchTarget)
    {
        var result = new SearchResult
        {
            LinkType = LinkType.ResultCharacter,
            Name = "Test",
            Description = new MarkdownDbValue(descriptionMarkdown),
            IsPublic = true,
            IsActive = true,
            Identification = "1",
            ProjectId = null,
        };
        return new TargetedSearchResultViewModel(result, searchTarget, new StubUriService(), projectViewModel: null);
    }

    // -------------------------------------------------------------------
    // Базовая подсветка
    // -------------------------------------------------------------------

    [Fact]
    public void GetFormattedDescription_PlainText_HighlightsSearchTerm()
    {
        var vm = CreateVm("hello world", searchTarget: "hello");
        vm.GetFormattedDescription(1000).ToHtmlString()
            .ShouldContain("<b><u>hello</u></b>");
    }

    // -------------------------------------------------------------------
    // Спецсимволы HTML в описании — исправление UX
    // -------------------------------------------------------------------

    [Fact]
    public void GetFormattedDescription_AmpersandInDescription_IsFoundAndHighlighted()
    {
        // Раньше поиск "AT&T" в описании "AT&T" не давал подсветки,
        // потому что поиск вёлся по HTML-закодированному "AT&amp;T".
        // Теперь сначала декодируем в plain text — совпадение находится.
        var vm = CreateVm("AT&T", searchTarget: "AT&T");
        var result = vm.GetFormattedDescription(1000).ToHtmlString();

        result.ShouldBe("<b><u>AT&amp;T</u></b>");
    }

    [Fact]
    public void GetFormattedDescription_AngleBracketInDescription_EncodedNotRaw()
    {
        // "price < 100" → после HTML-кодирования → "price &lt; 100"
        // Угловая скобка кодируется явно при сборке; в результате нет сырого '<'.
        var vm = CreateVm("price < 100", searchTarget: "price");
        var result = vm.GetFormattedDescription(1000).ToHtmlString();

        result.ShouldContain("<b><u>price</u></b>");
        result.ShouldNotContain("price <");   // сырой '<' не должен попасть в результат
        result.ShouldContain("&lt; 100");     // '<' должен оставаться закодированным
    }

    // -------------------------------------------------------------------
    // Защита от XSS: явный HtmlEncoder в BuildHighlightedHtml предотвращает инъекцию
    // -------------------------------------------------------------------

    [Fact]
    public void GetFormattedDescription_OutputNeverContainsRawHtmlFromDescription()
    {
        // Markdig убирает inline HTML-теги в plain-text режиме, поэтому <script>
        // до BuildHighlightedHtml не добирается. Даже если бы добрался —
        // HtmlEncoder.Default.Encode его закодировал бы. Оба уровня защищают от XSS.
        const string malicious = "click <script>alert(1)</script> here";
        var vm = CreateVm(malicious, searchTarget: "click");
        var result = vm.GetFormattedDescription(1000).ToHtmlString();

        result.ShouldNotContain("<script>");
        result.ShouldContain("<b><u>click</u></b>");
    }
}
