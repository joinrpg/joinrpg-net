using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;

namespace JoinRpg.Web.Models.CommonTypes;

/// <inheritdoc cref="IHtmlContent" />
/// <summary>
/// Proxy class that allows conversion from Blazor Markup string to Microsoft.AspNetCore versions.
/// </summary>
public sealed class JoinHtmlString : IHtmlContent
{
    private string Value { get; }

    private JoinHtmlString(string value) => Value = value;

    /// <summary>
    /// Converts from AspNetCore style string
    /// </summary>
    public static implicit operator JoinHtmlString(HtmlString content)
    {
        var stringWriter = new StringWriter();
        content.WriteTo(stringWriter, HtmlEncoder.Default);
        var value = stringWriter.ToString();
        return new JoinHtmlString(value);
    }

    /// <summary>
    /// Converts from Blazor style string
    /// </summary>
    public static implicit operator JoinHtmlString(MarkupString content) => new(content.Value);

    /// <summary>
    /// concat operator 
    /// </summary>
    public static JoinHtmlString operator +(JoinHtmlString left, JoinHtmlString right) => new(left.ToString() + right);

    /// <inheritdoc />
    public string ToHtmlString() => Value;

    /// <inheritdoc />
    public void WriteTo(TextWriter writer, HtmlEncoder encoder) => writer.Write(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>
    /// Is null or whitespace
    /// </summary>
    public bool IsNullOrWhiteSpace() => string.IsNullOrWhiteSpace(Value);
}
