# ADR017. Тип поля «ссылка на пользователя»

## Статус

Принято, реализуется. Issues: [#4509](https://github.com/joinrpg/joinrpg-net/issues/4509)
(одиночная ссылка), [#4511](https://github.com/joinrpg/joinrpg-net/issues/4511) (мультивыбор).
Предпосылка [#4510](https://github.com/joinrpg/joinrpg-net/issues/4510) (режим `Multiple`
у `JoinUserLinkEditor`) уже закрыта.

## Статус реализации

- ✅ **§1 Члены перечислений** — `ProjectFieldType.UserLink` и `ProjectFieldViewType.UserLink`,
  все `switch` в `ProjectFieldTypeHelper` закрыты, плюс `IsUserLink()` как единственное место,
  которое знает состав user-типов. `MultiUserLink` появится вместе с #4511.
- ✅ **§2 Хранение** — `FieldWithValue.UserIds`: читает терпимо (мусор в базе не роняет показ),
  пишет строго — `FieldUserValueInvalidException` на нечисло, неположительный id и на второй
  id у одиночного типа. `DisplayString` остался сырым, как и решено.
- ✅ **§3 Права на заполнение** — отдельного запрета нет, работает обычная галочка
  «игрок может менять» (тест `AddField_UserLink_RespectsCanPlayerEdit`). Прежний запрет для
  игроков снят на ревью #5023.
- ⚠️ **§4 Отображение** — частично. Сделан `FieldUserLinksLoader`: один
  `GetUserInfoHeaders` на экран, словарь идёт в `CustomFieldsViewModel` → `FieldValueViewModel.UserLinks`,
  `DisplayTemplates/FieldValueViewModel.cshtml` рисует компонент `UserLink`. Покрыты страница
  персонажа, заявка, подача заявки и печать. **Экспорт (`CustomExporter`) и x-api по-прежнему
  отдают сырые id** — они ходят мимо `FieldValueViewModel`, напрямую в `DisplayString`.
  Удалённый пользователь показывается как «пользователь удалён» (новый `ViewMode.Deleted`).
- ❌ **§5 Редактор** — не сделано. Пока обычный `<input type="text">` с id; остров
  `JoinUserLinkEditor` с резолвом ссылки — следующим PR.
- ❌ **§7 Валидация существования пользователя** — не сделано: сейчас проверяется только формат.
  Идёт вместе с редактором.
- ✅ **§9 Тесты** — `UserLinkFieldTest` (домен), `CustomFieldsViewModelTest` (резолв, удалённый
  пользователь, мусор), `FieldSetupServiceTest`. Тест инварианта приватности из §9 — вместе с §7.

## Контекст

Мастера хотят ссылаться из полей персонажа/заявки на пользователя сайта: куратор роли,
ответственный за группу, второй игрок в паре, владелец транспорта. Сейчас это делают строкой
(«Вася Пупкин», ссылка руками) — из такой строки нельзя ни перейти в профиль, ни понять, о каком
из трёх Вась речь.

Технически задача выглядит как «ещё один член `ProjectFieldType` + ветка в шаблонах», но у неё
два сквозных последствия, из-за которых и написан ADR:

1. **Значение поля — id, а показывать надо имя.** `FieldWithValue.DisplayString`
   ([FieldWithValue.cs:31](../src/JoinRpg.DomainTypes/Characters/FieldWithValue.cs)) синхронный,
   живёт в `JoinRpg.DomainTypes` и не имеет доступа к репозиториям. Потребителей у него восемь:
   экспорт ([CustomExporter.cs:194](../src/JoinRpg.WebPortal.Models/Exporters/CustomExporter.cs)),
   x-api ([ApiInfoBuilder.cs:77](../src/JoinRpg.WebPortal.Managers/Characters/ApiInfoBuilder.cs)),
   сетка ролей
   ([ProjectRoleGridViewModelBuilder.cs:197](../src/JoinRpg.WebPortal.Managers/CharacterGroups/ProjectRoleGridViewModelBuilder.cs)),
   список заявок ([ClaimListBuilder.cs:81](../src/JoinRpg.WebPortal.Models/ClaimList/ClaimListBuilder.cs)),
   просмотр персонажа/заявки, печать, MCP-сервер, модели писем. Без единого решения половина
   каналов покажет `123`, вторая — имя, и никто не разберёт, так задумано или это баг.
2. **Ссылка на человека — это персональные данные.** Надо явно зафиксировать, что ссылка даёт,
   а чего не даёт, иначе следующая правка это тихо расширит.

## Решение

### 1. Два новых члена перечисления, без новых полей в БД

```csharp
public enum ProjectFieldType
{
    // ...существующие, порядок не трогать...
    Uri,
    UserLink,       // #4509
    MultiUserLink,  // #4511
}
```

Как `Dropdown`/`MultiSelect` — два отдельных типа, а не один тип с флагом «мультивыбор».
Флаг потребовал бы новой колонки в `ProjectFields`, а новые поля `DataModel` согласовываются
с @leotsarev (см. [CLAUDE.md](../CLAUDE.md)); выигрыша по сравнению с двумя членами нет.

Члены **дописываются в конец обоих перечислений** — `ProjectFieldType` и
[`ProjectFieldViewType`](../src/JoinRpg.Web.ProjectCommon/Fields/FieldViewTypeEnums.cs): в БД
лежит `int`, а `ProjectFieldViewTypeHelper` кастует одно в другое напрямую
(`(ProjectFieldType)self`). Соответствие проверяет `EnumTests`.

В `ProjectFieldTypeHelper`: `HasValuesList` = `false`, `SupportsPricing` = `false`,
`SupportsPricingOnField` = `false`, `SupportsMassAdding` = `false` (падает из `HasValuesList`).
Все `switch` там кидают `ArgumentException` в `default` — компилятор новый член не подсветит,
ловят только `ProjectFieldTypeTests`. `SupportsMarkdown` остаётся `false` (значение не должно
проходить через markdown-рендерер).

### 2. Хранение — id через запятую

`UserLink` хранит `"123"`, `MultiUserLink` — `"123,456"`. Формат тот же, что у `MultiSelect`,
но парсинг другой: `FieldWithValue.SelectedIds` заполняется только при `Field.HasValueList`, а
у нового типа списка вариантов нет. Добавляется отдельное свойство:

```csharp
public IReadOnlyList<UserIdentification> UserIds =>
    Field.Type is ProjectFieldType.UserLink or ProjectFieldType.MultiUserLink
        ? [.. (Value?.ParseToIntList() ?? []).Select(x => new UserIdentification(x))]
        : [];
```

`DisplayString` для этих типов возвращает **сырые id** и напрямую для показа не используется
(см. §4). Это осознанно: сделать его «умным» нельзя, не протащив репозиторий в `DomainTypes`.

### 3. Редактирование — как у любого другого поля

Отдельного запрета нет: заполнять поле может тот, кому мастер проекта это разрешил галочкой
«игрок может менять», как у всех прочих типов.

> Первая редакция ADR запрещала `CanPlayerEdit` для user-полей: поле называет **третье лицо**,
> которое об этом не узнает и убрать себя не сможет, поэтому ответственность предлагалось
> оставить на мастере. Решение отменено на ревью (#5023) — ограничение сочли лишним.
> Запрет был выражен в трёх местах (`ProjectField.Validate`, `GameFieldCreateViewModel`,
> `FieldSetupServiceImpl`); все три сняты.

Следствие для §6: оракул перебора (резолв email/telegram → пользователь) больше не ограничен
мастерами самим типом поля. Эндпоинт резолва обязан быть доступен тому, кто поле заполняет, —
значит участнику проекта, а не только мастеру.

### 4. Отображение: резолв пачкой, общий форматтер

Отдельной абстракции для этого не заводим: резолвить нечего, это буквально
`IUserRepository.GetUserInfoHeaders(ids)`. Вся собственная логика — собрать идентификаторы по
полям экрана, а это чистая функция над `FieldWithValue.UserIds`, которой не нужен ни DI, ни мок
в тестах. Поэтому — extension-методы над репозиторием:

```csharp
// JoinRpg.WebPortal.Models/FieldUserLinksLoader.cs
public static Task<IReadOnlyDictionary<UserIdentification, UserInfoHeader>> LoadFieldUserLinks(
    this IUserRepository userRepository,
    IEnumerable<FieldWithValue> fields,
    Dictionary<int, string?>? overrideValues = null);
```

Несуществующие id в словарь не попадают — показ разбирается с этим сам (см. ниже).

> Первая редакция ADR предлагала здесь интерфейс `IUserFieldResolver` в
> `JoinRpg.Services.Interfaces` — чтобы им могли пользоваться и менеджеры, и
> `JoinRpg.Services.Email`. Письма каналом не оказались (рендер значений полей там
> закомментирован), а остальным потребителям хватило extension-метода: интерфейс пришлось бы
> регистрировать и подменять в тестах ради одного вызова.

Правило: **резолв всегда пачкой на весь экран/выгрузку**, не по одному значению. Поле с десятью
ссылками в списке заявок иначе даёт N+1; интеграционные тесты ленивых загрузок
([lazy-loads-baseline.md](lazy-loads-baseline.md)) это поймают, но лучше не доводить.

Каналы:

| Канал | Что показывает |
|---|---|
| Просмотр персонажа/заявки, сетка ролей | компонент `UserLink` поверх `UserLinkViewModel` |
| Редактор поля | `JoinUserLinkEditor` (§5) |
| Экспорт, печать | `DisplayName` текстом, через запятую |
| x-api (`ApiInfoBuilder.ToFieldValue`) | `Value` — сырые id (контракт), `DisplayString` — имена |
| MCP-сервер | как экспорт — имена |
| Письма | рендер значений полей сейчас закомментирован (`EmailServiceImpl.cs:164`); учесть при его возврате |

Показ только через `UserLinkViewModel` — не случайность. Рядом лежит
`UserProfileDetailsViewModel(user, currentProject, currentUserAccessor)`, который отдаёт email,
телефон и соцсети; если взять его «чтобы красиво», контракт §6 сломается молча.

Пользователь, которого удалили или который не нашёлся, рисуется как «пользователь удалён» и
ничего не роняет.

### 5. Редактор — Blazor-остров внутри MVC-формы

Форма редактирования полей — легаси-MVC (`Views/Shared/EditorTemplates/FieldValueViewModel.cshtml`),
значения читаются из `Request.Form` по префиксу `field_`
([FormCollectionHelpers.cs:22](../src/JoinRpg.Portal/Helpers/FormCollectionHelpers.cs)). Значит
остров обязан отрендерить **именованный инпут**, который уедет вместе с формой.

Образец уже есть: `CharacterGroupSelector` → `IntSelector` с параметром `Name`
(`Views/Character/Edit.cshtml:56`). `JoinUserLinkEditor` получает такой же параметр `Name` и
скрытый инпут с id через запятую; сам контрол и его режим `Multiple` менять не нужно — они
сделаны в #4510.

### 6. Приватность: что ссылка даёт и чего не даёт

**Не даёт доступа к контактам.** Контакты выдаются по
[`UserInfo.GetAccess`](../src/JoinRpg.DomainTypes/Users/UserInfo.cs): свой профиль, мастер проекта,
где у пользователя есть активная заявка, со-мастер, админ. Упоминание в поле в этот расчёт не
входит и входить не должно — ни сейчас, ни потом. Это инвариант, а не побочное свойство текущего
кода, и он закрывается тестом (§7).

**Даёт имя и ссылку на профиль — ровно то, что и так видно.** Профиль `/user/{id}` открыт
анонимам ([UserController.cs:19](../src/JoinRpg.Portal/Controllers/UserProfile/UserController.cs))
и показывает `DisplayName`, аватар и публичные проекты; контакты там закрыты тем же `GetAccess`.

**Публичное поле разрешено.** Формально `ProjectFieldVisibility.Public` у user-поля обходит
настройку персонажа «скрыть игрока» (`HidePlayerForCharacter` → `ViewMode.Hide`, см.
[PublicCharacterJsonBuilder.cs:25](../src/JoinRpg.Portal/Controllers/PublicCharacterJsonBuilder.cs)
и issue #4895): мастер, скрывший игрока на странице роли, может вписать того же человека в
публичное поле. Запрещать не будем — мастер выставляет содержимое своего проекта осознанно, а
запрет на `Public` только заставил бы обходить его обычным строковым полем.

**Оракул перебора — риск принят.** `UserLinkResolver` резолвит email/vk/telegram → userId и
отвечает «пользователь не найден», то есть подтверждает факт регистрации по чужому контакту.
Ограничить это правами не выходит: поле заполняет и игрок (§3), значит резолв доступен любому
участнику проекта, то есть фактически любому зарегистрированному — проект заводится в два клика.
Риск принят осознанно: существенная часть этих данных и так доступна через поиск по сайту.
Отдельных мер (rate limit, единый текст ошибки) в рамках этой задачи не вводим.

### 7. Валидация при сохранении

Значение приходит сырой строкой из формы, а также может прийти по API — проверять надо на
сервере. `FieldSaveHelper` синхронный и живёт в `JoinRpg.Domain` без доступа к репозиториям,
поэтому резолв и проверка существования пользователя делаются **до** него, в
`CharacterPropsService` ([ADR014](adr014-claim-props-service.md)) — он async и видит
`IUserRepository`. Там же: дедупликация id и лимит на число значений в `MultiUserLink`.

### 8. Клонирование проекта

`CloneProjectHelper.MapFieldValue` для полей без списка вариантов копирует значение как есть —
ссылки на пользователей переедут в клон. Это корректно: пользователь — сущность сайта, а не
проекта. Специально ничего не делаем, просто фиксируем, что поведение осознанное.

### 9. Тесты

- `ProjectFieldTypeTests` — новые члены во всех `switch` `ProjectFieldTypeHelper`.
- `EnumTests` — соответствие `ProjectFieldType` ↔ `ProjectFieldViewType`.
- Валидация `ProjectField`: `CanPlayerEdit = true` у user-поля — ошибка (§3).
- **Тест инварианта приватности:** пользователь, упомянутый в поле проекта, но не имеющий там
  заявки и мастерского доступа, не получает `UserProfileAccessReason` выше `NoAccess`, и в
  вью-модель поля не попадают email/телефон/соцсети.
- Резолв: несуществующий id → «пользователь удалён», страница не падает.
- Сохранение: мусор в `field_N` (не число, отрицательное, чужой формат) не доезжает до БД (§7).

## Что сознательно не делаем

- **Не уведомляем** пользователя о том, что на него сослались, и не показываем эти ссылки в его
  профиле — хотя заполнять поле может и игрок (§3), то есть назвать человека может кто угодно.
- **Не заводим спецгруппы** по значению поля (как у `Dropdown`): группа на пользователя не
  натягивается.
- **Не даём цен** на user-полях.
- **Не трогаем оракул перебора** в `UserLinkResolver` (§6).
