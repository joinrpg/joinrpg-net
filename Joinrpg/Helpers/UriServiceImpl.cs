using System;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Helpers
{
  [UsedImplicitly]
  internal class UriServiceImpl : IUriService
  {
    private readonly HttpContextBase _current;

    public UriServiceImpl(HttpContextBase current)
    {
      _current = current;
    }

    public string Get(ILinkable link)
    {
      return GetUri(link).ToString();
    }

    public Uri GetUri(ILinkable link)
    {
      return link.GetRouteTarget().GetUri(new UrlHelper(_current.Request.RequestContext));
    }
  }
}
