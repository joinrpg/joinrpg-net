using System.Data.Entity.Validation;
using JoinRpg.Domain;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Common;

public abstract class JoinMvcControllerBase : Controller
{

    protected ActionResult ViewIfFound(string? viewName, object? model)
        => model == null ? NotFound() : View(viewName, model);

    protected ActionResult ViewIfFound(object? model)
        => ViewIfFound(null, model);

    protected async Task<ActionResult> ViewIfFound<T>(Task<T> model)
        => ViewIfFound(null, await model);

    protected ActionResult NotModified() => new StatusCodeResult(StatusCodes.Status304NotModified);

    protected void AddModelException(Exception exception)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<JoinMvcControllerBase>>();


        switch (exception)
        {
            case DbEntityValidationException validation:
                var dbValidationErrors = validation.EntityValidationErrors
                    .SelectMany(eve => eve.ValidationErrors).ToList();
                if (dbValidationErrors.Count != 0)
                {
                    foreach (var error in dbValidationErrors)
                    {
                        ModelState.AddModelError("",
                            string.IsNullOrWhiteSpace(error.PropertyName)
                                ? error.ErrorMessage
                                : $"{error.PropertyName}: {error.ErrorMessage}");
                    }

                    return;
                }

                ModelState.AddModelError("", exception.ToString());
                return;
            case CharacterFieldRequiredException required:
                var errorMessage = GetErrorMessage(required);
                ModelState.AddModelError("", errorMessage);
                ModelState.AddModelError(Web.Models.FieldValueViewModel.HtmlIdPrefix + required.FieldId.ProjectFieldId, errorMessage);
                return;
            case FieldRequiredException required:
                var errorMessage1 = GetErrorMessage(required);
                ModelState.AddModelError("", errorMessage1);
                ModelState.AddModelError(required.FieldName, errorMessage1);
                return;
            case ClaimWrongStatusException _:
            case ProjectDeactivatedException _:
            case ClaimAlreadyPresentException _:
            case ClaimTargetIsNotAcceptingClaims _:
            case MasterHasResponsibleException _:
                ModelState.AddModelError("", exception.Message);
                return;
            case JoinRpgNameFieldDeleteException _:
                ModelState.AddModelError("",
                    "Невозможно удалить поле, потому что оно привязано к имени персонажа");
                return;
            case JoinFieldScheduleShouldBeUniqueException _:
                ModelState.AddModelError("", "Невозможно добавить второе поле с настройками расписания");
                return;
            default:

                logger.LogError(exception, "Исключение при обработке запроса");
                ModelState.AddModelError("", $"Неожиданная ошибка, обратитесь в техподдержку: {HttpContext.TraceIdentifier}, {exception.Message}");

                break;
        }
    }

    private static string GetErrorMessage(FieldRequiredException required) => $"{required.FieldName} — обязательное поле";
}
