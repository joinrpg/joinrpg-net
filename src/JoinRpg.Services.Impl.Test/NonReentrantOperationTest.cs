namespace JoinRpg.Services.Impl.Test;

public class NonReentrantOperationTest
{
    private readonly NonReentrantOperation guard = new("TestService");

    [Fact]
    public void SequentialOperations_AreAllowed()
    {
        using (guard.Enter("first")) { }

        Should.NotThrow(() =>
        {
            using (guard.Enter("second")) { }
        });
    }

    [Fact]
    public void NestedOperation_Throws()
    {
        using var outer = guard.Enter("outer");

        var exception = Should.Throw<InvalidOperationException>(() => guard.Enter("inner"));

        exception.Message.ShouldContain("inner");
        exception.Message.ShouldContain("TestService");
    }

    /// <summary>
    /// Сторож обязан опускаться и при исключении — иначе первая же упавшая операция навсегда
    /// заблокировала бы экземпляр.
    /// </summary>
    [Fact]
    public void AfterException_GuardIsReleased()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            using (guard.Enter("failing"))
            {
                throw new InvalidOperationException("что-то пошло не так");
            }
        });

        Should.NotThrow(() =>
        {
            using (guard.Enter("next")) { }
        });
    }
}
