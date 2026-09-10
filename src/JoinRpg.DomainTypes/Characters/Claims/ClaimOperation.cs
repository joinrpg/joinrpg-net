namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Операция с заявкой, для которой считаются причины запрета. От операции зависит и набор правил
/// (правила переноса не применяются к подаче), и то, какие причины вправе обойти мастер.
/// </summary>
/// <remarks>
/// Тут перечислены только те операции, для которых правила реально считаются. Ещё два мастерских
/// пути — <c>MoveToSecondRole</c> и <c>RestoreByMaster</c> — сейчас идут мимо общей валидации;
/// они появятся здесь вместе со своими исключениями из правил, см.
/// https://github.com/joinrpg/joinrpg-net/issues/4744.
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
        => operation is ClaimOperation.AddByMaster or ClaimOperation.MoveByMaster;

    /// <summary>Операция переносит уже существующую заявку, а не создаёт новую.</summary>
    public static bool IsMove(this ClaimOperation operation)
        => operation is ClaimOperation.MoveByMaster;
}
