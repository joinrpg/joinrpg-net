namespace JoinRpg.Web.Claims.Finance;

/// <summary>
/// Что показать мастеру в диалоге смены размера базового взноса, чтобы не листать заявку.
/// </summary>
/// <param name="ClaimId">Заявка, у которой меняется базовый взнос.</param>
/// <param name="BaseFee">Текущий размер базового взноса.</param>
/// <param name="FeeVariants">Варианты для подсказки ввода из расписания взносов.</param>
/// <param name="HasFieldsWithFee">Показывать предупреждение, что итог = взнос + модификаторы полей.</param>
public record ChangeFeeView(
    ClaimIdentification ClaimId,
    int BaseFee,
    IReadOnlyCollection<int> FeeVariants,
    bool HasFieldsWithFee);
