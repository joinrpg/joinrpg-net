using JoinRpg.IdPortal.OAuthServer.Cimd;

namespace JoinRpg.IdPortal.Test.Cimd;

/// <summary>
/// Ограждение вокруг таймаутов загрузчика. Тест намеренно проверяет числа, а не поведение:
/// воспроизвести медленное TLS-рукопожатие в тесте нечем, а последствия ошибки в этих числах
/// незаметны — документ просто не скачивается, и клиент получает invalid_client без намёка на
/// причину. Ровно так CIMD и не работал на dev: ConnectTimeout стоял 3 с, а рукопожатие у
/// claude.ai заняло ~4,6 с (TCP при этом ~0,08 с).
/// </summary>
public class CimdMetadataLoaderTimeoutTests
{
    /// <summary>Измеренное время рукопожатия, вокруг которого выбраны таймауты.</summary>
    private static readonly TimeSpan ObservedTlsHandshake = TimeSpan.FromSeconds(5);

    [Fact]
    public void ConnectTimeout_LeavesRoomForSlowTlsHandshake() =>
        CimdMetadataLoader.ConnectTimeout.ShouldBeGreaterThan(ObservedTlsHandshake);

    [Fact]
    public void RequestTimeout_IsLargerThanConnectTimeout() =>
        // Иначе общий таймаут срабатывает раньше, чем соединение успевает установиться,
        // и увеличение ConnectTimeout не даёт ничего.
        CimdMetadataLoader.RequestTimeout.ShouldBeGreaterThan(CimdMetadataLoader.ConnectTimeout);

    [Fact]
    public void Handler_DoesNotFollowRedirects()
    {
        // Следовать за редиректом — обход проверки адреса: публичный URL увёл бы на 127.0.0.1.
        using var handler = CimdMetadataLoader.CreateHandler();

        var sockets = handler.ShouldBeOfType<SocketsHttpHandler>();
        sockets.AllowAutoRedirect.ShouldBeFalse();
        sockets.ConnectTimeout.ShouldBe(CimdMetadataLoader.ConnectTimeout);
    }
}
