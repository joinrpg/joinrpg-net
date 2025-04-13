namespace JoinRpg.Helpers;

public static class CountHelper
{
    public static string DisplayCount(int count, string single, string multi1, string multi2)
    {
        var absCount = (uint)Math.Abs(count);
        var lastDigit = absCount % 10;
        var digitBeforeLast = (absCount % 100 - lastDigit) / 10;
        var selected = (digitBeforeLast, lastDigit) switch
        {
            (_, 0) => multi2,
            (1, _) => multi2,
            (_, 1) => single,
            (_, > 1 and < 5) => multi1,
            (_, >= 5) => multi2,
        };
        return count + " " + @selected;
    }
}
