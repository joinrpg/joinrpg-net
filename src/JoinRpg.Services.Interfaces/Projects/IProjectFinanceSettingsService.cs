using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Services.Interfaces.Projects;

public class SetFinanceSettingsRequest
{
    public required ProjectIdentification ProjectId { get; init; }
    public required bool WarnOnOverPayment { get; init; }
    public required bool PreferentialFeeEnabled { get; init; }
    public required string? PreferentialFeeConditions { get; init; }
}

public class CreateFeeSettingRequest
{
    public required ProjectIdentification ProjectId { get; init; }
    public required int Fee { get; init; }
    /// <summary><c>null</c>, если льготный взнос не задан.</summary>
    public required int? PreferentialFee { get; init; }
    public required DateTime StartDate { get; init; }
}

public class CreatePaymentTypeRequest
{
    public required ProjectIdentification ProjectId { get; init; }
    /// <summary>Ответственный мастер. <c>null</c> для онлайн-оплаты — она не привязана к мастеру.</summary>
    public required UserIdentification? TargetMasterId { get; init; }
    public required PaymentTypeKind TypeKind { get; init; }
    /// <summary>Имя нужно только для <see cref="PaymentTypeKind.Custom"/>, у остальных оно своё.</summary>
    public required string? Name { get; init; }
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
