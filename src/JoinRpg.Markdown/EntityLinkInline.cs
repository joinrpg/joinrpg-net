using Markdig.Syntax.Inlines;

namespace JoinRpg.Markdown;

internal class EntityLinkInline(string match, int index, string extra, ILinkRenderer renderer) : Inline
{
    public string Match { get; } = match;
    public int Index { get; } = index;
    public string Extra { get; } = extra;
    public ILinkRenderer Renderer { get; } = renderer;
}
