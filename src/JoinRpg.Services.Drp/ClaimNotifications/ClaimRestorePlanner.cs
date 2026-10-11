using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>Заявка в восстановленной базе.</summary>
public record ExistingClaimInfo(
    ClaimIdentification ClaimId,
    CharacterIdentification CharacterId,
    UserIdentification Player,
    ClaimStatus Status,
    UserIdentification ResponsibleMaster,
    int? ManualFee);

/// <summary>Персонаж в восстановленной базе.</summary>
/// <param name="IsSlot">
/// Слот: принятой заявка на нём не бывает — одобрение создаёт из слота нового персонажа, по умолчанию
/// с тем же именем.
/// </param>
/// <param name="DefaultResponsibleMaster">Ответственный по правилам проекта для новой заявки на него.</param>
public record ExistingCharacterInfo(
    CharacterIdentification CharacterId,
    string Name,
    bool IsActive,
    bool IsSlot,
    UserIdentification DefaultResponsibleMaster);

/// <summary>Мастер проекта — для сопоставления ответственного по имени из уведомлений.</summary>
public record ProjectMasterName(UserIdentification UserId, string DisplayName);

/// <summary>Всё, по чему строится план восстановления заявок одного проекта.</summary>
/// <param name="Events">События по заявкам проекта до момента аварии (<see cref="ClaimNotificationParser"/>).</param>
/// <param name="DefaultResponsibleMaster">Ответственный по правилам проекта для нового персонажа в корневой группе.</param>
/// <param name="BackupTakenAt">Момент бэкапа: события до него в восстановленной базе уже учтены.</param>
public record ClaimRestoreSource(
    IReadOnlyCollection<ClaimNotificationEvent> Events,
    IReadOnlyCollection<ExistingClaimInfo> Claims,
    IReadOnlyCollection<ExistingCharacterInfo> Characters,
    IReadOnlyCollection<ProjectMasterName> Masters,
    UserIdentification DefaultResponsibleMaster,
    DateTimeOffset BackupTakenAt);

/// <summary>Одно действие на заявку.</summary>
public enum ClaimRestoreAction
{
    /// <summary>Заявки нет в восстановленной базе — создать с прежним id.</summary>
    Create,

    /// <summary>Заявка есть, но после бэкапа менялась — привести к состоянию на момент аварии.</summary>
    Change,

    /// <summary>Заявка уже такая, как по уведомлениям.</summary>
    Nothing,
}

/// <summary>В чём план не уверен — мастер проверяет и правит.</summary>
public enum ClaimRestoreWarning
{
    PlayerNotFound,
    PlayerAmbiguous,

    /// <summary>Имя ответственного из уведомлений не совпало ни с одним мастером (или с несколькими).</summary>
    ResponsibleNotFound,

    /// <summary>
    /// Под имя персонажа подходит несколько персонажей проекта — выбрать из
    /// <see cref="ClaimRestorePlanRow.CharacterCandidates"/>; без выбора создавать нельзя.
    /// </summary>
    CharacterAmbiguous,

    /// <summary>Имени персонажа в уведомлениях нет — указать персонажа вручную.</summary>
    CharacterNotFound,

    /// <summary>Заявку перемещали, но персонажа с новым именем в базе нет.</summary>
    MovedCharacterNotFound,

    /// <summary>Имя персонажа другое, а перемещений не было — похоже на переименование, его план не делает.</summary>
    CharacterRenamed,

    /// <summary>
    /// Принятая заявка на слоте: одобрение создаёт из слота нового персонажа. Для потерянной заявки план
    /// создаёт его сам; для заявки из базы — выбрать персонажа вручную.
    /// </summary>
    ApprovedOnSlot,

    /// <summary>По уведомлениям не понять, в каком статусе заявка.</summary>
    StatusUnknown,

    /// <summary>
    /// Заявки нет в базе, но ни события о подаче после бэкапа, ни подачи вовсе — проверить, та ли это
    /// заявка (ошибка момента бэкапа, уведомление о подаче без получателей).
    /// </summary>
    LostClaimNotSubmittedAfterBackup,
}

