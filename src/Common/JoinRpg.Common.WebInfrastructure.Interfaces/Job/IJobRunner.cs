using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Раннер зарегистрированной джобы: запускает её под имперсонацией в переданном scope.
/// Используется планировщиком, админской страницей джоб и тестами.
/// </summary>
public interface IJobRunner
{
    /// <summary>
    /// Запустить джобу в переданном scope под имперсонацией (см. <see cref="IJobImpersonator"/>).
    /// </summary>
    Task RunJob(IServiceScope serviceScope, CancellationToken cancellationToken);

    /// <summary>
    /// Короткое имя класса джобы (для отображения).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Полное имя класса джобы — оно же ключ журнала <see cref="IJobRepository"/>.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Тип джобы, на которой стоит раннер.
    /// </summary>
    Type JobType { get; }
}
