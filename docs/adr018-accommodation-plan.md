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

### 1. Корень агрегата — пул комнат, а не тип проживания, не проект и не комната

Границу задаёт инвариант. Их три:

- в комнате не больше жильцов, чем позволяет вместимость;
- группа селится только в комнату из **своего** пула;
- удалить можно только незаселённую комнату.

Сегодня пул комнат и тип проживания — одно и то же: у `ProjectAccommodation` есть
`AccommodationTypeId`, и комнаты принадлежат типу. **Но это ненадолго**, см. §2: тип проживания и
категорию комнат планируется разделить, и тогда несколько типов будут брать комнаты из общего пула.
Инвариант вместимости при этом станет **над-типовым**: в комнате 101 могут оказаться группы,
купившие разные типы, и свободное место в ней перестанет быть свойством одного типа.

Поэтому корень агрегата — **пул комнат (категория)**, а не тип проживания. Сегодня это различение
без разницы в данных (пул ровно один на тип), но с разницей в модели: агрегат с самого начала
устроен как «комнаты плюс *множество* типов, которые из них селятся», а не «комнаты одного типа».

Комната как корень слишком мелка: проверка вместимости требует всех её жильцов, а массовые операции
(`UnOccupyRoomType`) трогают десятки комнат разом — с корнем-комнатой они так и остались бы циклом с
N сохранениями.

Проект как корень **отвергнут не по объёму**. На крупном проекте комнат десятки, в пределе — единицы
сотен (оценка @leotsarev по боевым данным), так что загрузка всех комнат проекта сама по себе не
проблема и аргумента в духе [ADR011](adr011-roles-grid-payload.md) здесь нет. Против него другое:
проектный корень не выражает ни одного из трёх инвариантов и потому превращает границу агрегата в
границу «всё поселение», где каждая операция вольна трогать что угодно. Ровно от этого уходят
ADR013/ADR014.

Пул попадает в середину: это **и** естественная граница инвариантов (в том числе будущих,
над-типовых), **и** то, что уже грузится одним запросом (`GetRoomTypeAsync` с
`Include(ProjectAccommodations)` + `Include(Desirous)`), **и** то, что показывает страница комнат
`EditRoomTypeRooms`.

Плата за выбор честная: проектный корень был бы проще для `UnOccupyAll` и для будущего
автозаселения. Раз объём комнат невелик, схлопнуть план до проектного корня позже — рефакторинг на
уровне загрузчика, а не переделка модели.

Проектная операция `UnOccupyAll` остаётся операцией **над несколькими агрегатами** — цикл по пулам
внутри одной мутации и одного `SaveChanges`. Это сознательное отступление: для редкой
административной операции атомарность важнее чистоты границы, а дефект №3 именно про то, что
сегодня её нет.

### 2. Задел на разделение `AccommodationType` и `RoomCategory`

Планируемое (не в этом ADR и не в ближайших PR) разделение: **категория комнат** — «Люкс» с
комнатами 101, 102; **типы проживания** — «Люкс с пятницы», «Люкс с четверга», «Люкс с пятницы на
одного», каждый со своей ценой и своей вместимостью, и все трое селятся из общего пула комнат
категории «Люкс». Цена и вместимость — у типа; комнаты — у категории.

Схема БД и `ProjectInfo` сейчас не меняются. Меняется только форма доменной модели — так, чтобы
разделение потом было расширением, а не переделкой. Отсюда четыре решения:

