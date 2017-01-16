using System;
using JetBrains.Annotations;
using Markdig;
using Markdig.Renderers;

namespace Joinrpg.Markdown
{
  internal class EntityLinkerExtension : IMarkdownExtension
  {
    [NotNull]
    private ILinkRenderer LinkRenderers { get; }

    public EntityLinkerExtension([NotNull] ILinkRenderer linkRenderers)
    {
      if (linkRenderers == null) throw new ArgumentNullException(nameof(linkRenderers));
      LinkRenderers = linkRenderers;
    }

    #region Implementation of IMarkdownExtension

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
      pipeline.InlineParsers.AddIfNotAlready(new LinkerParser(LinkRenderers));
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
      renderer.ObjectRenderers.AddIfNotAlready(new LinkerRenderAdapter(LinkRenderers));
    }

    #endregion
  }
}