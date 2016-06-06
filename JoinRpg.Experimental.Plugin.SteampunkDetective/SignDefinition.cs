namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class SignDefinition
  {
    public int Code { get; set; }
    public SignType SignType { get; set; }
    public int FieldId { get; set; }
    public string[] AllowedFieldValues { get; set; }
  }
}