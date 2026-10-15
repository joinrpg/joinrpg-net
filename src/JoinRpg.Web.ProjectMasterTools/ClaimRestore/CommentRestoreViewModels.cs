using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>Что мешает восстановить комментарии заявки (DRP, #5492).</summary>
public enum CommentRestoreWarningView
{
    [Display(Name = "Заявки нет в базе — сначала восстановите заявки выше")]
    ClaimNotFound,

    [Display(Name = "Номера части комментариев в базе заняты другим — эти комментарии не восстановятся")]
    CommentIdConflict,

    [Display(Name = "Часть уведомлений по заявке не разобралась — возможно, потерян комментарий, проверьте вручную")]
    UnparsedEvents,
}

/// <summary>Комментарий, который вернётся в обсуждение заявки.</summary>
/// <param name="CommentId">Прежний номер; null — в уведомлении его не было, база выдаст новый.</param>
/// <param name="IsVisibleToPlayer">Будет ли виден игроку.</param>
/// <param name="VisibilityQuestionable">Видимость определена эвристикой и может быть неверной — проверить вручную.</param>
/// <param name="ActionName">Действие, которое комментарий сопровождал; null — обычный комментарий.</param>
/// <param name="TextHtml">Текст (markdown), уже отрисованный и очищенный санитайзером на сервере.</param>
public record CommentRestoreCommentView(
    int? CommentId,
    DateTimeOffset CreatedAt,
    string AuthorName,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    bool VisibilityQuestionable,
    string? ActionName,
    string TextHtml);

/// <summary>План восстановления комментариев одной заявки.</summary>
/// <param name="ClaimExists">Заявка есть в базе; если нет — восстанавливать некуда.</param>
/// <param name="CharacterName">Имя персонажа в базе; null — заявки нет.</param>
/// <param name="PlayerName">Игрок заявки; null — заявки нет.</param>
/// <param name="Comments">Комментарии к восстановлению, по времени.</param>
/// <param name="SkippedExisting">Сколько комментариев из уведомлений уже есть в базе.</param>
/// <param name="Conflicts">Номера комментариев, которые в базе заняты другим.</param>
/// <param name="SkippedFinance">Сколько финансовых событий пропущено — их восстановят отдельно.</param>
/// <param name="Unparsed">Сколько уведомлений по заявке не разобралось.</param>
public record CommentRestoreRowViewModel(
    ClaimIdentification ClaimId,
    bool ClaimExists,
    string? CharacterName,
    string? PlayerName,
    IReadOnlyList<CommentRestoreCommentView> Comments,
    int SkippedExisting,
    IReadOnlyList<int> Conflicts,
    int SkippedFinance,
    int Unparsed,
    IReadOnlyList<CommentRestoreWarningView> Warnings);

/// <summary>План восстановления комментариев к заявкам проекта. Строки — в порядке номеров заявок.</summary>
/// <param name="UnavailableReason">Не null — восстановление недоступно, строк нет.</param>
public record CommentRestorePlanViewModel(
    ProjectIdentification ProjectId,
    ClaimRestoreUnavailableReason? UnavailableReason,
    IReadOnlyList<CommentRestoreRowViewModel> Rows);

/// <summary>Итог восстановления комментариев одной заявки.</summary>
/// <param name="Inserted">Сколько вставлено.</param>
/// <param name="AlreadyExisted">Сколько уже было в обсуждении.</param>
/// <param name="Conflicts">Номера, которые вставить не удалось: заняты другим.</param>
public record CommentRestoreApplyResult(int Inserted, int AlreadyExisted, IReadOnlyList<int> Conflicts);
