using System.Data;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Finance;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Панели страницы «Настройка финансов». Право на деньги проекта проверяет сервис.
/// </summary>
/// <remarks>
/// <c>[IgnoreAntiforgeryToken]</c> здесь сознательно нет: остров присылает токен заголовком,
/// и глобальный фильтр antiforgery закрывает и эти ручки.
/// </remarks>
[Route("/webapi/{projectId}/finance-settings/[action]")]
[RequireMaster]
[ApiController]
public class FinanceSettingsController(IFinanceSettingsClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FinanceGeneralSettingsViewModel>> GetGeneralSettings(ProjectIdentification projectId)
        => Ok(await client.GetGeneralSettings(projectId));

    [HttpPost]
    public async Task<ActionResult> SaveGeneralSettings(
        ProjectIdentification projectId,
        FinanceGeneralSettingsViewModel model)
    {
        model.ProjectId = projectId;
        await client.SaveGeneralSettings(model);
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<FinanceFeeSettingsViewModel>> GetFeeSettings(ProjectIdentification projectId)
        => Ok(await client.GetFeeSettings(projectId));

    [HttpPost]
    public async Task<ActionResult> CreateFeeSetting(ProjectIdentification projectId, CreateFeeSettingViewModel model)
        => await FeeRefusalToBadRequest(() => client.CreateFeeSetting(projectId, model));

    [HttpPost]
    public async Task<ActionResult> DeleteFeeSetting(ProjectIdentification projectId, [FromQuery] int projectFeeSettingId)
        => await FeeRefusalToBadRequest(() => client.DeleteFeeSetting(projectId, projectFeeSettingId));

    [HttpGet]
    public async Task<ActionResult<FinancePaymentTypesViewModel>> GetPaymentTypes(ProjectIdentification projectId)
        => Ok(await client.GetPaymentTypes(projectId));

    [HttpPost]
    public async Task<ActionResult> CreatePaymentType(ProjectIdentification projectId, NewPaymentTypeViewModel model)
    {
        // Без имени сервис бросит ArgumentException, а ловить его целиком — значит прятать любой другой сбой
        if (model.Kind == PaymentTypeKind.Custom && string.IsNullOrWhiteSpace(model.Name))
        {
            return BadRequest("Укажите название метода оплаты");
        }
        return await PaymentTypeRefusalToBadRequest(() => client.CreatePaymentType(projectId, model));
    }

    [HttpPost]
    public async Task<ActionResult> TogglePaymentType(ProjectIdentification projectId, [FromQuery] int paymentTypeId)
        => await PaymentTypeRefusalToBadRequest(() => client.TogglePaymentType(new PaymentTypeIdentification(projectId, paymentTypeId)));

    [HttpGet]
    public async Task<ActionResult<FinanceGoogleDocsExportViewModel>> GetGoogleDocsExport(ProjectIdentification projectId)
        => Ok(await client.GetGoogleDocsExport(projectId));

    /// <summary>Отказы сервиса по методам оплаты — текстом: остров показывает его мастеру.</summary>
    private async Task<ActionResult> PaymentTypeRefusalToBadRequest(Func<Task> action)
    {
        try
        {
            await action();
            return Ok();
        }
        catch (MustBeAdminException)
        {
            return BadRequest("Включить онлайн-оплату может только администратор сайта");
        }
        catch (JoinRpgInvalidUserException)
        {
            return BadRequest("У этого мастера уже есть метод оплаты «Наличными». Обновите страницу.");
        }
        catch (DataException)
        {
            return BadRequest("Этот метод оплаты в проекте уже есть. Обновите страницу.");
        }
        catch (JoinRpgEntityNotFoundException)
        {
            return BadRequest("Метод оплаты не найден. Обновите страницу и попробуйте ещё раз.");
        }
    }

    /// <summary>Отказы сервиса по расписанию взносов — текстом: остров показывает его мастеру.</summary>
    private async Task<ActionResult> FeeRefusalToBadRequest(Func<Task> action)
    {
        try
        {
            await action();
            return Ok();
        }
        catch (CannotPerformOperationInPast)
        {
            return BadRequest("Нельзя менять взнос, который уже вступил в силу, или вводить его задним числом");
        }
        catch (PreferentialFeeNotEnabled)
        {
            return BadRequest("Льготный взнос в проекте выключен — включите его в общих настройках");
        }
        catch (JoinRpgEntityNotFoundException)
        {
            return BadRequest("Взнос не найден. Обновите страницу и попробуйте ещё раз.");
        }
    }
}
