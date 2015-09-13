using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.WebPages;

namespace JoinRpg.Web.App_Code
{
  /// <summary>
  /// It's crazy that we need this in helpers
  /// </summary>
  /// <remarks>Source: Dino Esposito - Programming Microsoft ASP.NET MVC</remarks>
  public static class MvcIntrinsics
  {
    public static System.Web.Mvc.HtmlHelper Html
    {
      get { return ((System.Web.Mvc.WebViewPage)WebPageContext.Current.Page).Html; }
    }

    public static System.Web.Mvc.AjaxHelper Ajax
    {
      get { return ((System.Web.Mvc.WebViewPage)WebPageContext.Current.Page).Ajax; }
    }

    public static System.Web.Mvc.UrlHelper Url
    {
      get { return ((System.Web.Mvc.WebViewPage)WebPageContext.Current.Page).Url; }
    }

  }
}
