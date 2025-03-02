namespace JoinRpg.PrimitiveTypes;
public record class UserIdentification(int Value) : SingleValueType<int>(Value)
{
    public static UserIdentification? FromOptional(int? userId)
        => userId is not null ? new UserIdentification(userId.Value) : null;
}
