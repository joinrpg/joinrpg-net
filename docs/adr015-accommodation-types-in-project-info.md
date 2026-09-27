# ADR015: типы проживания — настройка проекта внутри ProjectInfo

Проблема:
==

Настройки поселения размазаны и текут EF-сущностями наружу:

- флаг `EnableAccommodation` живёт в `ProjectInfo.AccomodationEnabled` (метаданные проекта);
- типы проживания (`ProjectAccommodationType` — название, стоимость, вместимость,
  `IsPlayerSelectable`, описание) живут только в EF и достаются отдельным запросом через
  `IAccommodationRepository.GetAccommodationForProject`;
- `IAccommodationService.SaveRoomTypeAsync(ProjectAccommodationType)` принимает и возвращает EF-сущность,
  то есть контракт сервисного слоя выражен в терминах таблицы
  (`src/JoinRpg.Services.Interfaces/IAccommodationService.cs`);
- проверка прав на изменение типа проживания есть только в атрибуте контроллера
  (`[MasterAuthorize(Permission.CanManageAccommodation)]`), сам `SaveRoomTypeAsync`/`RemoveRoomType`
  не проверяет ничего — любой новый вызывающий (джоба, MCP, другой сервис) обойдёт проверку молча;
- `AccommodationExtensions` (`src/JoinRpg.Domain/AccommodationExtensions.cs`) — ровно тот legacy-паттерн
  «методы расширения поверх EF-сущностей вместо доменных объектов», от которого уходит
  [structure.md](structure.md).

Следствия на практике: стоимость проживания нужна в расчёте баланса заявки
(`FinanceExtensions.ClaimAccommodationFee`), в карточке заявки, в диалоге выбора типа у игрока,
при клонировании проекта — везде, где `ProjectInfo` уже на руках и бесплатен
(кеш на запрос), но за типами всё равно идёт отдельный поход в БД.

Решение:
==

**Типы проживания — это настройка проекта, и их место в `ProjectInfo`.**
Комнаты и заселение — не настройка, и в `ProjectInfo` им не место.

Граница, которую этот ADR фиксирует как правило:

| Сущность | Природа | Где живёт |
|---|---|---|
| `EnableAccommodation` | настройка мастера | `ProjectInfo` (уже там) |
| `ProjectAccommodationType` | настройка мастера, единицы-десятки на проект, меняется редко | **`ProjectInfo` (этот ADR)** |
| `ProjectAccommodation` (комнаты) | оперативные данные, объём растёт с заявками | вне `ProjectInfo` |
| `AccommodationRequest`, `AccommodationInvite` | зависят от заявок | вне `ProjectInfo` |

Это прямое применение критерия из [project-entities.md](project-entities.md): «в `ProjectInfo` —
все настройки проекта, которые настраивает или меняет мастер; не класть персонажей, заявки и
зависимые от них сущности».

Что даёт перенос:

- **Бесплатно в точке использования.** `IProjectMetadataRepository` кеширован на запрос, и типы
  становятся доступны там, где `ProjectInfo` уже загружен, без второго запроса.
- **Готовый канал мутации.** `IProjectPropsService` ([ADR009](adr009-project-props-service.md)) уже
  даёт права, `ProjectActiveRequirement`, структурное логирование операции, `SaveChanges`,
  `handle.Refresh()` и `PrimeCache`. Отдельному корню всю эту машинерию пришлось бы продублировать —
  и, судя по сегодняшнему `SaveRoomTypeAsync` без проверки прав, продублировать хуже.
- **Права перестают быть свойством контроллера.** `Permission.CanManageAccommodation` проверяется
  внутри операции, а не только в атрибуте.
- **Типизированные идентификаторы.** `AccommodationTypeIdentification` уже лежит в
  `JoinRpg.DomainTypes` рядом с остальными метаданными
  (`Characters/Claims/Accommodation/AccommodationIdentifications.cs`) — сейчас он используется
  только на границе с Blazor, а внутри всё на `int roomTypeId`.

### Отвергнутая альтернатива: отдельный корень поселения

Завести `AccommodationPlan` — самостоятельный агрегат со своим загрузчиком (по образцу
`CharacterInfo`, [ADR013](adr013-character-info.md)), и положить туда **и** типы, и комнаты.

