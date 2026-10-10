using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Чтение для слияния пользователей (ADR024).
/// </summary>
public interface IUserMergeRepository
{
    /// <summary>
    /// Конфликты по данным обоих аккаунтов и то, что переедет. Проверки самих аккаунтов (один и тот
    /// же, служебный) — на стороне сервиса, здесь их нет.
    /// </summary>
    Task<UserMergePreview> GetPreview(UserIdentification primary, UserIdentification secondary);
}
