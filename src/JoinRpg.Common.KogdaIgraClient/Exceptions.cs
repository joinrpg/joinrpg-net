namespace JoinRpg.Common.KogdaIgraClient;

public class KogdaIgraConnectException(Exception inner, string message = "Ошибка при соединении с КогдаИгрой") : Exception(message, inner);

public class KogdaIgraParseException(int Id, string message = "Ошибка при разборе ответа от КогдаИгры") : Exception(message)
{
    public int Id { get; } = Id;
}
