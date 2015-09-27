using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IUriService
  {
    string Get(ILinkable link);
    string GetHostName();
    string GetScheme();
  }
}
