namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Админское слияние двух аккаунтов одного человека (ADR024). Поглощаемый аккаунт удаляется,
/// всё, что на него ссылалось, переходит к основному.
/// </summary>
public interface IUserMergeService
{
    /// <summary>
    /// Что произойдёт при слиянии и что ему мешает. Ничего не меняет.
    /// </summary>
    Task<UserMergePreview> GetPreview(UserIdentification primary, UserIdentification secondary);

    /// <summary>
    /// Слить <paramref name="secondary"/> в <paramref name="primary"/> и удалить его. Необратимо.
    /// </summary>
    /// <exception cref="UserMergeConflictException">Предпросмотр нашёл конфликты.</exception>
    Task Merge(UserIdentification primary, UserIdentification secondary);
}
