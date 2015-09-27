using System.Web;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Helpers
{
  [UsedImplicitly]
  class UriServiceImpl : IUriService
  {
    private readonly HttpContext _current;

    public UriServiceImpl(HttpContext current)
    {
      _current = current;
    }

    public string Get(ILinkable link)
    {
      return link.GetRouteTarget().GetUri(_current);
    }

    public string GetHostName()
    {
      return "dev.joinrpg.ru";
    }

    public string GetScheme()
    {
      return "http";
    }
  }
}
