using System.Text.Json;

namespace JoinRpg.TestHelpers;

/// <summary>
/// Модели, которые едут в Blazor-остров, обязаны переживать JSON round-trip: сервер отдаёт их
/// по webapi, а остров десериализует уже в браузере.
/// <para>
/// Это не теоретическая придирка. Сетка ролей полтора года строилась тестами, которые проверяли
/// только результат билдера в памяти, и поэтому пропустили на прод sentinel
/// <c>UserIdentification(-1)</c> в <c>UserLinkViewModel.Hidden</c>: сервер отдавал 200 и валидный
/// JSON, в логах было чисто, а остров молча падал на десериализации (типизированные id не
/// принимают неположительные значения) и навсегда оставался в «Идет загрузка...».
/// </para>
/// <para>
/// Поэтому тесты билдеров островных моделей должны прогонять результат через этот хелпер и
/// проверять утверждения на том, что вернулось с «того конца провода», а не на исходном объекте.
/// </para>
/// </summary>
public static class JsonRoundTrip
{
    /// <summary>
    /// Сериализует модель и десериализует обратно. Возвращает восстановленную модель — именно её
    /// увидит остров.
    /// </summary>
    public static T Ensure<T>(T model) => (T)Ensure(model, typeof(T))!;

    /// <summary>
    /// То же самое, когда тип известен только в рантайме (обход моделей рефлексией).
    /// </summary>
    public static object Ensure(object? model, Type type)
    {
        string json;
        try
        {
            json = JsonSerializer.Serialize(model, type);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Модель {type.Name} не сериализуется в JSON, а она едет в Blazor-остров.", e);
        }

        try
        {
            return JsonSerializer.Deserialize(json, type)
                ?? throw new InvalidOperationException(
                    $"Модель {type.Name} десериализовалась в null. JSON: {json}");
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException(
                $"Модель {type.Name} сериализуется, но не десериализуется обратно — остров "
                + $"упадёт на этом ответе, а сервер при этом отдаст 200 и ничего не напишет в лог. "
                + $"JSON: {json}",
                e);
        }
    }
}
