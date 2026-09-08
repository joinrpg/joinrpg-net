using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Services.Interfaces.Projects;

public class SetFinanceSettingsRequest
{
    public int ProjectId { get; set; }
    public bool WarnOnOverPayment { get; set; }
    public bool PreferentialFeeEnabled { get; set; }
    public required string? PreferentialFeeConditions { get; set; }
}

public class CreateFeeSettingRequest
{
    public int ProjectId { get; set; }
    public int Fee { get; set; }
    public int? PreferentialFee { get; set; }
    public DateTime StartDate { get; set; }
}

public class CreatePaymentTypeRequest
{
    public int ProjectId { get; set; }
    public UserIdentification? TargetMasterId { get; set; }
    public PaymentTypeKind TypeKind { get; set; }
    public required string? Name { get; set; }
}

/// <summary>
/// Настройки финансов проекта: типы оплаты, расписание взносов, общие финансовые флаги. Всё, что
/// меняет этот сервис, попадает в снапшот метаданных проекта
/// (<c>ProjectInfo.ProjectFinanceSettings</c>), поэтому изменения идут через <c>ProjectPropsService</c>.
/// Операции по конкретным заявкам (приём взноса, переводы) — в <see cref="IFinanceService"/>.
/// </summary>
public interface IProjectFinanceSettingsService
{
    /// <summary>
    /// Создаёт тип оплаты указанного вида для проекта и ответственного мастера.
    /// </summary>
    Task CreatePaymentType(CreatePaymentTypeRequest request);

    /// <summary>
    /// Включает или выключает тип оплаты. Выключение — всегда soft-delete
    /// (<c>PaymentType.IsActive = false</c>), физически тип оплаты не удаляется.
    /// </summary>
    Task TogglePaymentActiveness(ProjectIdentification projectId, int paymentTypeId);

    Task EditCustomPaymentType(ProjectIdentification projectId, int paymentTypeId, string name, bool isDefault);

    Task CreateFeeSetting(CreateFeeSettingRequest request);

    Task DeleteFeeSetting(ProjectIdentification projectId, int projectFeeSettingId);

    Task SaveGlobalSettings(SetFinanceSettingsRequest request);
}
