namespace JoinRpg.Data.Write.Interfaces;

/// <summary>
/// Запись слияния пользователей (ADR024). Доступен только через
/// <see cref="IUnitOfWork.GetUserMergeWriteRepository"/>: изменения профиля, сделанные на трекаемых
/// сущностях того же <c>DbContext</c>, сохраняются в одной транзакции с переносом ссылок.
/// </summary>
public interface IUserMergeWriteRepository
{
    /// <summary>
    /// В одной транзакции: повторно проверяет конфликты, правит профиль, переписывает все ссылки с
    /// <paramref name="secondary"/> на <paramref name="primary"/> и удаляет <paramref name="secondary"/>.
    /// </summary>
    /// <param name="mergeProfile">
    /// Правка профиля основного на трекаемых сущностях этого же <c>DbContext</c>. Вызывается только
    /// после повторной проверки конфликтов, сохраняется в той же транзакции.
    /// </param>
    /// <returns>Сколько строк изменено по каждой колонке <c>Таблица.Колонка</c> — для журнала.</returns>
    Task<IReadOnlyDictionary<string, int>> MergeAndDelete(UserIdentification primary, UserIdentification secondary, Action mergeProfile);
}
