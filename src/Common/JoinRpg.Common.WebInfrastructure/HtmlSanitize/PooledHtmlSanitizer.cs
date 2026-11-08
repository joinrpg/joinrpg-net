using Microsoft.Extensions.ObjectPool;
using Vereyon.Web;

namespace JoinRpg.Common.WebInfrastructure.HtmlSanitize;

/// <summary>
/// Базовая обёртка над <see cref="ObjectPool{T}"/> санитайзеров: берёт экземпляр при создании
/// и возвращает в пул при <see cref="Dispose"/>.
/// </summary>
internal abstract class PooledHtmlSanitizer : IJoinHtmlSanitizer
{
    private readonly ObjectPool<HtmlSanitizer> pool;
    private readonly HtmlSanitizer sanitizer;
    private bool disposed;

    protected PooledHtmlSanitizer(ObjectPool<HtmlSanitizer> pool)
    {
        this.pool = pool;
        sanitizer = pool.Get();
    }

    /// <inheritdoc />
    public string Sanitize(string html)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return sanitizer.Sanitize(html);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Идемпотентно: MS DI может вызвать Dispose повторно при разборе скоупа,
        // если потребитель уже освободил экземпляр через using.
        if (disposed)
        {
            return;
        }

        disposed = true;
        pool.Return(sanitizer);
    }
}

internal sealed class JoinSimpleHtmlSanitizer(ObjectPool<HtmlSanitizer> pool) : PooledHtmlSanitizer(pool), IJoinSimpleHtmlSanitizer;

internal sealed class JoinRemoveAllHtmlSanitizer(ObjectPool<HtmlSanitizer> pool) : PooledHtmlSanitizer(pool), IJoinRemoveAllHtmlSanitizer;

internal sealed class JoinTelegramHtmlSanitizer(ObjectPool<HtmlSanitizer> pool) : PooledHtmlSanitizer(pool), IJoinTelegramHtmlSanitizer;
