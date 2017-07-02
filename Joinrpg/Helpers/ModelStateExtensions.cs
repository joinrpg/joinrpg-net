using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.Domain;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Helpers
{
  public static class ModelStateExtensions
  {
    //TODO messages from resources
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
            dict.AddModelError("",
              String.IsNullOrWhiteSpace(error.PropertyName)
                ? error.ErrorMessage
                : $"{error.PropertyName}: {error.ErrorMessage}");
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

      var projectDeactivated = exception as ProjectDeactivedException;
      if (projectDeactivated != null)
      {
        dict.AddModelError("", exception.Message);
        return;
      }

      var already = exception as ClaimAlreadyPresentException;
      if (already != null)
      {
        dict.AddModelError("", exception.Message);
        return;
      }

      var noaccept = exception as ClaimTargetIsNotAcceptingClaims;
      if (noaccept != null)
      {
        dict.AddModelError("", exception.Message);
        return;
      }

      var failedToRemoveMaster = exception as MasterHasResponsibleException;
      if (failedToRemoveMaster != null)
      {
        dict.AddModelError("", exception.Message);
        return;
      }

      dict.AddModelError("", exception.ToString());
    }

    public static void AddErrors(this ModelStateDictionary modelStateDictionary, IdentityResult result)
    {
      foreach (var error in result.Errors)
      {
        modelStateDictionary.AddModelError("", error);
      }
    }
  }
}
