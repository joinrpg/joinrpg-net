using System.Collections.Concurrent;
using Vereyon.Web;

namespace JoinRpg.Markdown;

internal static partial class HtmlSanitizers
{
    /// <summary>
    /// Represents a one-use HTML sanitizer which should not be shared and has to be disposed immediately after use.
    /// </summary>
    public interface IDisposableHtmlSanitizer : IHtmlSanitizer, IDisposable;

    private class DisposableHtmlSanitizer : IDisposableHtmlSanitizer
    {
        private readonly IHtmlSanitizer _sanitizer;
        private readonly Action<IHtmlSanitizer> _onDispose;
        private bool _disposed = false;

        public DisposableHtmlSanitizer(IHtmlSanitizer sanitizer, Action<IHtmlSanitizer> onDispose)
        {
            ArgumentNullException.ThrowIfNull(sanitizer);
            ArgumentNullException.ThrowIfNull(onDispose);
            _sanitizer = sanitizer;
            _onDispose = onDispose;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("Object has already been disposed");
            }

            _disposed = true;
            _onDispose(_sanitizer);
        }

        /// <inheritdoc />
        public string Sanitize(string html) => _sanitizer.Sanitize(html);
    }

    internal class HtmlSanitizerPool
    {
        private readonly Func<IHtmlSanitizer> _factory;
        private readonly ConcurrentBag<IHtmlSanitizer> _pool = new();

        public int Count => _pool.Count;

        public HtmlSanitizerPool(Func<IHtmlSanitizer> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            _factory = factory;
        }

        public IDisposableHtmlSanitizer Get()
        {
            if (!_pool.TryTake(out IHtmlSanitizer? sanitizer))
            {
                sanitizer = _factory();
            }

            return new DisposableHtmlSanitizer(sanitizer, _pool.Add);
        }
    }
}
