# JoinRpg.Common.PrimitiveTypes

Общие типы для сайтов вокруг joinrpg.ru: идентификатор пользователя, email, имя, контакты,
ссылки на соцсети, диапазон дат. Нужен, если сайт логинит пользователей через id.joinrpg.ru или
обменивается данными с joinrpg.ru.

```
dotnet add package JoinRpg.Common.PrimitiveTypes
```

Нужен net10.0. Всё лежит в `JoinRpg.Common.PrimitiveTypes`, кроме `UserInfoHeader` — он в
`JoinRpg.Common.PrimitiveTypes.Users`.

## Пользователь

```csharp
var userId = UserIdentification.Parse("UserId(42)", null);  // «42» тоже разберётся
int raw = userId;                                           // неявно
UserIdentification? maybe = UserIdentification.FromOptional(nullableInt);  // null → null
```

- `UserIdentification` — id пользователя joinrpg.ru.
- `UserInfoHeader` — id, отображаемое имя (`UserDisplayName`) и аватарка: то, что нужно, чтобы
  показать пользователя в списке.
- `UserFullName` из `PrefferedName`, `BornName`, `SurName`, `FatherName`.
- `UserContacts`: `Email`, `VkSocialLink`, `TelegramSocialLink`, `LiveJournalId`.
- `AvatarIdentification`, `AvatarInfo` — аватарка по id и готовый URL с размером.

## Типизированные строки

`Email`, `PhoneNumber`, `PrefferedName`, `MarkdownString`, `VkId` и другие с атрибутом
`[TypedStringValue]` проверяют значение при создании из строки — неявным приведением,
`FromOptional` или `Parse`: обрезают пробелы и проверяют длину (`Email.MinLength`,
`Email.MaxLength`), у `Email` — ещё наличие `@`. Неверная строка даёт `ArgumentException`.
`new Email(...)` значение не проверяет.

```csharp
Email email = "user@example.com";                    // неявно из string, с проверкой
Email? optional = Email.FromOptional(input);         // пустая строка или пробелы → null
string s = email;                                    // неявно в string
```

`ToString()` даёт `Email(user@example.com)`; для вывода — `.Value` или приведение к `string`.
`MarkdownString` не обрезается и не ограничен по длине.

## Прочее

- `DateRange` — диапазон дат игры; `DateRangeFormatter.FormatDisplay(range, culture)` выводит
  его («1–5 марта 2026» для `ru-RU`; без `culture` берётся текущая).
- `CountHelper.DisplayCount(5, "заявка", "заявки", "заявок")` — число со склонением.
- `TelegramChatId` — chat_id Bot API (бывает отрицательным); `TelegramHtmlString` — текст
  сообщения с HTML-разметкой Telegram; `KogdaIgraIdentification` — id игры на kogda-igra.ru.
- `SendingResult` — исход отправки уведомления: успех, повторяемая ошибка, общая ошибка сервиса
  (нужна пауза), постоянная ошибка адресата.
- `IUriLocator<T>` — интерфейс «дай ссылку на объект».

## Сериализация, конфигурация, EF Core

Типизированные Id сериализуются в JSON строкой вида `"UserId(42)"` (`TypedEntityIdJsonConverter<T>`)
и читаются из конфигурации (`TypedEntityIdTypeConverter<T>`) — атрибуты уже стоят на типах.
Типизированные строки своего конвертера не имеют: в JSON это объект `{"Value": "..."}`, при
чтении значение не проверяется. Для колонок EF Core — пакет `JoinRpg.Common.EntityFrameworkCore`.

## Свои типы

Атрибуты `[TypedEntityId]` и `[TypedStringValue]` лежат здесь, но код по ним генерирует
`JoinRpg.Common.PrimitiveTypes.SourceGenerator`. Этот пакет его не запускает — если нужны свои
типы, подключите генератор отдельно, см. [его README](https://github.com/joinrpg/joinrpg-net/blob/master/src/Common/JoinRpg.Common.PrimitiveTypes.SourceGenerator/README.md).
