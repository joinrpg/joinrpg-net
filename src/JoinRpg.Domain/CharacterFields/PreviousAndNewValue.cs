namespace JoinRpg.Domain.CharacterFields;

public class PreviousAndNewValue
{
    public MarkdownDbValue DisplayString { get; }
    public MarkdownDbValue PreviousDisplayString { get; }

    public PreviousAndNewValue(string newValue, string? previousValue)
    {
        DisplayString = new MarkdownDbValue(newValue);
        PreviousDisplayString = new MarkdownDbValue(previousValue);
    }

    public PreviousAndNewValue(MarkdownDbValue newValue, MarkdownDbValue previousValue)
    {
        DisplayString = newValue;
        PreviousDisplayString = previousValue;
    }
}
