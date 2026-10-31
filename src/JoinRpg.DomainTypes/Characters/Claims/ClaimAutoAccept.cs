namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Что мешает принять заявку автоматически (см. <see cref="ClaimAutoAccept"/>).
/// </summary>
public enum AutoAcceptBlocker
{
    /// <summary>
    /// В шаблоне, на который подана заявка, не осталось свободных мест: мастер мог пригласить
    /// игрока сверх лимита (#4743), и принять такую заявку нельзя, пока лимит не поднимут.
    /// </summary>
    SlotExhausted,

    /// <summary>Проект требует доступа к защищённым данным, а игрок его не выдал.</summary>
    SensitiveDataNotGranted,
}

/// <summary>
/// Условия автоприёма заявки (<c>ProjectClaimSettings.AutoAcceptClaims</c>): по ним сервис заявок
/// решает, принять ли заявку сразу после подачи, игроку — показывают, почему автоприём пока
/// не сработал, а по их выполнении игроку дают кнопку «Автопринять мою заявку».
/// </summary>
public static class ClaimAutoAccept
{
    /// <summary>
    /// Автоприём действует на заявку: включён в проекте, заявка подана на шаблон (слот, #5645)
    /// и ещё ждёт решения. Для остальных заявок блокеры не спрашивают: автоприём для них
    /// не сработает, что бы игрок ни делал.
    /// </summary>
    public static bool AppliesTo(ClaimInfo claimInfo)
        => claimInfo.ProjectInfo.ClaimSettings.AutoAcceptClaims
           && claimInfo.Character.CharacterTypeInfo.IsSlot
           && claimInfo.Claim.Status is ClaimStatus.AddedByUser or ClaimStatus.Discussed;

    /// <summary>
    /// Что не хватает до срабатывания автоприёма: пустой список — заявку можно принимать
    /// автоматически. Вне <see cref="AppliesTo"/> список всегда пуст.
    /// </summary>
    public static IReadOnlyList<AutoAcceptBlocker> GetBlockers(ClaimInfo claimInfo)
    {
        if (!AppliesTo(claimInfo))
        {
            return [];
        }

        var blockers = new List<AutoAcceptBlocker>();

        // Мастер вправе пригласить игрока в шаблон сверх лимита (#4743), и принять такую заявку
        // нельзя, пока лимит не поднимут. null — лимита нет, места есть всегда.
        if (claimInfo.Character.CharacterTypeInfo.SlotLimit == 0)
        {
            blockers.Add(AutoAcceptBlocker.SlotExhausted);
        }

        if (!claimInfo.Claim.PlayerAllowedSensitiveData
            && claimInfo.ProjectInfo.ProfileRequirementSettings.SensitiveDataRequired)
        {
            blockers.Add(AutoAcceptBlocker.SensitiveDataNotGranted);
        }

        return blockers;
    }
}
