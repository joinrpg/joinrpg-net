using HtmlAgilityPack;

namespace JoinRpg.IdPortal.Test.Infrastructure;

public class HtmlFormHelpersTest
{
    // Разметка CheckboxInput: checkbox со значением true перед hidden.
    private static string CheckboxInput(bool isChecked, bool isDisabled = false)
        => $"""
            <input type="checkbox" name="Flag" value="true" {(isChecked ? "checked" : "")} {(isDisabled ? "disabled" : "")} />
            <input type="hidden" name="Flag" value="{(isDisabled && isChecked ? "true" : "false")}" />
            """;

    private static Dictionary<string, string?> Fields(string formContent)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml($"<form>{formContent}</form>");
        return doc.GetFormFields();
    }

    [Fact]
    public void CheckedCheckboxInput_PostsTrue() => Fields(CheckboxInput(isChecked: true))["Flag"].ShouldBe("true");

    [Fact]
    public void UncheckedCheckboxInput_PostsFalse() => Fields(CheckboxInput(isChecked: false))["Flag"].ShouldBe("false");

    [Fact]
    public void DisabledCheckboxInput_PostsCurrentValue()
        => Fields(CheckboxInput(isChecked: true, isDisabled: true))["Flag"].ShouldBe("true");

    [Fact]
    public void DisabledFieldIsNotPosted()
        => Fields("""
            <input type="text" name="Name" value="a" disabled />
            <input type="text" name="Name" value="b" />
            """)["Name"].ShouldBe("b");

    [Fact]
    public void OnlyCheckedRadioIsPosted()
        => Fields("""
            <input type="radio" name="Choice" value="a" checked />
            <input type="radio" name="Choice" value="b" />
            """)["Choice"].ShouldBe("a");

    [Fact]
    public void CheckedCheckboxWithoutValue_PostsOn()
        => Fields("""<input type="checkbox" name="Remember" checked />""")["Remember"].ShouldBe("on");

    [Fact]
    public void ValueIsHtmlDecoded()
        => Fields("""<input type="hidden" name="Token" value="a&amp;b" />""")["Token"].ShouldBe("a&b");
}
