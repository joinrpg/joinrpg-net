namespace JoinRpg.Markdown;
using Markdig.Renderers;

/// <summary>
/// interfaces that allows consumers to plugin its renderers
/// </summary>
public interface ILinkRenderer
{
    /// <summary>
    /// List of types to match (like %link1, %link2)
    /// </summary>
    string[] LinkTypesToMatch { get; }

    /// <summary>
    /// Function that do actual rendering 
    /// </summary>
    void Render(HtmlRenderer renderer, string match, int index, string extra);
}
