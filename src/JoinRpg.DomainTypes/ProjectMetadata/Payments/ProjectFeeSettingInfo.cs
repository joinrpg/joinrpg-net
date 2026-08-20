namespace JoinRpg.DomainTypes.ProjectMetadata.Payments;

/// <summary>
/// Строка расписания взносов: с <paramref name="StartDate"/> действует такой взнос.
/// </summary>
/// <param name="StartDate">Дата, с которой действует взнос.</param>
/// <param name="Fee">Обычный взнос.</param>
/// <param name="PreferentialFee">Льготный взнос. <c>null</c>, если льготный взнос не задан.</param>
public record ProjectFeeSettingInfo(DateTime StartDate, int Fee, int? PreferentialFee);
