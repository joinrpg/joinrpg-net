namespace JoinRpg.Common.PrimitiveTypes;

/// <summary>
/// Результат отправки
/// </summary>
/// <param name="Succeeded"></param>
/// <param name="Repeatable">
/// Когда <see cref="Succeeded"/>=false, обозначает ошибку, которую есть смысл повторять
/// </param>
/// <param name="Common">
/// Проблема связана не с конкретным сообщением, а с общим статусом сервиса (нужно произвести кулдаун, но не увеличивать счетчик попыток для отправки)
/// </param>
public record struct SendingResult(bool Succeeded, bool Repeatable, bool Common)
{
    public static SendingResult Success() => new(true, false, false);

    public static SendingResult RepeatableFailure() => new(false, true, false);
    public static SendingResult CommonFailure() => new(false, true, true);
}
