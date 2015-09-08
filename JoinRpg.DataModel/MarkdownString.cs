using System.ComponentModel.DataAnnotations.Schema;

namespace JoinRpg.DataModel
{
  [ComplexType]
  public class MarkdownString
  {
    public MarkdownString(string contents)
    {
      //TODO: Validate for correct Markdown
      Contents = contents;
    }

    public MarkdownString() : this(null)
    {
    }

    public string Contents { get; set; }
  }
}
