namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Фабрика раннеров джоб. Реализации в пакете контрактов нет: её регистрирует
/// <c>AddMidnightJobs</c> из JoinRpg.Common.WebInfrastructure вместе с планировщиком.
/// Нужна, чтобы <c>AddJob</c> мог зарегистрировать раннер джобы, не зная реализации.
/// </summary>
public interface IJobRunnerFactory
{
    /// <summary>
    /// Создать раннер для джобы с этим типом.
    /// </summary>
    IJobRunner Create(Type jobType);
}
