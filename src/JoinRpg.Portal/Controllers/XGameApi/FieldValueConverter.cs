using System.Text.Json;

namespace JoinRpg.Portal.Controllers.XGameApi;

public static class FieldValueConverter
{
    public static Dictionary<int, string?> ConvertToStringValues(Dictionary<int, JsonElement> fieldValues)
    {
        var result = new Dictionary<int, string?>(fieldValues.Count);
        foreach (var (key, value) in fieldValues)
        {
            result[key] = ConvertElement(value);
        }
        return result;
    }

    private static string? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => ConvertArray(element),
            JsonValueKind.Object => throw new ArgumentException("Object values are not supported"),
            _ => throw new ArgumentException($"Unsupported JSON value kind: {element.ValueKind}"),
        };
    }

    private static string ConvertArray(JsonElement element)
    {
        var items = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                throw new ArgumentException("Array elements must be primitive values");
            }
            var converted = ConvertElement(item);
            if (converted is not null)
            {
                items.Add(converted);
            }
        }
        return string.Join(",", items);
    }
}
