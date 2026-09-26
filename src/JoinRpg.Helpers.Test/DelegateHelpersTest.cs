namespace JoinRpg.Helpers.Test;

public class DelegateHelpersTest
{
    [Fact]
    public async Task AsAlwaysTrueAsyncFunc_FromAction_RunsActionAndReturnsTrue()
    {
        var seen = 0;
        Action<int> action = value => seen = value;

        var result = await action.AsAlwaysTrueAsyncFunc()(42);

        result.ShouldBeTrue();
        seen.ShouldBe(42);
    }

    [Fact]
    public async Task AsAlwaysTrueAsyncFunc_FromAsyncAction_AwaitsItAndReturnsTrue()
    {
        var seen = 0;
        Func<int, Task> action = async value =>
        {
            await Task.Yield();
            seen = value;
        };

        var result = await action.AsAlwaysTrueAsyncFunc()(42);

        result.ShouldBeTrue();
        seen.ShouldBe(42);
    }

    /// <summary>
    /// Действие не должно выполняться при обёртывании — только при вызове полученной функции.
    /// Иначе ядро операции получило бы уже случившийся побочный эффект.
    /// </summary>
    [Fact]
    public void AsAlwaysTrueAsyncFunc_DoesNotRunActionUntilInvoked()
    {
        var called = false;
        Action<int> action = _ => called = true;

        _ = action.AsAlwaysTrueAsyncFunc();

        called.ShouldBeFalse();
    }
}
