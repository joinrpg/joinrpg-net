using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;

namespace JoinRpg.Web.Helpers
{
  public static class ModelStateExtensions
  {
    public static void AddException(this ModelStateDictionary dict, Exception exception)
    {
      var validation = exception as DbEntityValidationException;
      if (validation != null)
      {
        var dbValidationErrors = validation.EntityValidationErrors.SelectMany(eve => eve.ValidationErrors).ToList();
        if (dbValidationErrors.Any())
        {
          foreach (var error in dbValidationErrors)
          {
            dict.AddModelError("", error.PropertyName + ": " + error.ErrorMessage);
          }
          return;
        }
      }
      dict.AddModelError("", exception.ToString());
    }
  }
}