За: поселение действительно самостоятельный контекст; свободные места считаются только по паре
«тип + комнаты + жильцы», и держать половину пары в `ProjectInfo` выглядит половинчато; грузилось бы
только для проектов с включённым поселением.

Против (и это перевесило):

- дублирование машинерии ADR009 — права, активность проекта, логирование, инвалидация кеша;
- второй кеш без инвалидации рядом с первым и вопрос согласованности там, где трогают оба
  (клонирование проекта, закрытие проекта);
- потребителям, которым нужна только стоимость типа (расчёт баланса, карточка заявки, селектор),
  пришлось бы делать лишний async-вызов там, где сегодня достаточно `ProjectInfo`.

Отдельный агрегат для **комнат и заселения** при этом остаётся уместным — но это отдельное решение
и отдельный ADR, здесь он вне скоупа.

Подробности
==

### 1. Доменные типы

Файл: новый, `src/JoinRpg.DomainTypes/ProjectMetadata/Accommodation/AccommodationTypeInfo.cs`.

```csharp
public record AccommodationTypeInfo(
    AccommodationTypeIdentification Id,
    string Name,
    MarkdownString Description,
    int Cost,
    int Capacity,
    bool IsPlayerSelectable);
```

Поля `IsInfinite` и `IsAutoFilledAccommodation` в доменный тип **не переносятся**: в EF-сущности они
помечены `//Not implemented yet, do not use` и нигде не читаются. Колонки остаются в БД, миграции нет.

Рядом — `ProjectAccommodationSettings`:

```csharp
public record ProjectAccommodationSettings(
    bool Enabled,
    IReadOnlyCollection<AccommodationTypeInfo> Types)
{
    public AccommodationTypeInfo GetTypeById(AccommodationTypeIdentification id) => ...;

    /// <summary>Типы, доступные игроку для самостоятельного выбора</summary>
    public IReadOnlyCollection<AccommodationTypeInfo> PlayerSelectableTypes => ...;
}
```

В `ProjectInfo` это **один** параметр `ProjectAccommodationSettings accommodationSettings`, а не два.
Конструктор `ProjectInfo` уже на 19 позиционных параметрах и продублирован в четырёх `With*`-методах;
плодить там ещё одно поле — значит множить эту боль. Существующее свойство `AccomodationEnabled`
(с исторической опечаткой в имени) остаётся как `[Obsolete]`-прокси на `AccommodationSettings.Enabled`,
чтобы не переписывать разом ~десяток вызывающих — burndown по счётчику предупреждений, как принято
при миграциях в этом репозитории.

### 2. Загрузка

- В `Project` добавляется навигация `public virtual ICollection<ProjectAccommodationType> ProjectAccommodationTypes`.
  Это **только навигация**: FK `ProjectAccommodationTypes.ProjectId` уже существует, новых колонок нет,
  миграции нет, `db-schema.md` не меняется.
- В `ProjectLoaderCommon.GetProjectWithFieldsAsync` добавляется `.Include(p => p.ProjectAccommodationTypes)`.
- В `ProjectMetadataRepository.CreateInfoFromProject` — маппинг в `ProjectAccommodationSettings`.

Про стоимость `Include` в горячем пути. Типов проживания на проект единицы-десятки, строки узкие
(`Name`, `Description`, три int, два bool), связанных коллекций не тянем — это несопоставимо с
разбором из [ADR011](adr011-roles-grid-payload.md), где проблему создавали `Include` по персонажам и
пользователям. Отдельная ветка «грузить типы только если `EnableAccommodation`» **не делается**:
условный `Include` разваливает единственный запрос на два, а выигрыш — доли миллисекунды.
Замер до/после на проекте с включённым поселением — часть PR 1 (см. план); если замер покажет иное,
решение пересматривается.

### 3. Изменение настроек

`SaveRoomTypeAsync` / `RemoveRoomType` уезжают из `IAccommodationService` в новые операции поверх
`IProjectPropsService`:

```csharp
Task<AccommodationTypeIdentification> CreateAccommodationType(ProjectIdentification projectId, AccommodationTypeCreateRequest request);
Task UpdateAccommodationType(AccommodationTypeIdentification id, AccommodationTypeUpdateRequest request);
Task DeleteAccommodationType(AccommodationTypeIdentification id);
```

