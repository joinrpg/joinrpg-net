using System.ComponentModel.DataAnnotations;
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
            case ValidationException validation:
                var errorMessage = validation.ValidationResult.ErrorMessage ?? "Validation error";
                dict.AddModelError("", errorMessage);
                if (validation.Source is not null)
                {
                    dict.AddModelError(validation.Source, errorMessage);
                }
                return;

            case FieldRequiredException required:
                dict.AddModelError("", required.FieldName + " is required");
                dict.AddModelError(required.FieldName, " required");
                return;
            case ClaimWrongStatusException _:
            case ProjectDeactivedException _:
            case ClaimAlreadyPresentException _:
            case ClaimTargetIsNotAcceptingClaims _:
            case MasterHasResponsibleException _:
            case CharacterShouldNotHaveClaimsException _:
            case CharacterGroupShouldNotHaveClaimsException _:
            case JoinRpgEntityNotFoundException _:
            case JoinRpgCharacterBrokenStateException _:
                dict.AddModelError("", exception.Message);
                return;
            case JoinRpgNameFieldDeleteException _:
                dict.AddModelError("",
                    "Невозможно удалить поле, потому что оно привязано к имени персонажа");
                return;
            case JoinFieldScheduleShouldBeUniqueException _:
                dict.AddModelError("", "Невозможно добавить второе поле с настройками расписания");
                return;
            case JoinRpgProjectException _:
                dict.AddModelError("", exception.Message);
                return;
            default:
                dict.AddModelError("", exception.ToString());
                break;
        }
    }
}
