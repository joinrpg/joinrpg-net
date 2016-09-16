using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Markdig.Helpers;
using Markdig.Parsers;

namespace Joinrpg.Markdown
{
  internal class LinkerParser : InlineParser
  {
    private ILinkRenderer LinkRenderer { get; }

    public LinkerParser([NotNull] ILinkRenderer linkRenderer)
    {
      if (linkRenderer == null) throw new ArgumentNullException(nameof(linkRenderer));
      LinkRenderer = linkRenderer;
    }

    private TextMatchHelper _textMatchHelper;
    
    public override void Initialize()
    {
      _textMatchHelper = new TextMatchHelper(new HashSet<string>(LinkRenderer.LinkTypesToMatch.Select(c => "@" + c)));

      OpeningCharacters = new[] {'@'};
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
      if (slice.Start != 0 && !slice.PeekCharExtra(-1).IsWhitespace())
      {
        return false;
      }
      string match;
      if (!_textMatchHelper.TryMatch(slice.Text, slice.Start, slice.Length, out match))
      {
        return false;
      }

      // Move the cursor to the character after the matched string
      slice.Start += match.Length;

      var builder = new StringBuilder();
      while (slice.CurrentChar.IsDigit() && !slice.IsEmpty)
      {
        builder.Append(slice.CurrentChar);
        slice.Start ++;
      }

      var index = builder.Length > 0 ? int.Parse(builder.ToString()) : 0;

      if (index == 0)
      {
        //If we failed to parse index, abort
        slice.Start -= builder.Length;
        slice.Start -= match.Length;
        return false;
      }

      builder.Clear();

      if (slice.CurrentChar == '(')
      {
        slice.Start ++;

        while (slice.CurrentChar != ')' && !slice.IsEmpty)
        {
          builder.Append(slice.CurrentChar);
          slice.Start++;
        }

        if (!slice.IsEmpty)
        {
          slice.Start++;
        }
      }

      var extra = builder.ToString();
      processor.Inline = new EntityLinkInline(match, index, extra);

      return true;
    }
  }
}