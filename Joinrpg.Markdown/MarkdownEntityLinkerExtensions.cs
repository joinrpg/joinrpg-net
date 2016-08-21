using Markdig;

namespace Joinrpg.Markdown
{
  internal static class MarkdownEntityLinkerExtensions
  {
    public static MarkdownPipelineBuilder UseEntityLinker(this MarkdownPipelineBuilder pipeline, ILinkRenderer renderer)
    {
      pipeline.Extensions.AddIfNotAlready(new EntityLinkerExtension(renderer));
      return pipeline;
    }
  }
}