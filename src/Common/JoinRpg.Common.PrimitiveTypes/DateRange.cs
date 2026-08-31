namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Период дат с началом и концом.
/// </summary>
public readonly record struct DateRange(DateOnly Begin, DateOnly End);
