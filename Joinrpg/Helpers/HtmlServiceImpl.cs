using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Helpers
{
  [UsedImplicitly]
  class HtmlServiceImpl : IHtmlService
  {
    public string MarkdownToHtml(MarkdownString md) => md.ToHtmlString().ToHtmlString();
  }
}
