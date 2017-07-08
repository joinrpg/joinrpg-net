
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  public class PreviousAndNewValue
  {
    public MarkdownString DisplayString { get; }
    public MarkdownString PreviousDisplayString { get; }

    public PreviousAndNewValue(string newValue, string previousValue)
    {
      DisplayString = new MarkdownString(newValue);
      PreviousDisplayString = new MarkdownString(previousValue);
    }

    public PreviousAndNewValue(MarkdownString newValue, MarkdownString previousValue)
    {
      DisplayString = newValue;
      PreviousDisplayString = previousValue;
    }
  }
}
