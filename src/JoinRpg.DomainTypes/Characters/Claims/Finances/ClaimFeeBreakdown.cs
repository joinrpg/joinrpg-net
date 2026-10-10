using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.DomainTypes.Characters.Claims.Finances;

/// <summary>
/// Взнос по платным полям одной привязки (к персонажу или к заявке).
/// </summary>
/// <param name="Fee">Сумма взносов за поля.</param>
/// <param name="FieldsWithFeeCount">Сколько полей вообще платные — даже если сейчас за них 0.</param>
public record class FieldsFeeSubtotal(int Fee, int FieldsWithFeeCount);

/// <summary>
/// Разбивка взноса заявки по слагаемым — то, из чего складывается <see cref="ClaimBalance.TotalFee"/>.
/// Считается <see cref="ClaimBalanceExtensions.CalculateFeeBreakdown(ClaimInCharacter, DateTime?)"/>.
/// </summary>
/// <remarks>
/// Нужна только странице заявки, где взнос расписан по строкам. Спискам, гриду и проблемам хватает
/// итогов из <see cref="ClaimBalance"/>: он компактный и уходит в острова (кабинет капитана), тащить
/// туда разбивку незачем.
/// </remarks>
/// <param name="BaseFee">Базовый взнос: зафиксированный в заявке или из расписания проекта на дату.</param>
/// <param name="IsBaseFeeFixed">Базовый взнос зафиксирован в заявке, а не взят из расписания.</param>
/// <param name="BaseFeeSetting">
/// Строка расписания проекта, действующая на дату. <c>null</c>, если взнос зафиксирован в заявке
/// (расписание тогда не важно) или на эту дату взнос в проекте не назначен.
/// </param>
/// <param name="CharacterFields">Платные поля персонажа.</param>
/// <param name="ClaimFields">Платные поля заявки.</param>
/// <param name="AccommodationFee">Стоимость выбранного проживания, 0 если проживание не выбрано.</param>
public record class ClaimFeeBreakdown(
    int BaseFee,
    bool IsBaseFeeFixed,
    ProjectFeeSettingInfo? BaseFeeSetting,
    FieldsFeeSubtotal CharacterFields,
    FieldsFeeSubtotal ClaimFields,
    int AccommodationFee)
{
    /// <summary>Есть ли у заявки базовый взнос — зафиксированный или по расписанию.</summary>
    public bool HasBaseFee => IsBaseFeeFixed || BaseFeeSetting is not null;

    /// <summary>Взнос за все платные поля.</summary>
    public int FieldsFee => CharacterFields.Fee + ClaimFields.Fee;

    /// <summary>Есть ли у заявки хоть одно платное поле.</summary>
    public bool HasFieldsWithFee => CharacterFields.FieldsWithFeeCount + ClaimFields.FieldsWithFeeCount > 0;

    /// <summary>Итоговый взнос: базовый + поля + проживание.</summary>
    public int TotalFee => BaseFee + FieldsFee + AccommodationFee;
}
