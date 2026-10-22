using System.Globalization;
using Newtonsoft.Json;

namespace PscbApi;

/// <summary>
/// Writes a <see cref="DateTimeOffset"/> exactly the way the bank's own request examples show it:
/// six fractional digits and a UTC offset, e.g. <c>2015-05-01T00:00:00.000000+03:00</c>.
/// </summary>
/// <remarks>
/// Newtonsoft's default round-trip format drops the fraction when it is zero and produces
/// <c>2026-10-01T00:00:00+03:00</c>. The bank rejects such a body: <c>merchantApi/getPayments</c>
/// answers <c>STATUS_FAILURE</c> / <c>SERVER_ERROR</c> with an empty <c>errorDescription</c>.
/// Verified against the production endpoint on 2026-10-21.
/// <para>
/// Write-only on purpose: responses are parsed by the default reader, which already handles the
/// bank's own format (<c>2026-10-10T18:19:02.802+03:00</c>, three digits).
/// </para>
/// </remarks>
internal class PscbDateTimeOffsetConverter : JsonConverter
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.ffffffzzz";

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
        => objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTimeOffset offset)
        {
            writer.WriteValue(offset.ToString(Format, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNull();
        }
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        => throw new NotSupportedException($"{nameof(PscbDateTimeOffsetConverter)} is write-only");
}
