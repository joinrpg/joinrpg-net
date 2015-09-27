using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IHtmlService
  {
    string MarkdownToHtml(MarkdownString md);
  }
}
