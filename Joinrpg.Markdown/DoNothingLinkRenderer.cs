using System;
using System.Collections.Generic;

namespace Joinrpg.Markdown
{
    /// <summary>
    /// Default implementations of link renderer that will not render any links
    /// </summary>
    internal class DoNothingLinkRenderer : ILinkRenderer
    {
        /// <inheritdoc />
        public IEnumerable<string> LinkTypesToMatch { get; } = new string[] { };

        /// <inheritdoc />
        public string Render(string match, int index, string extra) => throw new NotSupportedException();

        /// <summary>
        /// Instance copy to avoid allocations
        /// </summary>
        public static readonly DoNothingLinkRenderer Instance = new DoNothingLinkRenderer();

        /// <summary>
        /// Private ctor (we will need only one copy)
        /// </summary>
        private DoNothingLinkRenderer() { }
    }
}
