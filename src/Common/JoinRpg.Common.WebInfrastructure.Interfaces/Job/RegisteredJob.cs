namespace JoinRpg.Common.WebInfrastructure.Job;

/// <summary>
/// Маркер зарегистрированной джобы — чтобы знать полный список джоб без создания их зависимостей.
/// </summary>
public record class RegisteredJob(Type JobType);