1. **План ключуется категорией, и конвертации id в домене нет.** Заводится
   `RoomCategoryIdentification`. Публичной фабрики «id типа → id категории» **не существует**:
   знание о том, к какой категории относится тип, — это настройка мастера, то есть свойство
   `AccommodationTypeInfo.RoomCategoryId` (по критерию ADR015 — там же, где имя и цена). Кому нужен
   пул по типу, спрашивает `projectInfo.AccommodationSettings.GetTypeById(typeId).RoomCategoryId`;
   у кого на руках категория — конвертировать нечего.

   Полностью фикция всё же не исчезает: сегодня за `RoomCategoryId` нет колонки, поэтому маппер
   метаданных заполняет его из `ProjectAccommodationType.Id`, а write-репозиторий на обратном пути
   кладёт `categoryId.RoomCategoryId` в существующую колонку
   `ProjectAccommodation.AccommodationTypeId`. Оба места — **в DAL**, где отображение «id ↔ колонка»
   и так живёт для каждой сущности, и оба помечены комментарием про будущий split. Смысл решения
   именно в этом: конвертацию нельзя устранить без миграции, но можно не выносить её в домен
   публичным API.

   Остаточный риск назван честно: числа совпадают, поэтому собранный вручную
   `new RoomCategoryIdentification(projectId, <число типа>)` сработает молча и сломается в день
   разделения. Исчезает не возможность так сделать, а повод. Радикальное лекарство — таблица
   `RoomCategory` уже сейчас — **отвергнуто** (@leotsarev): миграцию ради задела не делаем.
   Вместо неё дешёвая страховка: тест-страж в `JoinRpg.Dal.Impl.Test` фиксирует, что сегодня
   `type.RoomCategoryId.RoomCategoryId == type.Id.AccommodationTypeId`, со ссылкой на этот параграф.
   В день разделения он падёт первым и приведёт разработчика ровно сюда, а не в случайное место,
   где id разъехались.
2. **План держит коллекцию типов, а не один тип.** Сегодня в ней ровно один элемент; инвариант
   «ровно один» **не** проверяется, чтобы его не пришлось снимать при разделении.
3. **Вместимостей две, и они про разное.** У типа проживания — **сколько человек мы продаём** в
   комнату (`AccommodationTypeInfo.Capacity`, уже есть с ADR015). У комнаты — **сколько в ней
   коек**, физический предел (`RoomInfo.Capacity`, заводится этим ADR, нужна колонка в БД).
   Свободное место считается по обеим и потому остаётся **функцией от пары «комната + тип
   кандидата»**: `plan.GetFreeSpace(roomId, typeId)`.
4. **Группа знает свой тип.** `AccommodationGroupInfo` несёт `AccommodationTypeIdentification` —
   сегодня он у всех групп плана одинаковый и выводится из плана, завтра станет существенным.

#### Правило свободного места

Тип, под которым группу поселили, **ограничивает комнату целиком, а не одно место**: если в «Люкс»
(две койки) въехала группа типа «Люкс на одного», комната занята полностью. Отсюда правило:

> Вместимость комнаты = минимум из её физической вместимости и вместимостей типов всех, кто в ней
> уже живёт. Кандидат добавляет в этот минимум ещё и вместимость своего типа.

```
effective(room)          = min(room.Capacity, min по типам жильцов)
freeSpace(room, type)    = min(effective(room), type.Capacity) − occupancy(room)
```

Правило покрывает и сегодняшний день, и день после разделения. Сегодня тип в пуле один, физическая
вместимость при бэкфилле равна его вместимости, и формула вырождается в нынешнее
`type.Capacity − occupancy` — поведение не меняется.

#### Что это чинит и чем платит

Физическая вместимость комнаты закрывает три дырки разом:

- **«Сколько всего мест» снова имеет ответ** — `Σ room.Capacity` по комнатам пула. Сегодня
  `RoomTypeViewModelBase.TotalCapacity` считает `RoomsCount * Capacity`, а
  `AccommodationListViewModel` суммирует это по типам проекта; на общем пуле такая сумма посчитала
  бы одни и те же комнаты дважды. Сумма по комнатам двойного счёта не знает.
- **«Комната заполнена» снова определено** — `occupancy >= effective(room)`.
  `AccommodationRepositoryImpl.FullyOccupiedRoomsCount` сегодня сравнивает занятость с вместимостью
  **типа**; после разделения это было бы бессмысленно, теперь у отчёта есть правило.
