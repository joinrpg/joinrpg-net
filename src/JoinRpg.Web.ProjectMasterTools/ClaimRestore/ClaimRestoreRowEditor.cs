using JoinRpg.Web.Claims;

namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>
/// Строка плана с правками мастера: выбор персонажа и игрока для новой заявки, ответственный,
/// отметка «применять».
/// </summary>
public class ClaimRestoreRowEditor
{
    public ClaimRestoreRowEditor(ClaimRestoreRowViewModel row)
    {
        Row = row;
        // Атрибуты, которые, похоже, правили руками после восстановления, по умолчанию не меняем.
        bool ByDefault(ClaimRestoreAttributeKindView kind) => !row.AttributesNotByDefault.Contains(kind);
        Character = ByDefault(ClaimRestoreAttributeKindView.Character) ? row.Character?.CharacterId : null;
        NewCharacterName = row.CharacterName?.Trim() ?? "";
        Player = row.Player?.UserId;
        ManualPlayer = row.Action == ClaimRestoreActionView.Create && Player is null && row.PlayerCandidates.Count == 0;
        ResponsibleMaster = ByDefault(ClaimRestoreAttributeKindView.ResponsibleMaster) ? row.ResponsibleMaster : null;
        Status = ByDefault(ClaimRestoreAttributeKindView.Status) ? row.Status : null;
        ApplyFee = row.ManualFee is not null && ByDefault(ClaimRestoreAttributeKindView.ManualFee);
        // Строки с пометками мастер проверяет сам — по умолчанию не отмечены. «Правили руками» решается
        // по атрибутам выше, остальное в строке применяется.
        Selected = CanApply && BlockingProblem is null && row.ApplyByDefault
            && row.Warnings.All(w => w == ClaimRestoreWarningView.ChangedAfterRestore);
    }

    public ClaimRestoreRowViewModel Row { get; }

    public bool Selected { get; set; }

    /// <summary>Статус после применения; для «изменить» null — не менять.</summary>
    public ClaimStatusView? Status { get; set; }

    /// <summary>Ставить ли ручной взнос из плана.</summary>
    public bool ApplyFee { get; set; }

    /// <summary>
    /// Статус после применения будет тем, на который рассчитан план полей заявки: плановое изменение
    /// статуса не снято и не заменено другим.
    /// </summary>
    public bool AppliesPlannedStatus => Row.Status is null || Status == Row.Status;

    /// <summary>Статусы из разных источников — можно выбрать не самый свежий.</summary>
    public IReadOnlyList<RestoreSourcedValueView<ClaimStatusView>> StatusOptions
        => Row.Attributes?.Status is { } status ? [status.Chosen, .. status.Alternatives] : [];

    /// <summary>С какого момента статус такой: у выбранного варианта — его время.</summary>
    private DateTimeOffset? StatusChangedAt
        => Status is null ? null
            : Status == Row.Status ? Row.StatusChangedAt
            : StatusOptions.FirstOrDefault(o => o.Value == Status)?.At ?? Row.StatusChangedAt;

    /// <summary>
    /// Создать: существующий персонаж; null — новый с именем <see cref="NewCharacterName"/> (так же
    /// понимает null выбор персонажа с пунктом «новый персонаж»). Изменить: куда переместить; null — не перемещать.
    /// </summary>
    public CharacterIdentification? Character { get; set; }

    /// <summary>
    /// Создать нового персонажа с именем <see cref="NewCharacterName"/>. «Не выбрано» у «создать» не бывает:
    /// без найденного персонажа — новый; если подходящих по имени несколько, у строки пометка, и она
    /// по умолчанию не отмечена — мастер выберет сам.
    /// </summary>
    public bool NewCharacter => Row.Action == ClaimRestoreActionView.Create && Character is null;

    public string NewCharacterName { get; set; }

    /// <summary>Игрок новой заявки, выбранный из кандидатов.</summary>
    public UserIdentification? Player { get; set; }

    /// <summary>Игрок указан вручную ссылкой — <see cref="PlayerLinks"/>.</summary>
    public bool ManualPlayer { get; set; }

    public List<string> PlayerLinks { get; set; } = [];

    public UserIdentification? ResponsibleMaster { get; set; }

    /// <summary>Чем закончилось применение: null — не применяли.</summary>
    public bool? Succeeded { get; set; }

    public string? Error { get; set; }

    public bool CanApply => Row.Action != ClaimRestoreActionView.Nothing;

    private string? PlayerLink => PlayerLinks.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();

