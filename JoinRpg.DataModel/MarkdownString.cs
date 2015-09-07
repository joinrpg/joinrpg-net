namespace JoinRpg.DataModel
{
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
