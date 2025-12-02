using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.PrimitiveTypes.Test;

/// <summary>
/// Эти enum не должны меняться, они хранятся в БД в виде числовых значений
/// </summary>
public class EnumVerifyTest
{
    [Fact]
    public Task CommentExtraAction_Snapshot()
    {
        var snapshot = Enum.GetValues<CommentExtraAction>()
            .Cast<CommentExtraAction>()
            .ToDictionary(
                k => k.ToString(),
                v => (int)v
            );

        return Verify(snapshot);
    }
}