/// <summary>
/// Строка плана. Для <see cref="ClaimRestoreAction.Change"/> заполнено только то, что меняется;
/// null — оставить как есть.
/// </summary>
/// <param name="CharacterName">Имя персонажа по уведомлениям.</param>
/// <param name="Character">
/// Создать: найденный по имени персонаж из базы; null — создать нового с именем <paramref name="CharacterName"/>
/// (несколько строк с одним таким именем — один новый персонаж: следующие подавать на созданного первой).
/// Изменить: персонаж, на которого заявку переместили.
/// </param>
/// <param name="CharacterCandidates">Подходящие по имени персонажи, если однозначного нет.</param>
public record ClaimRestorePlanRow(
    ClaimIdentification ClaimId,
    ClaimRestoreAction Action,
    string? CharacterName,
    CharacterIdentification? Character,
    IReadOnlyCollection<CharacterIdentification> CharacterCandidates,
    UserIdentification? Player,
    IReadOnlyCollection<UserIdentification> PlayerCandidates,
    ClaimStatus? Status,
    DateTimeOffset? StatusChangedAt,
    UserIdentification? ResponsibleMaster,
    int? ManualFee,
    DateTimeOffset FirstActivity,
    DateTimeOffset LastActivity,
    IReadOnlyCollection<ClaimRestoreWarning> Warnings);

/// <summary>
/// Сверяет то, что известно о заявках по уведомлениям, с восстановленной базой и предлагает по одному
/// действию на заявку (DRP, #5481).
/// </summary>
public static class ClaimRestorePlanner
{
    /// <returns>
    /// Строки в порядке применения: сначала те, после которых заявка не принята, — они освобождают
    /// персонажей для принятых. Заявки без событий после бэкапа в план не попадают.
    /// </returns>
    public static IReadOnlyList<ClaimRestorePlanRow> Plan(ClaimRestoreSource source)
    {
        var claims = source.Claims.ToDictionary(c => c.ClaimId);
        var characters = source.Characters.ToDictionary(c => c.CharacterId);

        var rows = new List<(ClaimRestorePlanRow Row, ClaimStatus? FinalStatus)>();
        foreach (var group in source.Events.GroupBy(e => e.ClaimId))
        {
            if (claims.TryGetValue(group.Key, out var existing))
            {
                var row = PlanExisting(source, characters, existing, [.. group.Where(e => e.CreatedAt >= source.BackupTakenAt)]);
                if (row is not null)
                {
                    rows.Add((row, row.Status ?? existing.Status));
                }
            }
            else
            {
                var row = PlanLost(source, group.Key, [.. group]);
                rows.Add((row, row.Status));
            }
        }

        return [.. rows
            .OrderBy(r => r.FinalStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn)
            .ThenBy(r => r.Row.ClaimId.ClaimId)
            .Select(r => r.Row)];
    }

    private static ClaimRestorePlanRow PlanLost(ClaimRestoreSource source, ClaimIdentification claimId, IReadOnlyCollection<ClaimNotificationEvent> events)
    {
        var state = RestoredClaimStateBuilder.Build(claimId, events);
        var warnings = ConvertWarnings(state.Warnings);
        var approved = state.Status is ClaimStatus.Approved or ClaimStatus.CheckedIn;

        IReadOnlyCollection<CharacterIdentification> candidates = [];
        CharacterIdentification? character = null;
        if (state.CharacterName is null)
        {
            warnings.Add(ClaimRestoreWarning.CharacterNotFound);
        }
        else
        {
            var matches = FindCharactersByName(source, state.CharacterName);
            if (approved && matches.Any(c => c.IsSlot))
            {
                // Одобрение слота создало персонажа с его именем: он и потерян — создать заново.
                warnings.Add(ClaimRestoreWarning.ApprovedOnSlot);
                matches = [.. matches.Where(c => !c.IsSlot)];
            }
            (character, candidates) = Choose(matches, warnings);
        }

        var defaultResponsible = character is not null
            ? source.Characters.Single(c => c.CharacterId == character).DefaultResponsibleMaster
            : source.DefaultResponsibleMaster;
        var responsible = FindResponsible(source, state, warnings) ?? defaultResponsible;

        if (state.Status is null)
        {
            warnings.Add(ClaimRestoreWarning.StatusUnknown);
        }
        if (!state.IsNewClaim || state.FirstActivity < source.BackupTakenAt)
        {
            warnings.Add(ClaimRestoreWarning.LostClaimNotSubmittedAfterBackup);
        }

        return new ClaimRestorePlanRow(
            claimId,
            ClaimRestoreAction.Create,
            state.CharacterName,
            character,
            candidates,
            state.Player,
            state.PlayerCandidates,
            state.Status,
            state.StatusChangedAt,
            responsible,
            state.ManualFee,
            state.FirstActivity,
            state.LastActivity,
            warnings);
    }

