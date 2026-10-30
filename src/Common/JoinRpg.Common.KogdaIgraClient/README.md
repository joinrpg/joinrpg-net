# JoinRpg.Common.KogdaIgraClient

HTTP-клиент к API [kogda-igra.ru](https://kogda-igra.ru) — календаря ролевых игр.

```
dotnet add package JoinRpg.Common.KogdaIgraClient
```

Нужен net10.0, всё лежит в `JoinRpg.Common.KogdaIgraClient`.

```csharp
services.AddKogdaIgraClient();
services.Configure<KogdaIgraOptions>(configuration.GetSection("KogdaIgra"));
```

```json
"KogdaIgra": {
  "HostName": "https://kogda-igra.ru"
}
```

`HostName` обязателен: без него любой вызов падает с `KogdaIgraConnectException`. Для разработки есть `https://dev.kogda-igra.ru`.

## `IKogdaIgraApiClient`

- `GetGameInfo(int gameId)` — карточка игры (`KogdaIgraGameInfo`: название, даты, регион,
  мастерская группа, ссылки на сайт и соцсети). `null` — игры нет, у неё нет продолжительности
  или ссылка на сайт не http(s). Удалённая на КогдаИгре игра приходит с данными
  и `IsDeleted = true`.
- `GetChangedGamesSince(DateTimeOffset since)` — Id и время правки игр, изменённых после `since`;
  переадресации и записи без даты пропускаются. Перевод в московское время, которого ждёт API,
  клиент делает сам.

## Ошибки

- `KogdaIgraConnectException` — сеть, таймаут или код ответа не 2xx.
- `KogdaIgraParseException` — ответ не разобрался или вместо карточки пришла чужая запись;
  в `Id` — запрошенная игра (0 для списка изменений).
- `KogdaIgraEmptyResponseException` — пустой ответ вместо JSON.
