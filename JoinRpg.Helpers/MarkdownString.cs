using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace JoinRpg.DataModel
{
  [ComplexType]
  public class MarkdownString
  {
    public MarkdownString([CanBeNull] string contents)
    {
      //TODO: Validate for correct Markdown
      Contents = contents;
    }

    public MarkdownString() : this(null)
    {
    }

    [CanBeNull]
    public string Contents { get; set; }
  }
}
