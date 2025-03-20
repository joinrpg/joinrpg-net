namespace JoinRpg.Helpers;

public static class CountHelper
{
    public static string DisplayCount(int count, string single, string multi1, string multi2)
    {
        var selected = count == 0 ? multi2 : (count == 1 ? single : (count < 5 ? multi1 : multi2));
        return count + " " + @selected;
    }
}
