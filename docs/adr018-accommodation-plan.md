# ADR018: AccommodationPlan — доменный агрегат комнат и заселения

Проблема:
==

[ADR015](adr015-accommodation-types-in-project-info.md) разделил поселение надвое: **тип проживания**
— настройка мастера, уехал в `ProjectInfo.AccommodationSettings` и меняется через
`IProjectPropsService`; **комната и заселение** — оперативные данные, в `ProjectInfo` им не место.
Вторая половина осталась за скоупом того ADR, и этот ADR — про неё (issue #5037).

Сегодня в `IAccommodationService` живут `AddRooms`, `EditRoom`, `DeleteRoom`, `GetRoomTypeAsync`,
`OccupyRoom`, `UnOccupyRoom`, `UnOccupyRoomAll`, `UnOccupyRoomType`, `UnOccupyAll`. Все они:

- наследуют `[Obsolete] DbServiceImplBase` и ходят в `UnitOfWork.GetDbSet<T>()` напрямую;
- принимают и отдают EF-сущности (`ProjectAccommodation`, `ProjectAccommodationType`);
- обслуживаются методами-расширениями поверх EF (`src/JoinRpg.Domain/AccommodationExtensions.cs`) —
  ровно тем legacy-паттерном, от которого уходит [structure.md](structure.md);
- принимают `int projectId, int roomTypeId, int roomId` россыпью, хотя типизированные
  идентификаторы для поселения уже есть
  (`src/JoinRpg.DomainTypes/Characters/Claims/Accommodation/AccommodationIdentifications.cs`).

### Дефекты, которые чинятся вместе с переносом

1. **`EditRoom` и `DeleteRoom` не проверяют права вообще.** В сервисе нет ни `RequestMasterAccess`,
   ни чего-либо ещё — защищает только атрибут контроллера
   (`[MasterAuthorize(Permission.CanManageAccommodation)]`). Любой новый вызывающий — джоба, MCP,
   другой сервис — обойдёт проверку молча. У типов проживания была та же дыра, её закрыл #5017.
   `AddRooms` тоже не проверяет ничего.

2. **`projectId` у `EditRoom`/`DeleteRoom` необязательный** (`int? projectId = null`), и приватный
   `GetRoom` фильтрует по проекту, только если его передали. Сегодняшний вызывающий передаёт, но
   контракт разрешает отредактировать или удалить комнату чужого проекта.

3. **`UnOccupyRoomType` и `UnOccupyAll` делают N сохранений.** Каждая комната выселяется отдельным
   `UnOccupyRoomImpl` со своим `SaveChangesAsync`; падение на пятой комнате оставляет четыре
   выселенными и четыре письма отправленными. Проверка прав тоже повторяется на каждой комнате.

4. **Ни один метод не проверяет активность проекта.** Заселять и выселять можно в архивном проекте.

5. **`OccupyRoom` не проверяет, что заявка на проживание того же типа, что комната.** Комната
   грузится по `(ProjectId, RoomId)`, заявки — по `(ProjectId, Id ∈ ids)`, а `roomTypeId`,
   который контроллер передаёт в форме, до сервиса не доходит вовсе. То есть заявку на палатку
   можно поселить в гостиничный номер. Сегодня от этого защищает только UI.

6. **Проверка свободного места опирается на relationship fixup EF.** В цикле по заявкам
   `room.GetRoomFreeSpace()` считает жильцов по `room.Inhabitants`, а заселение делается присвоением
   `accommodationRequest.Accommodation = room`. То, что обратная навигация обновится до следующей
   итерации, — свойство трекера EF6, не выраженное в коде. Считать вместимость нужно явно.

7. **`OccupyRoom`/`UnOccupyRoom` падают NRE, если комнаты или заявки нет** — `FirstOrDefaultAsync`
   без проверки на `null`, дальше сразу `room.Project`. Контроллер превращает это в 500.

Решение:
==

**Комнаты и заселение — отдельный агрегат `AccommodationPlan`**, со своим доменным типом,
загрузчиком и точкой мутации — по образцу `CharacterInfo` ([ADR013](adr013-character-info.md)) и
`CharacterPropsService` ([ADR014](adr014-claim-props-service.md)).

### 1. Корень агрегата — тип проживания, а не проект и не комната

Границу задаёт инвариант. Их три, и ни один не пересекает границу типа проживания:

- в комнате не больше жильцов, чем `Capacity` её типа;
- заявка на проживание селится только в комнату **своего** типа;
- удалить можно только незаселённую комнату.

Комната как корень слишком мелка: проверка вместимости требует всех её жильцов, а массовые операции
(`UnOccupyRoomType`) трогают десятки комнат разом — с корнем-комнатой они так и остались бы циклом с
N сохранениями. Проект как корень слишком крупен: комнат на проекте столько же, сколько заявок,
и грузить их все ради переименования одной — та же болезнь, что разобрана в
[ADR011](adr011-roles-grid-payload.md).

Тип проживания попадает ровно в середину: это **и** естественная граница инвариантов, **и** то, что
уже грузится одним запросом (`GetRoomTypeAsync` с `Include(ProjectAccommodations)` +
`Include(Desirous)`), **и** то, что показывает страница комнат `EditRoomTypeRooms`.

Проектная операция `UnOccupyAll` остаётся операцией **над несколькими агрегатами** — цикл по типам
внутри одной мутации и одного `SaveChanges`. Это сознательное отступление: для редкой
административной операции атомарность важнее чистоты границы, а дефект №3 именно про то, что
сегодня её нет.

### 2. Сам тип проживания в агрегат не копируется

`AccommodationPlan` держит ссылку на `ProjectInfo` и на `AccommodationTypeInfo` из его
`AccommodationSettings` — ровно как `CharacterInfo` держит `ProjectInfo`, а не пересобирает поля
проекта. Это прямое следствие ADR015: единственный источник правды о типе — метаданные проекта,
и второго не заводим. `Capacity` для проверки вместимости берётся оттуда же.

Следствие: инвариант ссылочного равенства из ADR013 действует и здесь —
`ReferenceEquals(Plan.ProjectInfo, Plan.AccommodationType-owner)`, и межзапросного кеша у плана
нет.

### 3. Заявку на проживание делят два агрегата — и делят по колонкам

`AccommodationRequest` (группа соседей, желающих жить вместе) уже принадлежит character-агрегату:
`ClaimServiceImpl.SetAccommodationType` и `LeaveAccommodationGroupAsync` создают и расформировывают
её через `ICharacterPropsService.ChangeClaim` ([ADR014](adr014-claim-props-service.md)), а тип
догружается именованным загрузчиком `ctx.LoadAccommodationType`.

Поэтому владение разделяется **по колонкам одной строки**, и это надо назвать явно:

| Что | Кто владеет | Чем меняется |
|---|---|---|
| `AccommodationTypeId`, состав `Subjects`, создание и удаление строки | character-агрегат | `ICharacterPropsService.ChangeClaim` |
| `AccommodationId` — в какой комнате живёт группа | **`AccommodationPlan`** | `IAccommodationPropsService` (этот ADR) |
| `IsAccepted` (приглашения) | `AccommodationInviteServiceImpl` | не трогаем, см. «Что вне скоупа» |

`AccommodationPlan` видит группы как read-only состав (`Subjects` — список
`ClaimIdentification`) и пишет ровно одно поле — номер комнаты. Обратное тоже верно: character-путь
не должен менять `AccommodationId` иначе как через расформирование группы, и сегодня он этого и не
делает (`ConsiderLeavingRoom` только шлёт письмо).

### 4. Деньги в агрегат не входят

Страница комнат показывает по каждому жильцу «не оплачено X из Y». Складывать финансовые поля в
`AccommodationPlan` — значит завести третью модель расчёта баланса рядом с `Claim` и
`CharacterClaimInfo`. Вместо этого план несёт `ClaimIdentification` жильцов, а вью-сервис страницы
догружает их bulk'ом через существующий `ICharacterInfoRepository.GetCharacterInfos(ids)` и считает
баланс уже принятым способом — `FinanceExtensions.CalculateClaimBalance(CharacterInfo, CharacterClaimInfo, ProjectInfo)`.
Это один дополнительный запрос на страницу, не N+1.

Имена игроков — тем же путём, что в ADR013: `UserInfoHeader` bulk'ом, не `Include` на каждого.

### 5. Отвергнутые альтернативы

**Именованные загрузчики на хэндле `IProjectPropsService`** (путь из PR #4843): комнаты в
`ProjectInfo` не попадут, поэтому их пришлось бы догружать через хэндл, как это делает claim-контур
с `LoadAccommodationType`. Отвергнуто: это работает, когда догружаемое — **деталь** операции над
корнем (проверить существование типа при смене типа у заявки), и разваливается, когда догружаемое
**является** предметом операции. `ChangeProjectPropertiesAsync` для переименования комнаты
пересобирал бы `ProjectInfo` и прогревал кеш метаданных на каждое действие с комнатой — при том,
что метаданные не менялись. Плюс правило ADR009 «метаданные меняются только через props-сервис»
превратилось бы в «а ещё через него меняется всё, что удалось догрузить», и граница, проведённая
ADR015, стёрлась бы обратно.

**Всё поселение одним агрегатом, включая типы** — рассмотрено и отвергнуто в ADR015; типы остаются
в `ProjectInfo`.

### 6. Ответ на возражение ADR015 про дублирование машинерии

ADR015 отверг отдельный корень тем, что пришлось бы дублировать машинерию ADR009: права, активность
проекта, логирование, инвалидацию кеша. Для **типов** это верно. Для комнат — нет:

- **инвалидация кеша не нужна вовсе.** План не меняет метаданные, значит ни `handle.Refresh()`, ни
  `PrimeCache` не требуются. Именно это и был главный пункт возражения;
- **второго кеша не возникает.** У `AccommodationPlan`, как и у `CharacterInfo`, межзапросного кеша
  нет — данные мутируют при каждом действии с заявкой;
- остаются права, активность и логирование — и это уже **третий** случай одного и того же цикла
  (ADR009, ADR014, этот ADR). Значит его пора вынести: общий `ProjectOperationGuard`
  (проверка права с admin-bypass, `ProjectActiveRequirement`, структурный лог операции с её
  аргументами — `Information` при успехе, `Warning` при провале) используется всеми тремя
  props-сервисами. Если при реализации окажется, что общего кода на три строки, guard не заводим и
  копируем — но решать это по факту, а не заранее.

Подробности
==

### 1. Типизированный идентификатор комнаты

У комнаты типизированного id нет — добавляется в существующий файл
`src/JoinRpg.DomainTypes/Characters/Claims/Accommodation/AccommodationIdentifications.cs`, рядом с
остальными идентификаторами поселения (все id поселения держим в одном файле, даже ценой не совсем
подходящего имени папки — разносить их по трём местам хуже):

```csharp
/// <summary>
/// Идентификатор комнаты (места поселения) в проекте.
/// </summary>
[method: JsonConstructor]
[TypedEntityId]
public partial record AccommodationRoomIdentification(
    ProjectIdentification ProjectId,
    int RoomId) : IProjectEntityId;
```

### 2. Доменные типы

Файлы: новые, `src/JoinRpg.DomainTypes/Accommodation/`.

```csharp
/// <summary>
/// План поселения одного типа проживания: его комнаты и группы жильцов.
/// Привязан к конкретному экземпляру <see cref="ProjectInfo"/>, кешированию между запросами
/// не подлежит.
/// </summary>
public record class AccommodationPlan
{
    public AccommodationTypeIdentification Id { get; }
    public ProjectInfo ProjectInfo { get; }
    public AccommodationTypeInfo AccommodationType { get; }

    public IReadOnlyCollection<RoomInfo> Rooms { get; }

    /// <summary>Все группы этого типа, включая ещё не расселённые</summary>
    public IReadOnlyCollection<AccommodationGroupInfo> Groups { get; }

    public IEnumerable<AccommodationGroupInfo> UnassignedGroups => Groups.Where(g => g.RoomId is null);

    public RoomInfo GetRoom(AccommodationRoomIdentification roomId);
    public AccommodationGroupInfo GetGroup(AccommodationRequestIdentification groupId);
}

/// <summary>Комната</summary>
public record class RoomInfo(
    AccommodationRoomIdentification Id,
    string Name,
    int Capacity,
    IReadOnlyCollection<AccommodationGroupInfo> Inhabitants)
{
    public int Occupancy => Inhabitants.Sum(i => i.Persons);
    public int FreeSpace => Capacity - Occupancy;
    public bool IsOccupied => Inhabitants.Count > 0;
}

/// <summary>Группа, живущая (или желающая жить) вместе — одна <c>AccommodationRequest</c></summary>
public record class AccommodationGroupInfo(
    AccommodationRequestIdentification Id,
    AccommodationRoomIdentification? RoomId,
    IReadOnlyCollection<ClaimIdentification> Subjects)
{
    public int Persons => Subjects.Count;
}
```

Инварианты — в конструкторе `AccommodationPlan`, как это делают `CharacterTypeInfo` и `CharacterInfo`:

- `Id.ProjectId == ProjectInfo.ProjectId`, и `AccommodationType` — тот самый экземпляр, что лежит в
  `ProjectInfo.AccommodationSettings` (`ReferenceEquals`);
- у всех комнат и групп `ProjectId` совпадает с `Id.ProjectId`;
- `RoomInfo.Inhabitants ⊆ Groups`, и группа с непустым `RoomId` присутствует ровно в одной комнате;
- `Capacity` у всех комнат один и тот же — `AccommodationType.Capacity` (сегодня вместимость
  хранится у типа, а не у комнаты; хранить её в `RoomInfo` — задел на будущие «нестандартные»
  комнаты, но правды сверх типа в ней сейчас нет).

Превышение вместимости в конструкторе **не проверяется**: сегодняшние данные могли переполниться
(вместимость типа уменьшили после заселения), и загрузка плана не должна падать на таком проекте.
Это проверка операции, а не инварианта загрузки.

`GetRoomFreeSpace(ProjectAccommodation)` и `IsOccupied(ProjectAccommodation)` из
`src/JoinRpg.Domain/AccommodationExtensions.cs` становятся свойствами `RoomInfo` и после миграции
удаляются. Остальные методы этого файла (`GetClaimNeighbours`, `GetRoomFreeSpace(AccommodationRequest)`)
обслуживают contour приглашений и карточку заявки — они вне скоупа и остаются.

### 3. Загрузка

Файл: новый, `src/JoinRpg.Data.Interfaces/Accommodation/IAccommodationPlanRepository.cs`.

```csharp
public interface IAccommodationPlanRepository
{
    Task<AccommodationPlan?> GetPlanOrDefault(AccommodationTypeIdentification typeId);
    Task<IReadOnlyCollection<AccommodationPlan>> GetAllPlans(ProjectIdentification projectId);
}
```

Реализация — `src/JoinRpg.Dal.Impl/Repositories/Accommodation/AccommodationPlanLoader.cs`, по
образцу `CharacterInfoLoader` (ADR014): ядро принимает готовый `ProjectInfo`, чтобы им могли
пользоваться и кеширующий репозиторий, и write-хэндл, и инвариант ссылочного равенства не ломался.
Берёт `MyDbContext` напрямую, **не** наследует `GameRepositoryImplBase` (прогрев контекста всем
проектом — причина TTFB из ADR011). Один EF6-запрос с проекцией в приватные row-типы, группы —
вложенным `Select`, не `Include`. Капканы EF6 те же, что перечислены в ADR013 §5.

### 4. Мутации

Внутренний props-сервис (`JoinRpg.Services.Impl/Accommodation/`), один метод:

```csharp
internal interface IAccommodationPropsService
{
    Task<TResult> ChangeAccommodationPlan<TArgs, TResult>(
        AccommodationTypeIdentification typeId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<AccommodationMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "");
}
```

Плюс перегрузка без результата и перегрузка по `ProjectIdentification` (для `UnOccupyAllRooms`,
которая работает по всем типам проекта в одной транзакции).

`AccommodationMutationContext` отдаёт: трекаемые EF-сущности комнат и заявок на проживание,
доменный снимок `AccommodationPlan` **до** мутации, делегаты `AddEntity`/`RemoveEntity` (не `DbSet`
наружу — как в ADR014, это делает контекст подделываемым в юнит-тестах) и `AddLegacyEmail` для
писем о заселении.

Write-хэндл `IAccommodationPlanWriteRepository` берётся **только** из
`IUnitOfWork.GetAccommodationPlanWriteRepository()`, а не из DI: `MyDbContext` зарегистрирован
транзиентом, и DI-экземпляр дал бы другой контекст — мутация трекалась бы в одном, а `SaveChanges`
шёл в другом (ADR009 §1, ADR014 §2).

Публичный контракт `IAccommodationService` переписывается на типизированные идентификаторы:

```csharp
public interface IAccommodationService
{
    Task<IReadOnlyCollection<AccommodationRoomIdentification>> AddRooms(
        AccommodationTypeIdentification typeId, string rooms);

    Task RenameRoom(AccommodationRoomIdentification roomId, string name);
    Task DeleteRoom(AccommodationRoomIdentification roomId);

    Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds);

    Task UnOccupyGroup(AccommodationRequestIdentification groupId);
    Task UnOccupyRoom(AccommodationRoomIdentification roomId);
    Task UnOccupyRoomType(AccommodationTypeIdentification typeId);
    Task UnOccupyAllRooms(ProjectIdentification projectId);
}
```

`GetRoomTypeAsync` уходит: его единственный вызывающий — страница `EditRoomTypeRooms`, которая
переезжает на `IAccommodationPlanRepository`. Классы-запросы `OccupyRequest`, `UnOccupyRequest`,
`UnOccupyAllRequest`, `UnOccupyRoomTypeRequest` (mutable, с `int`-полями, один из них вообще без
вызывающих) удаляются.

`UnOccupyGroup` принимает id группы, а тип проживания в нём не закодирован — хэндл определяет
корень одним лёгким запросом «какого типа эта группа» перед загрузкой плана.

Права и активность — явными параметрами каждой операции:

| Операция | Право | Активность |
|---|---|---|
| `AddRooms`, `RenameRoom`, `DeleteRoom` | `CanManageAccommodation` | `MustBeActive` |
| `OccupyRoom`, `UnOccupy*` | `CanSetPlayersAccommodations` | `MustBeActive` |

Это и есть закрытие дефектов 1 и 4. Разделение прав сохраняет сегодняшнее поведение атрибутов
контроллера: управление комнатами и расселение игроков — разные права.

Проверки внутри мутации (дефекты 5, 6, 7):

- группа существует в плане и, значит, **того же типа**, что комната (`plan.GetGroup` кидает, если
  нет) — дефект 5 закрывается самой формой агрегата;
- свободное место считается по снимку явно, с учётом уже поселённых в этой же операции групп, без
  надежды на relationship fixup — `JoinRpgInsufficientRoomSpaceException`;
- отсутствие комнаты или группы — `JoinRpgEntityNotFoundException` с типизированным id, а не NRE.
  Контроллер после этого может перестать ловить всё подряд в `catch { return StatusCode(500); }`.

`JoinRpgInsufficientRoomSpaceException` и `RoomIsOccupiedException` сегодня принимают EF-сущность
`ProjectAccommodation` (`src/JoinRpg.Domain/Exceptions.cs`); переводятся на
`AccommodationRoomIdentification` — так же, как ADR014 поступил с `ClaimWrongStatusException`.

### 5. Письма

`OccupyRoomEmail`/`UnOccupyRoomEmail` (`RoomEmailBase`) несут EF-сущности `ProjectAccommodation Room`
и `Claim[] Changed`, а `EmailServiceImpl` зовёт по ним `GetAllInhabitants()`. Переписывать
почтовый контур этот ADR не берётся: письма отправляются через `ctx.AddLegacyEmail(...)` — тот же
механизм, которым ADR014 сохранил легаси-письма заявок. Единственное изменение по существу:
письмо уходит **после** успешного `SaveChanges`, один раз на операцию, а не по письму на комнату
внутри цикла.

### 6. Потребители

| Потребитель | Сейчас | Станет |
|---|---|---|
| `AccommodationTypeController.EditRoomTypeRooms` | `accommodationService.GetRoomTypeAsync` + ручная сверка `ProjectId` | `IAccommodationPlanRepository.GetPlanOrDefault` |
| `RoomTypeViewModel(ProjectAccommodationType, …)` | EF-сущность с `Include(ProjectAccommodations/Desirous)` | `AccommodationPlan`; EF-конструктор удаляется, остаётся уже существующий поверх `AccommodationTypeInfo` (ADR015) |
| `RoomViewModel(ProjectAccommodation, …)` | EF-сущность | `RoomInfo` |
| `AccRequestViewModel(AccommodationRequest, …)` + `RequestParticipantViewModel(Claim, …)` | EF-сущности, `Claim.ClaimTotalFee`/`ClaimFeeDue` (оба `[Obsolete]`) | `AccommodationGroupInfo` + bulk `CharacterInfo`, `CalculateClaimBalance` поверх агрегата |
| `AccommodationTypeController` (Occupy/UnOccupy/AddRoom/EditRoom/DeleteRoom) | `int`-параметры, `catch`-всё | типизированные id, доменные исключения |
| `SmokeProjectFixture` (интеграционные тесты) | `AddRooms(projectId.Value, roomTypeId.AccommodationTypeId, "1,2")` | `AddRooms(roomTypeId, "1,2")` |
| `AccommodationPrintController`, `AccomodationReportExporter` | `IAccommodationRepository.GetClaimAccommodationReport` | не меняются — это отчёт, плоские строки, агрегат ему не нужен |
| `AccommodationInviteServiceImpl`, `ClaimAccommodationViewModel` | `AccommodationExtensions` поверх EF | не меняются, см. «Что вне скоупа» |

`IAccommodationRepository.GetRoomTypesForProject` (строки со счётчиками занятости для страницы
«Типы проживания») остаётся: это сводка по всему проекту, и гонять ради неё полный план каждого
типа незачем.

### 7. Что вне скоупа

- **Приглашения** (`AccommodationInvite`, `AccommodationInviteServiceImpl`) и **выбор типа игроком**
  (`SetAccommodationType`, `LeaveAccommodationGroupAsync`). Они уже живут в character-контуре
  (ADR014) либо ждут собственной миграции; здесь меняется только колонка «в какой комнате».
- **Почтовые модели** `RoomEmailBase` и `EmailServiceImpl` — остаются на EF-сущностях.
- **Автозаселение** (`OccupyAll` с `//TODO: Implement mass occupation`) — не реализовано сегодня,
  не реализуется и здесь. Агрегат делает его дешевле: вместимость и свободные группы уже собраны.
- **Вместимость на уровне комнаты** (`RoomInfo.Capacity` сверх типа) — поле заводится, данных под
  ним нет, миграции нет.

### 8. План миграции

1. **PR 1.** `AccommodationRoomIdentification`; `AccommodationPlan`, `RoomInfo`,
   `AccommodationGroupInfo` с инвариантами; юнит-тесты в `JoinRpg.DomainTypes.Test` поверх
   `MockedProject`. Потребителей не трогаем.
2. **PR 2.** `IAccommodationPlanRepository` + загрузчик + тесты маппинга в `JoinRpg.Dal.Impl.Test`,
   включая тест-страж: свободное место по плану совпадает с `AccommodationExtensions.GetRoomFreeSpace`
   на том же наборе данных.
3. **PR 3.** Страница комнат на план: `EditRoomTypeRooms`, вью-модели, деньги bulk'ом через
   `ICharacterInfoRepository`. `GetRoomTypeAsync` удаляется.
4. **PR 4.** Write-хэндл, `IAccommodationPropsService`, вынос `ProjectOperationGuard` (если
   наберётся), перевод `AddRooms`/`RenameRoom`/`DeleteRoom` — права, активность, типизированный
   проект. Закрывает дефекты 1, 2, 4 для управления комнатами.
5. **PR 5.** Перевод `OccupyRoom`/`UnOccupy*` — один `SaveChanges` на операцию, проверка типа,
   явный подсчёт мест, письма через `AddLegacyEmail`. Закрывает дефекты 3, 5, 6, 7.
6. **PR 6.** Зачистка: `GetRoomFreeSpace(room)`/`IsOccupied` из `AccommodationExtensions`,
   классы-запросы `OccupyRequest` и соседи, EF-конструкторы вью-моделей, `catch`-всё в контроллере.

Каждый из дефектов 1–7 сопровождается юнит-тестом, воспроизводящим его до фикса (правило CLAUDE.md
про фикс багов). Для дефектов 1, 2 и 4 это тесты сервиса на фейковом хэндле — ровно как сделано
для `AccommodationTypeService` (`src/JoinRpg.Services.Impl.Test/Projects/AccommodationTypeServiceTest.cs`).

Последствия
==

- **Сервисный слой поселения перестаёт торговать EF-сущностями.** После ADR015 и этого ADR
  `JoinRpg.Domain/AccommodationExtensions.cs` остаётся только с частями, обслуживающими контур
  приглашений.
- **Права на комнаты перестают быть свойством контроллера** — дефекты, живущие с #5017 в типах
  и до сих пор в комнатах, закрываются одинаково.
- **Массовое выселение становится атомарным.** Цена — одна длинная транзакция вместо N коротких;
  для проекта с сотнями комнат это заметно, но частичное выселение хуже медленного.
- **Третий props-сервис.** Их становится три (проект, персонаж, поселение), и это тот момент, когда
  общий цикл «право → активность → мутация → сохранение → лог» стоит вынести один раз.
- **Правило владения `AccommodationRequest` зафиксировано по колонкам.** Это не самая изящная
  граница, и её придётся держать в голове при следующей правке claim-контура — зато она явная,
  а не выведенная из того, кто первым дотянулся до сущности.
- **Переходный период**: пока PR 3–5 не влиты, план сосуществует со старым `IAccommodationService`.

Открытые вопросы
==

- **Цифр по проду нет.** Сколько комнат на крупном проекте и сколько времени занимает `UnOccupyAll`
  — не измерено; оценка «комнат столько же, сколько заявок» сделана по смыслу сущности. Если
  окажется, что на боевом проекте комнат тысячи, выбор «транзакция на весь проект» для `UnOccupyAll`
  придётся пересмотреть — вплоть до возврата к транзакции на тип.
- **Вместимость в `RoomInfo`** заведена без данных под ней. Альтернатива — не заводить и брать
  `AccommodationType.Capacity` в точке использования; решено в пользу поля, потому что иначе
  `FreeSpace` невозможно посчитать, не таща тип в каждую комнату. Если «нестандартные комнаты» так и
  не появятся, поле останется производным — это дёшево.
- **`ProjectOperationGuard`** — выносить или нет, решается фактом при реализации PR 4, а не этим ADR.

Статус
==

Предложен. Issue #5037.