    private static ClaimRestorePlanRow? PlanExisting(
        ClaimRestoreSource source,
        IReadOnlyDictionary<CharacterIdentification, ExistingCharacterInfo> characters,
        ExistingClaimInfo existing,
        IReadOnlyCollection<ClaimNotificationEvent> events)
    {
        if (events.Count == 0)
        {
            return null;
        }

        var state = RestoredClaimStateBuilder.Build(existing.ClaimId, events, existing.Status, existing.Player);
        // Игрок у заявки из базы известен — неуверенность в нём по уведомлениям не важна.
        var warnings = new List<ClaimRestoreWarning>();

        // Перемещение между тёзками по имени не видно — такое мастер поправит сам.
        CharacterIdentification? moveTo = null;
        IReadOnlyCollection<CharacterIdentification> candidates = [];
        var current = characters.GetValueOrDefault(existing.CharacterId);
        if (state.CharacterName is not null && !SameName(state.CharacterName, current?.Name))
        {
            if (state.MayHaveChangedCharacter)
            {
                var matches = FindCharactersByName(source, state.CharacterName);
                if (matches.Count == 0)
                {
                    warnings.Add(ClaimRestoreWarning.MovedCharacterNotFound);
                }
                (moveTo, candidates) = Choose(matches, warnings);
                if (moveTo == existing.CharacterId)
                {
                    moveTo = null;
                }
            }
            else
            {
                warnings.Add(ClaimRestoreWarning.CharacterRenamed);
            }
        }

        ClaimStatus? status = state.Status != existing.Status ? state.Status : null;
        var finalStatus = status ?? existing.Status;
        var finalCharacter = moveTo is { } target ? characters.GetValueOrDefault(target) : current;
        if (finalStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn && finalCharacter?.IsSlot == true)
        {
            warnings.Add(ClaimRestoreWarning.ApprovedOnSlot);
        }

        var responsible = FindResponsible(source, state, warnings);
        UserIdentification? responsibleChange = responsible is not null && responsible != existing.ResponsibleMaster ? responsible : null;
        int? feeChange = state.ManualFee is not null && state.ManualFee != existing.ManualFee ? state.ManualFee : null;

        var changed = moveTo is not null || status is not null || responsibleChange is not null || feeChange is not null;

        return new ClaimRestorePlanRow(
            existing.ClaimId,
            changed ? ClaimRestoreAction.Change : ClaimRestoreAction.Nothing,
            state.CharacterName,
            moveTo,
            candidates,
            existing.Player,
            [existing.Player],
            status,
            status is null ? null : state.StatusChangedAt,
            responsibleChange,
            feeChange,
            state.FirstActivity,
            state.LastActivity,
            warnings);
    }

    /// <summary>Активные персонажи проекта с таким именем (без учёта пробелов по краям).</summary>
    private static IReadOnlyList<ExistingCharacterInfo> FindCharactersByName(ClaimRestoreSource source, string name)
        => [.. source.Characters.Where(c => c.IsActive && SameName(c.Name, name))];

    private static bool SameName(string? left, string? right) => left?.Trim() == right?.Trim();

    /// <summary>Однозначное совпадение — персонаж; несколько — кандидаты и пометка.</summary>
    private static (CharacterIdentification? Character, IReadOnlyCollection<CharacterIdentification> Candidates) Choose(
        IReadOnlyList<ExistingCharacterInfo> matches, List<ClaimRestoreWarning> warnings)
    {
        switch (matches.Count)
        {
            case 0:
                return (null, []);
            case 1:
                return (matches[0].CharacterId, []);
            default:
                warnings.Add(ClaimRestoreWarning.CharacterAmbiguous);
                return (null, [.. matches.Select(c => c.CharacterId)]);
        }
    }

    /// <summary>Ответственный по имени из последней смены; null — смены не было или имя не сопоставилось.</summary>
    private static UserIdentification? FindResponsible(ClaimRestoreSource source, RestoredClaimState state, List<ClaimRestoreWarning> warnings)
    {
        if (state.ResponsibleMasterName is not { } name)
        {
            return null;
        }
        var matches = source.Masters.Where(m => m.DisplayName == name).ToList();
        if (matches.Count == 1)
        {
            return matches[0].UserId;
        }
        warnings.Add(ClaimRestoreWarning.ResponsibleNotFound);
        return null;
    }

    private static List<ClaimRestoreWarning> ConvertWarnings(IEnumerable<RestoredClaimWarning> warnings)
        => [.. warnings.Select(w => w switch
        {
            RestoredClaimWarning.PlayerNotFound => ClaimRestoreWarning.PlayerNotFound,
            RestoredClaimWarning.PlayerAmbiguous => ClaimRestoreWarning.PlayerAmbiguous,
            _ => throw new ArgumentOutOfRangeException(nameof(warnings), w, null),
        })];
}
