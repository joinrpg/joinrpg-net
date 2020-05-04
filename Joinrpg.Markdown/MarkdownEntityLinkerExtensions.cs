using System;
using JetBrains.Annotations;
using Markdig;

namespace Joinrpg.Markdown
{
    internal static class MarkdownEntityLinkerExtensions
    {
        public static MarkdownPipelineBuilder UseEntityLinker(this MarkdownPipelineBuilder pipeline,
          [NotNull] ILinkRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            pipeline.Extensions.AddIfNotAlready(new EntityLinkerExtension(renderer));
            return pipeline;
        }
    }
}
