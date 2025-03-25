namespace JoinRpg.Helpers;

public static class CountHelper
{
    public static string DisplayCount(int count, string single, string multi1, string multi2)
    {
        var absCount = (uint)Math.Abs(count);
        var mod = absCount % 10;
        var selected = mod switch
        {
            0 => multi2,
            1 when absCount % 100 == 11 => multi2,
            1 => single,
            > 1 and < 5 => multi1,
            >= 5 => multi2,
        };
        return count + " " + @selected;
    }
}
