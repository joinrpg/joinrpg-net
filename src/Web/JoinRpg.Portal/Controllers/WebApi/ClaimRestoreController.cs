using JoinRpg.Helpers;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Восстановление проекта: заявки из уведомлений (DRP, #5481), группы, персонажи и поля из выгрузок мастера
/// (#5529). Проект заблокирован (ADR023), поэтому <c>[ProjectShouldBeActive]</c> здесь нет: его проверяют сервисы.
/// </summary>
/// <remarks>
/// <c>[IgnoreAntiforgeryToken]</c> нет сознательно: остров присылает токен заголовком.
/// Выгрузки на сервере не хранятся — остров присылает их в каждом запросе плана, отсюда большие лимиты.
/// </remarks>
[ApiController]
[Route("/webapi/claim-restore/[action]")]
[RequireMaster(Permission.CanManageClaims)]
public class ClaimRestoreController(IClaimRestoreClient client) : ControllerBase
{
    /// <summary>Самый большой файл выгрузки, который принимаем.</summary>
    public const long MaxExportFileSize = 30 * 1024 * 1024;

    /// <summary>Запрос плана несёт все загруженные выгрузки (сжатые) — с запасом на несколько больших.</summary>
    private const long MaxPlanRequestSize = 200 * 1024 * 1024;

    private const string WrongProject = "Неверный запрос: объект не найден в проекте";

    [HttpPost]
    [RequestSizeLimit(MaxPlanRequestSize)]
    public async Task<Results<Ok<ClaimRestorePlanViewModel>, BadRequest<string>>> GetPlan(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] ClaimRestorePlanRequest request)
    {
        if (request.ProjectId != projectId)
        {
            return TypedResults.BadRequest(WrongProject);
        }
        try
        {
            return TypedResults.Ok(await client.GetPlan(request));
        }
        catch (FormatException)
        {
            return TypedResults.BadRequest("Не удалось прочитать загруженную выгрузку — уберите её и загрузите файл заново");
        }
    }

    [HttpPost]
    [RequestSizeLimit(MaxExportFileSize + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxExportFileSize + 1024 * 1024)]
    public async Task<Results<Ok<MasterExportUploadResult>, BadRequest<string>>> ParseExport(
        [FromQuery] ProjectIdentification projectId,
        IFormFile file)
    {
        if (file.Length > MaxExportFileSize)
        {
            return TypedResults.Ok(new MasterExportUploadResult(null, MasterExportUploadErrorView.TooLarge));
        }
        await using var stream = file.OpenReadStream();
        return TypedResults.Ok(await client.ParseExport(projectId, Path.GetFileName(file.FileName), stream));
    }

    [HttpPost]
    public async Task<Results<Ok<ClaimRestoreApplyResult>, BadRequest<string>>> Apply(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] ClaimRestoreApplyRequest request)
    {
        if (request.ClaimId.ProjectId != projectId || (request.Character is not null && request.Character.ProjectId != projectId))
        {
            return TypedResults.BadRequest(WrongProject);
        }
        return await Run(async () => TypedResults.Ok(await client.Apply(request)));
    }

    [HttpPost]
    public async Task<Results<Ok, BadRequest<string>>> ApplyGroup(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] GroupRestoreApplyRequest request)
    {
        if (request.GroupId.ProjectId != projectId)
        {
            return TypedResults.BadRequest(WrongProject);
        }
        return await Run(async () =>
        {
            await client.ApplyGroup(request);
            return TypedResults.Ok();
        });
    }

    [HttpPost]
    public async Task<Results<Ok, BadRequest<string>>> ApplyCharacter(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] CharacterRestoreApplyRequest request)
    {
        if (request.CharacterId.ProjectId != projectId
            || request.Groups?.Any(g => g.ProjectId != projectId) == true
            || request.Fields.Any(f => f.FieldId.ProjectId != projectId))
        {
            return TypedResults.BadRequest(WrongProject);
        }
        return await Run(async () =>
        {
            await client.ApplyCharacter(request);
            return TypedResults.Ok();
        });
    }

    [HttpPost]
    public async Task<Results<Ok, BadRequest<string>>> ApplyClaimFields(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] ClaimFieldsRestoreApplyRequest request)
    {
        if (request.ClaimId.ProjectId != projectId || request.Fields.Any(f => f.FieldId.ProjectId != projectId))
        {
            return TypedResults.BadRequest(WrongProject);
        }
        return await Run(async () =>
        {
            await client.ApplyClaimFields(request);
            return TypedResults.Ok();
        });
    }

    /// <summary>Отказ сервиса — текстом, его показывают мастеру в строке плана.</summary>
    private static async Task<Results<Ok<T>, BadRequest<string>>> Run<T>(Func<Task<Ok<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (IsRefusal(ex))
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok, BadRequest<string>>> Run(Func<Task<Ok>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (IsRefusal(ex))
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    // Сервисы восстановления отвергают так неверный ввод (персонаж из другого проекта, значение поля и т. п.).
    // Проект заблокирован, в архиве, нет прав — WebApiExceptionHandler отдаст их по-русски с 409/403.
    private static bool IsRefusal(Exception ex)
        => ex is FormatException or JoinRpgBaseException or ArgumentException && !WebApiExceptionHandler.IsExpectedRefusal(ex);
}