Все три — `Permission.CanManageAccommodation`, `ProjectActiveRequirement.MustBeActive`. Проверка
«тип занят, удалять нельзя» (`RoomIsOccupiedException`) остаётся доменной и выполняется внутри
мутации над EF-сущностью, как сейчас.

Остальные методы `IAccommodationService` (комнаты, `OccupyRoom`/`UnOccupy*`) в этом ADR **не трогаем**.

### 4. Потребители

Мигрируются отдельными PR:

| Потребитель | Сейчас | Станет |
|---|---|---|
| `AccommodationTypeViewService` | `accommodationRepository.GetAccommodationForProject` + фильтр по `IsPlayerSelectable` | `projectInfo.AccommodationSettings` |
| `AccommodationTypeController` | `accommodationService.GetRoomTypeAsync/SaveRoomTypeAsync/RemoveRoomType` | `ProjectInfo` + новые операции props-сервиса |
| `ClaimListController` (`GetRoomTypeById(...).Name` ради заголовка) | поход в БД за одним именем | `projectInfo.AccommodationSettings.GetTypeById` |
| `CreateProjectService.Setup` (создание типа по умолчанию) | `SaveRoomTypeAsync(new ProjectAccommodationType {...})` | `CreateAccommodationType` |
| `CloneProjectHelper` | копирование EF-сущностей | по возможности через доменную операцию |

`IAccommodationRepository.GetAccommodationForProject` и `GetRoomTypeById` после миграции всех
вызывающих удаляются. `GetRoomTypesForProject` (строки отчёта со счётчиками занятости) остаётся:
это оперативные данные, а не настройки.

`AccommodationExtensions` в этом ADR не трогаем — он про свободные места в комнатах, то есть
про часть, вынесенную из скоупа.

### 5. План миграции

1. **PR 1** — ✅ сделано. `AccommodationTypeInfo`, `ProjectAccommodationSettings`, навигация,
   `Include`, маппинг, поле в `ProjectInfo`, `[Obsolete]`-прокси `AccomodationEnabled`, юнит-тесты
   на маппинг поверх `MockedProject`. Потребители не трогались.
2. **PR 2** — операции создания/изменения/удаления типа на `IProjectPropsService`, перевод
   `AccommodationTypeController` и `CreateProjectService.Setup`, удаление `SaveRoomTypeAsync`/
   `RemoveRoomType` из `IAccommodationService`.
3. **PR 3** — перевод читающих потребителей (`AccommodationTypeViewService`, `ClaimListController`,
   клонирование), удаление `GetAccommodationForProject`/`GetRoomTypeById`.
4. **PR 4** — вычистить `[Obsolete] AccomodationEnabled`.

Последствия
==

- `ProjectInfo` растёт ещё на одну настройку — это осознанная плата за единый канал изменения
  метаданных и отсутствие второго кеша.
- Изменение типа проживания становится обычной операцией метаданных: права, логирование и
  согласованность `Project` ↔ `ProjectInfo` получаются из ADR009 даром.
- Сервисный слой поселения перестаёт торговать EF-сущностями; `IAccommodationService` сжимается до
  операций с комнатами и заселением — и становится понятно, что именно достаётся будущему
  отдельному агрегату поселения.
- Правило «тип проживания — настройка, комната и заселение — оперативные данные» зафиксировано
  и годится как ответ на следующий такой же вопрос.

Открытые вопросы
==

- Цифры по проду (сколько типов и комнат на проект, у скольких проектов включено поселение) в этот
  ADR не попали: запрос к прод-БД в сессии написания был заблокирован. Оценка «типов единицы-десятки»
  сделана по смыслу сущности. **Замер горячего пути в PR 1 тоже не сделан** — он требует боевого
  проекта с включённым поселением, из рабочего окружения такого нет. Косвенно: интеграционные тесты
  после добавления `Include` не замедлились. Если на проде обнаружится проект с сотнями типов
  проживания, решение пересматривается.
- Навигационное свойство в `Project` — изменение `JoinRpg.DataModel` без новых колонок; по правилу
  CLAUDE.md изменения в `DataModel` согласуются с @leotsarev.
