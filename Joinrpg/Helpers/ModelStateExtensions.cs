using System;
using System.Web.Mvc;

namespace JoinRpg.Web.Helpers
{
  public static class ModelStateExtensions
  {
    public static void AddException(this ModelStateDictionary dict, Exception exception)
    {
      dict.AddModelError("", exception.ToString());
    }
  }
}
