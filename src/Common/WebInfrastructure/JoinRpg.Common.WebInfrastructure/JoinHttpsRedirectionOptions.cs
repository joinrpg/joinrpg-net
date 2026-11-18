namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Настройки редиректа http → https и HSTS (секция <c>HttpsRedirection</c>).
/// </summary>
public class JoinHttpsRedirectionOptions
{
    public const string SectionName = "HttpsRedirection";

    /// <summary>
    /// Рубильник: <c>false</c> отключает и редирект, и HSTS. Нужен, чтобы при цикле редиректов
    /// (прокси не передаёт X-Forwarded-Proto: https) выключить их переменной окружения
    /// <c>HttpsRedirection__Enabled=false</c> и перезапуском подов, без отката образа.
    /// Читается один раз при сборке конвейера.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
