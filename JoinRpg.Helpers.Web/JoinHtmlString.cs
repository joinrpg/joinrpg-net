using System.IO;
using System.Text.Encodings.Web;
using CoreHtmlString = Microsoft.AspNetCore.Html.HtmlString;
using WebHtmlString = System.Web.HtmlString;

namespace JoinRpg.Helpers.Web
{
    /// <inheritdoc cref="System.Web.IHtmlString" />
    /// <inheritdoc cref="Microsoft.AspNetCore.Html.IHtmlContent" />
    /// <summary>
    /// Proxy class that allows conversion from System.Web style strings to Microsoft.AspNetCore versions
    /// Will be removed after porting to ASP.NET Core
    /// </summary>
    public sealed class JoinHtmlString : System.Web.IHtmlString, Microsoft.AspNetCore.Html.IHtmlContent
    {
        private string Value { get; }

        private JoinHtmlString(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Converts from AspNetCore style string
        /// </summary>
        public static implicit operator JoinHtmlString(CoreHtmlString content)
        {
            var stringWriter = new StringWriter();
            content.WriteTo(stringWriter, HtmlEncoder.Default);
            var value = stringWriter.ToString();
            return new JoinHtmlString(value);
        }

        /// <summary>
        /// Converts from System.Web style string
        /// </summary>
        /// <param name="content"></param>
        public static implicit operator JoinHtmlString(WebHtmlString content)
        {
            return new JoinHtmlString(content.ToHtmlString());
        }

        /// <inheritdoc />
        public string ToHtmlString() => Value;

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder) => writer.Write(Value);

        public override string ToString() => Value;
    }
}
