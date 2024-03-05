using Markdig;

namespace JoinRpg.Markdown;

internal static class MarkdownEntityLinkerExtensions
{
    public static MarkdownPipelineBuilder UseEntityLinker(this MarkdownPipelineBuilder pipeline, string[] prefixes)
    {
        pipeline.Extensions.AddIfNotAlready(new EntityLinkerExtension(prefixes));
        return pipeline;
    }
}
