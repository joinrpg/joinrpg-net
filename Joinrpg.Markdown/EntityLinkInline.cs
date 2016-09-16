using Markdig.Syntax.Inlines;

namespace Joinrpg.Markdown
{
  internal class EntityLinkInline : Inline
  {
    public string Match { get; }
    public int Index { get;  }
    public string Extra { get; }

    public EntityLinkInline(string match, int index, string extra)
    {
      Match = match;
      Index = index;
      Extra = extra;
    }
  }
}