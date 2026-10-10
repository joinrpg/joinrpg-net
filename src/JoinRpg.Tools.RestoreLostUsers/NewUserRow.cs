namespace JoinRpg.Tools.RestoreLostUsers;

/// <summary>
/// Значения, которые пишутся в MSSQL для заготовки пользователя.
/// </summary>
/// <param name="SecurityStamp">
/// Случайный, а не пустой, как при обычной регистрации: cookie, выданная до потери БД, несёт старый
/// штамп и не должна снова заработать — все восстановленные пользователи входят заново.
/// </param>
internal record NewUserRow(int UserId, string Email, DateTime RegisterDateUtc, string SecurityStamp)
{
    public static NewUserRow From(LostUserDecision decision, Guid securityStamp)
    {
        if (decision.Kind != DecisionKind.Create || decision.Email is null)
        {
            throw new ArgumentException("Создавать можно только пользователя с решением Create и адресом почты.", nameof(decision));
        }

        // RegisterDate — datetime без часового пояса, приложение пишет туда DateTime.UtcNow.
        return new NewUserRow(decision.UserId, decision.Email, decision.FirstSeen.UtcDateTime, securityStamp.ToString());
    }
}
