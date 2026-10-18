namespace JoinRpg.Common.WebComponents;

/// <summary>
/// Сервер отказал в операции и объяснил почему: <see cref="Exception.Message"/> можно показать пользователю как есть.
/// Клиент API бросает его, разобрав ProblemDetails из ответа.
/// </summary>
public class ServerProblemException(string message) : Exception(message);
