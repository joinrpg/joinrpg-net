using System.Collections.Generic;

namespace Joinrpg.Markdown
{
    /// <summary>
    /// interfaces that allows consumers to plugin its renderers
    /// </summary>
    public interface ILinkRenderer
    {
        /// <summary>
        /// List of types to match (like %link1, %link2)
        /// </summary>
        IEnumerable<string> LinkTypesToMatch { get; }

        /// <summary>
        /// Function that do actual rendering 
        /// </summary>
        string Render(string match, int index, string extra);
    }
}
