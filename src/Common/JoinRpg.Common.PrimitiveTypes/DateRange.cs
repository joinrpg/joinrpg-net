namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Период дат с началом и концом.
/// </summary>
public readonly record struct DateRange
{
    public DateOnly Begin { get; }

    public DateOnly End { get; }

    public DateRange(DateOnly Begin, DateOnly End)
    {
        if (End < Begin)
        {
            throw new ArgumentOutOfRangeException(nameof(End), End, "End должен быть не раньше Begin");
        }

        this.Begin = Begin;
        this.End = End;
    }

    public void Deconstruct(out DateOnly Begin, out DateOnly End)
    {
        Begin = this.Begin;
        End = this.End;
    }
}