    /// <summary>Что мешает применить строку; null — можно применять.</summary>
    public string? BlockingProblem
    {
        get
        {
            if (NothingToChange)
            {
                return "Не выбрано ни одного изменения";
            }
            if (Row.Action != ClaimRestoreActionView.Create)
            {
                return null;
            }
            if (ManualPlayer ? PlayerLink is null : Player is null)
            {
                return "Укажите игрока";
            }
            if (NewCharacter && string.IsNullOrWhiteSpace(NewCharacterName))
            {
                return "Укажите имя нового персонажа";
            }
            if (ResponsibleMaster is null)
            {
                return "Выберите ответственного мастера";
            }
            return null;
        }
    }

    /// <summary>Персонажи, из которых можно выбрать (без повторов).</summary>
    public IReadOnlyList<ClaimRestoreCharacterOption> CharacterOptions
        => [.. new[] { Row.Character }.OfType<ClaimRestoreCharacterOption>()
            .Concat(Row.CharacterCandidates)
            .DistinctBy(c => c.CharacterId)];

    /// <summary>Запрос на применение.</summary>
    /// <param name="createdCharacters">
    /// Новые персонажи, уже созданные предыдущими строками, по имени: следующие заявки на того же
    /// нового персонажа подаются на него, а не создают второго.
    /// </param>
    public ClaimRestoreApplyRequest ToApplyRequest(IReadOnlyDictionary<string, CharacterIdentification> createdCharacters)
    {
        if (Row.Action == ClaimRestoreActionView.Create)
        {
            var name = NewCharacterName.Trim();
            CharacterIdentification? character = NewCharacter
                ? createdCharacters.GetValueOrDefault(name)
                : Character;
            return new ClaimRestoreApplyRequest(
                Row.ClaimId,
                Row.Action,
                character,
                NewCharacter && character is null ? name : null,
                ManualPlayer ? null : Player,
                ManualPlayer ? PlayerLink : null,
                Status,
                StatusChangedAt,
                ResponsibleMaster,
                ApplyFee ? Row.ManualFee : null,
                Row.FirstActivity);
        }

        var status = Status == Row.CurrentStatus ? null : Status;
        return new ClaimRestoreApplyRequest(
            Row.ClaimId,
            Row.Action,
            Character,
            NewCharacterName: null,
            Player: null,
            PlayerLink: null,
            status,
            status is null ? null : StatusChangedAt,
            ResponsibleMaster,
            ApplyFee ? Row.ManualFee : null,
            Row.FirstActivity);
    }

    /// <summary>В строке «изменить» не осталось ни одного изменения.</summary>
    private bool NothingToChange
        => Row.Action == ClaimRestoreActionView.Change
            && Character is null && ResponsibleMaster is null && !ApplyFee
            && (Status is null || Status == Row.CurrentStatus);

    /// <summary>
    /// Переносит из строки прежнего плана результат применения и — если строка не применилась
    /// (ошибка или не отмечена) и действие то же — отметку и правки мастера: после перезагрузки плана
    /// мастер видит ошибку рядом со своими правками, а снятые отметки не возвращаются.
    /// </summary>
    /// <remarks>
    /// Если новый план сам нашёл персонажа (например, созданного другой строкой), выбор персонажа
    /// берётся из плана: перенос «новый персонаж» создал бы второго с тем же именем.
    /// </remarks>
    public void CopyEditsFrom(ClaimRestoreRowEditor previous)
    {
        Succeeded = previous.Succeeded;
        Error = previous.Error;
        if (previous.Succeeded == true || previous.Row.Action != Row.Action)
        {
            return;
        }
        Selected = previous.Selected;
        if (Row.Character is null)
        {
            Character = previous.Character;
            NewCharacterName = previous.NewCharacterName;
        }
        Player = previous.Player;
        ManualPlayer = previous.ManualPlayer;
        PlayerLinks = [.. previous.PlayerLinks];
        ResponsibleMaster = previous.ResponsibleMaster;
        Status = previous.Status;
        ApplyFee = previous.ApplyFee && Row.ManualFee is not null;
    }

    /// <summary>Имя нового персонажа, которого создаст эта строка, — чтобы запомнить его после применения.</summary>
    public string? CreatesCharacterNamed(IReadOnlyDictionary<string, CharacterIdentification> createdCharacters)
    {
        if (Row.Action != ClaimRestoreActionView.Create || !NewCharacter)
        {
            return null;
        }
        var name = NewCharacterName.Trim();
        return createdCharacters.ContainsKey(name) ? null : name;
    }
}
