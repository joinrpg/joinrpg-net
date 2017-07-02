
using JoinRpg.DataModel;

namespace JoinRpg.Domain.CharacterFields
{
  public class OldAndNewValue
  {
    public MarkdownString DisplayString { get; }
    public MarkdownString PreviousDisplayString { get; }

    public OldAndNewValue(string newValue, string previousValue)
    {
      DisplayString = new MarkdownString(newValue);
      PreviousDisplayString = new MarkdownString(previousValue);
    }

    public OldAndNewValue(MarkdownString newValue, MarkdownString previousValue)
    {
      DisplayString = newValue;
      PreviousDisplayString = previousValue;
    }
  }
}
