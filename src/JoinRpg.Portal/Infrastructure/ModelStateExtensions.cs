using System.Data.Entity.Validation;
using JoinRpg.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JoinRpg.Portal.Infrastructure;

internal static class ModelStateExtensions
{
    //TODO messages from resources
    public static void AddException(this ModelStateDictionary dict, Exception exception)
    {

        switch (exception)
        {
            case DbEntityValidationException validation:
                var dbValidationErrors = validation.EntityValidationErrors
                    .SelectMany(eve => eve.ValidationErrors).ToList();
                if (dbValidationErrors.Count != 0)
                {
                    foreach (var error in dbValidationErrors)
                    {
                        dict.AddModelError("",
                            string.IsNullOrWhiteSpace(error.PropertyName)
                                ? error.ErrorMessage
                                : $"{error.PropertyName}: {error.ErrorMessage}");
                    }

                    return;
                }

                dict.AddModelError("", exception.ToString());
                return;
            case CharacterFieldRequiredException required:
                var errorMessage = GetErrorMessage(required);
                dict.AddModelError("", errorMessage);
                dict.AddModelError(Web.Models.FieldValueViewModel.HtmlIdPrefix + required.FieldId.ProjectFieldId, errorMessage);
                return;
            case FieldRequiredException required:
                var errorMessage1 = GetErrorMessage(required);
                dict.AddModelError("", errorMessage1);
                dict.AddModelError(required.FieldName, errorMessage1);
                return;
            case ClaimWrongStatusException _:
            case ProjectDeactivatedException _:
            case ClaimAlreadyPresentException _:
            case ClaimTargetIsNotAcceptingClaims _:
            case MasterHasResponsibleException _:
                dict.AddModelError("", exception.Message);
                return;
            case JoinRpgNameFieldDeleteException _:
                dict.AddModelError("",
                    "Невозможно удалить поле, потому что оно привязано к имени персонажа");
                return;
            case JoinFieldScheduleShouldBeUniqueException _:
                dict.AddModelError("", "Невозможно добавить второе поле с настройками расписания");
                return;
            default:
                dict.AddModelError("", exception.ToString());
                break;
        }
    }

    private static string GetErrorMessage(FieldRequiredException required) => $"{required.FieldName} — обязательное поле";
}
