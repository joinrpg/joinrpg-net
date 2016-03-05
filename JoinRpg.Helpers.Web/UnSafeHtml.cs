namespace JoinRpg.Helpers.Web
{
  public class UnSafeHtml
  {
    public string UnValidatedValue { get; }

    private UnSafeHtml(string value)
    {
      UnValidatedValue = value;
    }

    public static implicit operator UnSafeHtml(string s) => new UnSafeHtml(s);
  }
}