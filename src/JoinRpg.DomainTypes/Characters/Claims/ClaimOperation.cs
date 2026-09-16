namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Операция с заявкой, для которой считаются причины запрета. От операции зависит и набор правил
/// (правила переноса не применяются к подаче), и то, какие причины вправе обойти мастер.
/// </summary>
/// <remarks>
/// Правила считаются не для всех операций: <see cref="MoveToSecondRole"/> идёт мимо общей
/// валидации намеренно (см. <see cref="ClaimOperationExtensions.ValidatesClaimTarget"/>), а
/// <c>RestoreByMaster</c> здесь пока вообще не представлен. Оба появятся со своими исключениями
/// из правил, см. https://github.com/joinrpg/joinrpg-net/issues/4744.
/// </remarks>
public enum ClaimOperation
{
    /// <summary>Игрок подаёт заявку сам.</summary>
    AddByPlayer,

    /// <summary>Мастер приглашает игрока на роль.</summary>
    AddByMaster,

    /// <summary>Мастер переносит существующую заявку на другого персонажа.</summary>
    MoveByMaster,

    /// <summary>
    /// Мастер выпускает игрока на вторую роль: зарегистрированная заявка выходит из игры, а на
    /// другого персонажа создаётся новая, сразу утверждённая.
    /// </summary>
    MoveToSecondRole,

    /// <summary>
    /// Не операция, а показ: «может ли игрок сюда заявиться» на странице персонажа, в сетке
    /// ролей и на форме подачи заявки.
    /// </summary>
    DisplayForPlayer,
}

public static class ClaimOperationExtensions
{
    /// <summary>
    /// Операцию выполняет мастер, а значит часть причин запрета он вправе обойти — см.
    /// <see cref="ClaimForbiddenReason.MasterCanOverride"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="ClaimOperation.DisplayForPlayer"/> сюда не входит намеренно: страница, которую
    /// открыл мастер, не должна утверждать, что закрытый проект принимает заявки. Показ считается
    /// от лица игрока независимо от того, кто смотрит.
    /// </remarks>
    public static bool PerformedByMaster(this ClaimOperation operation)
        => operation is ClaimOperation.AddByMaster or ClaimOperation.MoveByMaster
            or ClaimOperation.MoveToSecondRole;

    /// <summary>Операция переносит уже существующую заявку, а не создаёт новую.</summary>
    public static bool IsMove(this ClaimOperation operation)
        => operation is ClaimOperation.MoveByMaster;

    /// <summary>
    /// Для операции считаются правила «можно ли сюда заявиться».
    /// </summary>
    /// <remarks>
    /// <see cref="ClaimOperation.MoveToSecondRole"/> их не считает, и это осознанно: до ADR014
    /// валидация переноса на вторую роль была <b>закомментирована</b> (<c>// TODO improve
    /// valitdation here</c>), а включать её заодно с миграцией нельзя — вторая роль выдаётся на
    /// полигоне в пик регистрации, и внезапно заработавшая проверка уронила бы операцию в самый
    /// неудачный момент. Флаг существует ровно затем, чтобы проход через общий путь создания не
    /// включил проверку молча; TODO остаётся TODO, см.
    /// https://github.com/joinrpg/joinrpg-net/issues/4744.
    /// </remarks>
    public static bool ValidatesClaimTarget(this ClaimOperation operation)
        => operation is not ClaimOperation.MoveToSecondRole;
}
