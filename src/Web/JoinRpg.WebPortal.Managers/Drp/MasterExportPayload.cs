using System.IO.Compression;
using System.Text.Json;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Services.Drp.MasterExports;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Разобранная выгрузка мастера как непрозрачная строка для острова (DRP, #5529): выгрузки на сервере
/// не хранятся, остров держит строку у себя и присылает её при построении плана. JSON, сжатый gzip, в base64.
/// </summary>
/// <remarks>
/// Подписи нет сознательно: подделать строку может только мастер с правом управлять заявками, а он с тем же
/// успехом может загрузить поддельный файл. Права и значения проверяют доменные сервисы при применении.
/// </remarks>
internal static class MasterExportPayload
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

    public static string Serialize(ParsedMasterExport export)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            JsonSerializer.Serialize(gzip, export, Options);
        }
        return Convert.ToBase64String(buffer.ToArray());
    }

    /// <summary>
    /// Сколько может занимать распакованная выгрузка: с большим запасом над JSON выгрузки из файла
    /// предельного размера (30 МБ), но не даёт gzip-бомбе съесть память.
    /// </summary>
    public const long MaxDecompressedSize = 10L * 30 * 1024 * 1024;

    /// <exception cref="FormatException">Строка — не разобранная выгрузка.</exception>
    public static ParsedMasterExport Deserialize(string? payload) => Deserialize(payload, MaxDecompressedSize);

    /// <param name="maxDecompressedSize">Предел распакованного размера — параметром для тестов.</param>
    /// <exception cref="FormatException">Строка — не разобранная выгрузка или распаковывается больше предела.</exception>
    internal static ParsedMasterExport Deserialize(string? payload, long maxDecompressedSize)
    {
        if (string.IsNullOrEmpty(payload))
        {
            throw new FormatException("Empty master export payload");
        }
        ParsedMasterExport? export;
        try
        {
            using var gzip = new GZipStream(new MemoryStream(Convert.FromBase64String(payload)), CompressionMode.Decompress);
            using var limited = new LimitedReadStream(gzip, maxDecompressedSize);
            export = JsonSerializer.Deserialize<ParsedMasterExport>(limited, Options);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or NotSupportedException)
        {
            throw new FormatException("Invalid master export payload", ex);
        }
        return Validate(export);
    }

    /// <summary>
    /// JSON без обязательных коллекций System.Text.Json разбирает в null — проверяем сами, чтобы
    /// испорченная строка давала понятный отказ, а не падение где-то в планировщике.
    /// </summary>
    private static ParsedMasterExport Validate(ParsedMasterExport? export)
    {
        static bool Valid(ParsedMasterExport? e)
            => e is not null
                && Enum.IsDefined(e.Kind)
                && e.OtherHeaders is not null && e.OtherHeaders.All(h => h is not null)
                && e.Warnings is not null
                && e.Rows is not null
                && e.Rows.All(r => r is not null
                    && r.Groups is not null && r.Groups.All(g => g is not null && g.Name is not null)
                    && r.OtherCells is not null && r.OtherCells.All(c => c is not null && c.Header is not null && c.Value is not null)
                    && r.Warnings is not null
                    && (r.CharacterType is not { } type
                        || (Enum.IsDefined(type.Type) && !(type.Type == CharacterType.NonPlayer && type.IsHot))));

        return Valid(export) ? export! : throw new FormatException("Incomplete master export payload");
    }

    /// <summary>Поток только на чтение, который отказывается читать больше заданного.</summary>
    private sealed class LimitedReadStream(Stream inner, long limit) : Stream
    {
        private long read;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => read; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => Count(await inner.ReadAsync(buffer, cancellationToken));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        private int Count(int bytes)
        {
            read += bytes;
            return read > limit ? throw new InvalidDataException("Master export payload is too large") : bytes;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
