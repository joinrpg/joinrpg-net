using JoinRpg.Helpers;

namespace JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
public class KogdaIgraConnectException(Exception inner, string message = "Ошибка при соединении с КогдаИгрой") : JoinRpgBaseException(inner, message);

public class KogdaIgraParseException(int Id, string message = "Ошибка при разборе ответа от КогдаИгры") : JoinRpgBaseException(message)
{
    public int Id { get; } = Id;
}