- **Перепродажу видно.** Тип с вместимостью 10 над двухместной комнатой больше не даёт поселить
  десятерых: физический предел режет.

Плата — **миграция БД**: у `ProjectAccommodation` появляется колонка `Capacity` с бэкфиллом из
вместимости типа (единственная правда, доступная на момент миграции). Это изменение
`JoinRpg.DataModel`, требующее согласования по правилу CLAUDE.md — оно получено в обсуждении этого
ADR. Порядок — по [ef-migrations.md](ef-migrations.md), с перегенерацией
[db-schema.md](db-schema-regenerate.md).

Вторая часть платы — **UI**: мастеру теперь видны две вместимости, и их легко перепутать. `AddRooms`
получает вместимость параметром (по умолчанию — вместимость типа), на странице комнат вместимость
становится редактируемой. Как назвать их так, чтобы мастер не запутался, — в открытых вопросах.

Что при разделении придётся доделать (и что этот ADR **не** решает): как `RoomCategory` попадает в
`ProjectInfo`. По критерию ADR015 ответ очевиден: категория — настройка мастера, её место рядом с
`AccommodationTypeInfo` в `AccommodationSettings`, а `AccommodationTypeInfo` получает ссылку
`RoomCategoryId`. Комнаты остаются оперативными данными и в метаданные не едут.

Одно следствие стоит отметить заранее: после разделения `UnOccupyRoomType` перестанет означать
«выселить все комнаты этого типа» и станет означать «выселить все группы этого типа», оставив в
комнатах соседей из братских типов. Сегодня это одно и то же, поэтому операция принимает **тип**, а
не категорию, — и после разделения её смысл сузится сам собой, без смены сигнатуры.

### 3. Сами типы проживания в агрегат не копируются

`AccommodationPlan` держит ссылку на `ProjectInfo` и на `AccommodationTypeInfo` из его
`AccommodationSettings` — ровно как `CharacterInfo` держит `ProjectInfo`, а не пересобирает поля
проекта. Это прямое следствие ADR015: единственный источник правды о типе — метаданные проекта,
и второго не заводим. Вместимость и цена для расчётов берутся оттуда же.

Следствие: инвариант ссылочного равенства из ADR013 действует и здесь — каждый элемент
`Plan.AccommodationTypes` обязан быть тем же экземпляром, что лежит в
`Plan.ProjectInfo.AccommodationSettings`, — и межзапросного кеша у плана нет.

### 4. Заявку на проживание делят два агрегата — и делят по колонкам

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

### 5. Деньги в агрегат не входят

Страница комнат показывает по каждому жильцу «не оплачено X из Y». Складывать финансовые поля в
`AccommodationPlan` — значит завести третью модель расчёта баланса рядом с `Claim` и
`CharacterClaimInfo`. Вместо этого план несёт `ClaimIdentification` жильцов, а вью-сервис страницы
догружает их bulk'ом через существующий `ICharacterInfoRepository.GetCharacterInfos(ids)` и считает
баланс уже принятым способом — `FinanceExtensions.CalculateClaimBalance(CharacterInfo, CharacterClaimInfo, ProjectInfo)`.
Это один дополнительный запрос на страницу, не N+1.

Имена игроков — тем же путём, что в ADR013: `UserInfoHeader` bulk'ом, не `Include` на каждого.

### 6. Отвергнутые альтернативы

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

### 7. Ответ на возражение ADR015 про дублирование машинерии

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

### 1. Типизированные идентификаторы комнаты и категории

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

/// <summary>
/// Идентификатор категории комнат — пула, из которого селятся типы проживания.
/// </summary>
/// <remarks>
/// Пока категория и тип проживания не разделены (см. ADR018 §2), своей таблицы у категории нет.
/// Получить категорию по типу проживания можно только через метаданные —
/// <c>AccommodationTypeInfo.RoomCategoryId</c>; конвертации идентификаторов в домене нет и заводить
/// её нельзя.
/// </remarks>
[method: JsonConstructor]
[TypedEntityId]
public partial record RoomCategoryIdentification(
    ProjectIdentification ProjectId,
    int RoomCategoryId) : IProjectEntityId;
