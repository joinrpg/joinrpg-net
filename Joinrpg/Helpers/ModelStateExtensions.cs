using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.Domain;

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
      var required = exception as FieldRequiredException;
      if (required != null)
      {
        dict.AddModelError("", required.FieldName + "is required");
        dict.AddModelError(required.FieldName, "required");
        return;
      }
      var wrongStatus = exception as ClaimWrongStatusException;
      if (wrongStatus != null)
      {
        dict.AddModelError("", exception.Message);
        return;
      }
      dict.AddModelError("", exception.ToString());
    }
  }
}
