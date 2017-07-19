using System;

namespace JoinRpg.Web.XGameApi.Contract
{
  /// <summary>
  /// Short info about character
  /// </summary>
  public class CharacterHeader
  {
    /// <summary>
    /// Id
    /// </summary>
    public int CharacterId { get; set; }
    /// <summary>
    /// Last modified (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    /// <summary>
    /// Active/deleted
    /// </summary>
    public bool IsActive { get; set; }
    /// <summary>
    /// URI to full profile
    /// </summary>
    public string CharacterLink { get; set; }
  }
}