```

Вместе с этим `AccommodationTypeInfo` (заведён ADR015) получает свойство
`RoomCategoryIdentification RoomCategoryId` — единственный способ узнать пул по типу проживания.
Сегодня `ProjectMetadataRepository` заполняет его из `ProjectAccommodationType.Id`; колонки за ним
пока нет, и это отмечено комментарием в маппере.

### 2. Доменные типы

Файлы: новые, `src/JoinRpg.DomainTypes/Accommodation/`.

```csharp
/// <summary>
/// План поселения одного пула комнат (категории): его комнаты, типы проживания, которые из него
/// селятся, и группы жильцов.
/// Привязан к конкретному экземпляру <see cref="ProjectInfo"/>, кешированию между запросами
/// не подлежит.
/// </summary>
public record class AccommodationPlan
{
    public RoomCategoryIdentification Id { get; }
    public ProjectInfo ProjectInfo { get; }

    /// <summary>
    /// Типы проживания, селящиеся из этого пула. Пока тип и категория не разделены (§2) — ровно
    /// один; «ровно один» сознательно не является инвариантом.
    /// </summary>
    public IReadOnlyCollection<AccommodationTypeInfo> AccommodationTypes { get; }

    public IReadOnlyCollection<RoomInfo> Rooms { get; }

    /// <summary>Все группы этого пула, включая ещё не расселённые</summary>
    public IReadOnlyCollection<AccommodationGroupInfo> Groups { get; }

    public IEnumerable<AccommodationGroupInfo> UnassignedGroups => Groups.Where(g => g.RoomId is null);

    public RoomInfo GetRoom(AccommodationRoomIdentification roomId);
    public AccommodationGroupInfo GetGroup(AccommodationRequestIdentification groupId);
    public AccommodationTypeInfo GetAccommodationType(AccommodationTypeIdentification typeId);

    /// <summary>Сколько всего мест в пуле — сумма физических вместимостей комнат</summary>
    public int TotalCapacity => Rooms.Sum(r => r.Capacity);

    /// <summary>
    /// Сколько человек вмещает комната при нынешних жильцах: физический предел, ужатый
    /// вместимостями типов тех, кто уже въехал (тип занимает комнату целиком, см. §2).
    /// </summary>
    public int GetEffectiveCapacity(AccommodationRoomIdentification roomId)
    {
        var room = GetRoom(roomId);
        return room.Inhabitants
            .Select(i => GetAccommodationType(i.AccommodationTypeId).Capacity)
            .Append(room.Capacity)
            .Min();
    }

    public bool IsFull(AccommodationRoomIdentification roomId)
        => GetRoom(roomId).Occupancy >= GetEffectiveCapacity(roomId);

    /// <summary>
    /// Сколько человек ещё можно поселить в комнату под данным типом проживания.
    /// </summary>
    /// <remarks>
    /// Свободное место — функция от пары «комната + тип кандидата»: физический предел комнаты и
    /// вместимости типов всех участников (§2). Сегодня, когда тип в пуле один, а физическая
    /// вместимость равна его вместимости, формула вырождается в «вместимость типа минус занятость».
    /// </remarks>
    public int GetFreeSpace(AccommodationRoomIdentification roomId, AccommodationTypeIdentification typeId)
        => Math.Min(GetEffectiveCapacity(roomId), GetAccommodationType(typeId).Capacity)
           - GetRoom(roomId).Occupancy;
}

