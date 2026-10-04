using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Additional time slot info for schedules. Beware of field names - serialized to JSON.
/// </summary>
/// <param name="LocalStartTime">Начало слота — время на часах в часовом поясе проекта, без смещения</param>
/// <param name="TimeSlotInMinutes">Длина слота в минутах</param>
public record TimeSlotOptions(
    [property: JsonPropertyName("StartTime"), JsonConverter(typeof(TimeSlotOptions.WallClockConverter))]
    DateTime LocalStartTime,
    int TimeSlotInMinutes)
{
    /// <summary>
    /// Length of time slot
    /// </summary>
    [JsonIgnore]
    public TimeSpan TimeSlotLength => TimeSpan.FromMinutes(TimeSlotInMinutes);

    /// <summary>
    /// Конец слота — время на часах в часовом поясе проекта
    /// </summary>
    [JsonIgnore]
    public DateTime LocalEndTime => LocalStartTime.Add(TimeSlotLength);

    /// <summary>
    /// Самое раннее допустимое начало слота. У краёв диапазона DateTime начало и конец
    /// в поясе проекта не посчитать — DateTimeOffset выходит за допустимые значения.
    /// </summary>
    public static readonly DateTime MinStartTime = new(2000, 1, 1);

    /// <summary>
    /// Самое позднее допустимое начало слота (не включая)
    /// </summary>
    public static readonly DateTime MaxStartTime = new(2100, 1, 1);

    /// <summary>
    /// Начало слота в допустимых границах
    /// </summary>
    [JsonIgnore]
    public bool HasValidStartTime => LocalStartTime >= MinStartTime && LocalStartTime < MaxStartTime;

    /// <summary>
    /// Слот положительной длины. Огромная отрицательная длина уводит конец за начало диапазона DateTime.
    /// Сверху длина не ограничена: int минут — около 4000 лет, переполнения нет.
    /// </summary>
    [JsonIgnore]
    public bool HasValidLength => TimeSlotInMinutes > 0;

    /// <summary>
    /// Начало и конец слота можно посчитать в поясе проекта
    /// </summary>
    [JsonIgnore]
    public bool IsValid => HasValidStartTime && HasValidLength;

    /// <summary>
    /// Начало слота в часовом поясе проекта
    /// </summary>
    public DateTimeOffset GetStartTime(TimeZoneInfo projectTimeZone)
        => new(LocalStartTime, projectTimeZone.GetUtcOffset(LocalStartTime));

    /// <summary>
    /// Конец слота в часовом поясе проекта. Длина слота — реальные минуты, даже если внутри переход на летнее время.
    /// </summary>
    public DateTimeOffset GetEndTime(TimeZoneInfo projectTimeZone)
        => TimeZoneInfo.ConvertTime(GetStartTime(projectTimeZone).Add(TimeSlotLength), projectTimeZone);

    /// <summary>
    /// Слот по умолчанию: начинается сейчас по времени часового пояса проекта
    /// </summary>
    public static TimeSlotOptions CreateDefault(TimeZoneInfo timeZone) => CreateDefault(timeZone, DateTimeOffset.UtcNow);

    /// <summary>
    /// Слот по умолчанию: начинается в момент <paramref name="now"/> по времени пояса <paramref name="timeZone"/>
    /// </summary>
    public static TimeSlotOptions CreateDefault(TimeZoneInfo timeZone, DateTimeOffset now)
        => new(TimeZoneInfo.ConvertTime(now, timeZone).DateTime, TimeSlotInMinutes: 50);

    /// <summary>
    /// Разбирает значение, сохранённое в ProgrammaticValue варианта
    /// </summary>
    public static TimeSlotOptions? FromJson(string json) => JsonSerializer.Deserialize<TimeSlotOptions>(json);

    /// <summary>
    /// Значение для сохранения в ProgrammaticValue варианта
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this);

    /// <summary>
    /// Время на часах без смещения. Раньше начало слота сохранялось со смещением — его игнорируем:
    /// время всегда считается в часовом поясе проекта.
    /// </summary>
    internal class WallClockConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString() ?? throw new JsonException("Не задано начало таймслота");
            // Смещение, если оно есть, отбрасываем; без смещения DateTimeOffset.Parse подставит пояс сервера,
            // но время на часах (DateTime) от этого не меняется
            return DateTime.SpecifyKind(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).DateTime, DateTimeKind.Unspecified);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
