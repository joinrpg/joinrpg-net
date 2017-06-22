using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.PlayerIdCard
{
  [UsedImplicitly]
  internal class PlayerCardConfiguration
  {
    [UsedImplicitly]
    public MagicFieldConfig[] Fields { get; set; }
  }

  [UsedImplicitly]
  internal class MagicFieldConfig
  {
    [UsedImplicitly]
    public string Label { get; set; }
    [UsedImplicitly]
    public SpecialGroupConfig[] Groups { get; set; }
  }

  [UsedImplicitly]
  internal class SpecialGroupConfig
  {
    [UsedImplicitly]
    public int GroupId { get; set; }
    [UsedImplicitly]
    public string AddToLabel { get; set; }
  }
}