/// <summary>
/// Комната. <paramref name="Capacity"/> — физический предел (сколько коек); сколько человек
/// продаётся в комнату, говорит тип проживания, см. <see cref="AccommodationPlan.GetFreeSpace"/>.
/// </summary>
public record class RoomInfo(
    AccommodationRoomIdentification Id,
    string Name,
    int Capacity,
    IReadOnlyCollection<AccommodationGroupInfo> Inhabitants)
{
    public int Occupancy => Inhabitants.Sum(i => i.Persons);
    public bool IsOccupied => Inhabitants.Count > 0;
}

/// <summary>Группа, живущая (или желающая жить) вместе — одна <c>AccommodationRequest</c></summary>
public record class AccommodationGroupInfo(
    AccommodationRequestIdentification Id,
    AccommodationTypeIdentification AccommodationTypeId,
    AccommodationRoomIdentification? RoomId,
    IReadOnlyCollection<ClaimIdentification> Subjects)
{
    public int Persons => Subjects.Count;
}
```

Инварианты — в конструкторе `AccommodationPlan`, как это делают `CharacterTypeInfo` и `CharacterInfo`:

- `Id.ProjectId == ProjectInfo.ProjectId`, и все `AccommodationTypes` — те самые экземпляры, что
  лежат в `ProjectInfo.AccommodationSettings` (`ReferenceEquals`);
- у всех комнат и групп `ProjectId` совпадает с `Id.ProjectId`;
- `RoomInfo.Inhabitants ⊆ Groups`, и группа с непустым `RoomId` присутствует ровно в одной комнате;
- `Groups[i].AccommodationTypeId ∈ AccommodationTypes` — группа куплена по типу, селящемуся из
  этого пула.

**Чего среди инвариантов сознательно нет:**

- «типов ровно один» — сегодня это правда, но проверка пришлось бы снимать при разделении (§2);
- «вместимость не превышена» — сегодняшние данные могли переполниться (вместимость типа уменьшили
  после заселения), и загрузка плана не должна падать на таком проекте. Это проверка операции, а не
  инварианта загрузки. По той же причине не проверяется и `room.Capacity >= occupancy`.

`GetRoomFreeSpace(ProjectAccommodation)` и `IsOccupied(ProjectAccommodation)` из
`src/JoinRpg.Domain/AccommodationExtensions.cs` заменяются на `AccommodationPlan.GetFreeSpace` и
`RoomInfo.IsOccupied` и после миграции удаляются. Обрати внимание: `GetRoomFreeSpace` перестаёт
быть методом комнаты — это и есть то место, где разделение типа и категории оставило след в
сегодняшнем коде. Остальные методы файла (`GetClaimNeighbours`,
`GetRoomFreeSpace(AccommodationRequest)`) обслуживают контур приглашений и карточку заявки — они вне
скоупа и остаются.

### 3. Загрузка

Файл: новый, `src/JoinRpg.Data.Interfaces/Accommodation/IAccommodationPlanRepository.cs`.

```csharp
public interface IAccommodationPlanRepository
{
    Task<AccommodationPlan?> GetPlanOrDefault(RoomCategoryIdentification categoryId);
    Task<IReadOnlyCollection<AccommodationPlan>> GetAllPlans(ProjectIdentification projectId);

