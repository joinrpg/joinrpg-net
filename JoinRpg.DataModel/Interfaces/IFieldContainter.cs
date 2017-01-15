namespace JoinRpg.DataModel
{
  /// <summary>
  /// Represents something that can have fields
  /// </summary>
  public interface IFieldContainter
  {
    /// <summary>
    /// Contains values of fields for this character
    /// </summary>
    string JsonData { get; set; }
  }
}