    /// <summary>
    /// План пула, из которого селится данный тип проживания. Пока тип и категория не разделены,
    /// это тот же план, что <see cref="GetPlanOrDefault"/> по одноимённой категории; после
    /// разделения один план будут возвращать несколько типов.
    /// </summary>
    Task<AccommodationPlan?> GetPlanForTypeOrDefault(AccommodationTypeIdentification typeId);
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
        RoomCategoryIdentification categoryId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<AccommodationMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "");
}
```

Плюс перегрузка без результата и перегрузка по `ProjectIdentification` (для `UnOccupyAllRooms`,
которая работает по всем пулам проекта в одной транзакции).

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
    /// <summary>Комнаты добавляются в пул, а не в тип проживания</summary>
    Task<IReadOnlyCollection<AccommodationRoomIdentification>> AddRooms(
        RoomCategoryIdentification categoryId, string rooms);

    Task RenameRoom(AccommodationRoomIdentification roomId, string name);
    Task DeleteRoom(AccommodationRoomIdentification roomId);

    Task OccupyRoom(
        AccommodationRoomIdentification roomId,
        IReadOnlyCollection<AccommodationRequestIdentification> groupIds);

    /// <summary>Выселить все группы данного типа проживания. После разделения типа и категории
    /// соседи из братских типов останутся в комнатах — см. §2</summary>

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

- и комната, и группа существуют в одном плане, то есть группа селится в комнату **своего пула**
  (`plan.GetRoom`/`plan.GetGroup` кидают, если нет) — дефект 5 закрывается самой формой агрегата, и
  закрывается в той формулировке, которая переживёт разделение типа и категории;
- свободное место считается явно — `plan.GetFreeSpace(roomId, group.AccommodationTypeId)` по снимку,
  с учётом уже поселённых в этой же операции групп, без надежды на relationship fixup —
  `JoinRpgInsufficientRoomSpaceException`;
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
| `AccommodationTypeController.EditRoomTypeRooms` | `accommodationService.GetRoomTypeAsync` + ручная сверка `ProjectId` | `IAccommodationPlanRepository.GetPlanForTypeOrDefault` |
| `RoomTypeViewModel(ProjectAccommodationType, …)` | EF-сущность с `Include(ProjectAccommodations/Desirous)` | `AccommodationPlan`; EF-конструктор удаляется, остаётся уже существующий поверх `AccommodationTypeInfo` (ADR015) |
| `RoomViewModel(ProjectAccommodation, …)` | EF-сущность | `RoomInfo` |
| `AccRequestViewModel(AccommodationRequest, …)` + `RequestParticipantViewModel(Claim, …)` | EF-сущности, `Claim.ClaimTotalFee`/`ClaimFeeDue` (оба `[Obsolete]`) | `AccommodationGroupInfo` + bulk `CharacterInfo`, `CalculateClaimBalance` поверх агрегата |
| `AccommodationTypeController` (Occupy/UnOccupy/AddRoom/EditRoom/DeleteRoom) | `int`-параметры, `catch`-всё | типизированные id, доменные исключения |
| `SmokeProjectFixture` (интеграционные тесты) | `AddRooms(projectId.Value, roomTypeId.AccommodationTypeId, "1,2")` | `AddRooms(projectInfo.AccommodationSettings.GetTypeById(roomTypeId).RoomCategoryId, "1,2")` |
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
- **Само разделение `AccommodationType` и `RoomCategory`** — здесь только форма доменной модели под
  него (§2). Таблицы, миграции, UI категорий, правило свободного места в смешанной комнате и
  перенос `RoomCategory` в `ProjectInfo` — отдельная работа и, вероятно, отдельный ADR.

### 8. План миграции

0. **PR 0** — этот ADR и ссылка на него в [docs/README.md](README.md).
1. **PR 1.** Миграция БД: колонка `ProjectAccommodations.Capacity` с бэкфиллом из вместимости типа
   ([ef-migrations.md](ef-migrations.md)), навигация в `JoinRpg.DataModel`, перегенерация
   [db-schema.md](db-schema-regenerate.md). Отдельным PR, потому что это единственное изменение
   схемы во всём плане.
2. **PR 2.** `AccommodationRoomIdentification`, `RoomCategoryIdentification`,
   `AccommodationTypeInfo.RoomCategoryId` с маппингом; `AccommodationPlan`,
   `RoomInfo`, `AccommodationGroupInfo` с инвариантами; юнит-тесты в `JoinRpg.DomainTypes.Test`
   поверх `MockedProject` — в том числе тест на план с двумя типами в одном пуле, который сегодня
   не собирается из БД, но обязан собираться из модели. Потребителей не трогаем.
3. **PR 3.** `IAccommodationPlanRepository` + загрузчик + тесты маппинга в `JoinRpg.Dal.Impl.Test`,
   включая тест-страж: `plan.GetFreeSpace(room, type)` совпадает с
   `AccommodationExtensions.GetRoomFreeSpace` на том же наборе данных.
4. **PR 4.** Страница комнат на план: `EditRoomTypeRooms`, вью-модели, деньги bulk'ом через
   `ICharacterInfoRepository`, редактирование физической вместимости комнаты.
   `GetRoomTypeAsync` удаляется.
5. **PR 5.** Write-хэндл, `IAccommodationPropsService`, вынос `ProjectOperationGuard` (если
   наберётся), перевод `AddRooms`/`RenameRoom`/`DeleteRoom` — права, активность, типизированный
   проект. Закрывает дефекты 1, 2, 4 для управления комнатами.
6. **PR 6.** Перевод `OccupyRoom`/`UnOccupy*` — один `SaveChanges` на операцию, проверка пула,
   явный подсчёт мест, письма через `AddLegacyEmail`. Закрывает дефекты 3, 5, 6, 7.
7. **PR 7.** Зачистка: `GetRoomFreeSpace(room)`/`IsOccupied` из `AccommodationExtensions`,
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
- **Массовое выселение становится атомарным.** Цена — одна транзакция вместо N коротких, и при
  десятках-сотнях комнат на проекте она остаётся короткой.
- **Третий props-сервис.** Их становится три (проект, персонаж, поселение), и это тот момент, когда
  общий цикл «право → активность → мутация → сохранение → лог» стоит вынести один раз.
- **Правило владения `AccommodationRequest` зафиксировано по колонкам.** Это не самая изящная
  граница, и её придётся держать в голове при следующей правке claim-контура — зато она явная,
  а не выведенная из того, кто первым дотянулся до сущности.
- **Модель готова к разделению типа и категории раньше, чем БД.** Цена уплачена вперёд: лишний
  идентификатор, коллекция из одного элемента и свободное место, вынесенное из комнаты в план.
  Если разделение не состоится, это останется небольшой избыточностью — но не ошибкой: `GetFreeSpace`
  на плане честнее и сегодня, потому что вместимость и правда лежит на типе, а не на комнате.
- **Переходный период**: пока PR 3–5 не влиты, план сосуществует со старым `IAccommodationService`.

Открытые вопросы
==

- ~~Сколько комнат на крупном проекте~~ — **закрыт**: десятки, в пределе единицы сотен
  (@leotsarev). Транзакция на весь проект для `UnOccupyAll` при таком объёме безопасна, к варианту
  «транзакция на тип» возвращаться не нужно. Время самой операции по-прежнему не измерено, но при
  таком числе комнат это перестало быть риском решения.
- ~~Вместимость в `RoomInfo`~~ — **снят**: вместимости у комнаты нет вовсе, см. §2.3.
- ~~Правило свободного места в комнате со смешанными типами~~ — **закрыт** (@leotsarev): тип
  занимает комнату целиком, «Люкс на одного» делает «Люкс» полным. Формула — в §2.
- **Как назвать две вместимости в UI.** Мастер увидит вместимость у типа проживания («сколько
  продаём») и у комнаты («сколько коек»); одинаково названные, они гарантированно будут перепутаны.
  Названия и подсказки — к PR 4, вопрос к @leotsarev.
- ~~Заводить ли таблицу `RoomCategory` сразу~~ — **закрыт: не заводим** (@leotsarev). Миграция и
  правка `DataModel` ради одного лишь задела не окупаются. Следствие принимается сознательно:
  `RoomCategoryIdentification` численно равен `AccommodationTypeIdentification`, поля-фикции в
  маппере и в write-репозитории остаются, и ошибка «передали не тот id» до дня разделения молчит.
  Страховка — отсутствие конвертации в домене плюс тест-страж на сегодняшнее равенство (§2.1).
- **`ProjectOperationGuard`** — выносить или нет, решается фактом при реализации PR 4, а не этим ADR.

Статус
==

Предложен. Issue #5037.
