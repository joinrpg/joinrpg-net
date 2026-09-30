# ADR018: RoomCategoryPlan — доменный агрегат комнат и заселения

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

3. **`UnOccupyRoomType` и `UnOccupyAll` делают сохранение на каждую комнату.** Каждая комната
   выселяется отдельным `UnOccupyRoomImpl` со своим `SaveChangesAsync`; падение на пятой комнате
   оставляет четыре выселенными. Проверка прав тоже повторяется на каждой комнате. Чинится
   **наполовину и намеренно** — см. §1.

4. **Ни один метод не проверяет активность проекта.** Заселять и выселять можно в архивном проекте.

5. **`OccupyRoom` не проверяет, что заявка на проживание того же типа, что комната.** Комната
   грузится по `(ProjectId, RoomId)`, заявки — по `(ProjectId, Id ∈ ids)`, а `roomTypeId`,
   который контроллер передаёт в форме, до сервиса не доходит вовсе. То есть заявку на палатку
   можно поселить в гостиничный номер. Сегодня от этого защищает только UI.

6. **Проверка свободного места опирается на relationship fixup EF** — на то, что EF, увидев
   присвоение `accommodationRequest.Accommodation = room`, сам добавит заявку в обратную навигацию
   `room.Inhabitants`. В цикле по заявкам `room.GetRoomFreeSpace()` считает жильцов именно по
   `room.Inhabitants`, то есть корректность цикла держится на поведении трекера EF6, нигде в коде не
   выраженном. Считать вместимость нужно явно.

7. **`OccupyRoom`/`UnOccupyRoom` падают NRE, если комнаты или заявки нет** — `FirstOrDefaultAsync`
   без проверки на `null`, дальше сразу `room.Project`. Контроллер превращает это в 500.

Решение:
==

**Комнаты и заселение — отдельный агрегат `RoomCategoryPlan`**, по одному на категорию комнат, со
своим доменным типом, загрузчиком и точкой мутации — по образцу `CharacterInfo`
([ADR013](adr013-character-info.md)) и `CharacterPropsService`
([ADR014](adr014-claim-props-service.md)).

Имя выбрано так, чтобы область агрегата была видна в нём самом: план — про **одну** категорию
комнат, а не про поселение проекта целиком. `RoomCategoryInfo` сознательно не занимаем — после
разделения (§2) категория станет настройкой мастера внутри `ProjectInfo`, и это имя понадобится ей,
парой к `AccommodationTypeInfo`. Разделение труда тогда ляжет ровно по границе ADR015:
`RoomCategoryInfo` — настройка, `RoomCategoryPlan` — оперативные данные.

Схема БД этим ADR **не меняется**: ни новых таблиц, ни новых колонок, ни миграций.

Два слова, которыми пользуется весь документ:

- **пул** (он же категория комнат) — набор комнат, из которого селятся один или несколько типов
  проживания. Сегодня пул ровно один на тип, завтра — см. §2;
- **группа** — компания, живущая или желающая жить вместе, одна строка `AccommodationRequest`.
  Именно группа, а не отдельная заявка игрока, заселяется в комнату.

### 1. Корень агрегата — пул комнат, а не тип проживания, не проект и не комната

Границу задаёт инвариант. Их три:

- в комнате не больше жильцов, чем позволяет вместимость;
- группа селится только в комнату из **своего** пула;
- удалить можно только незаселённую комнату.

Сегодня пул и тип проживания — одно и то же: у `ProjectAccommodation` есть `AccommodationTypeId`, и
комнаты принадлежат типу. Но это ненадолго (§2): тип проживания и категорию комнат планируется
разделить, и тогда несколько типов будут брать комнаты из общего пула. Инвариант вместимости при
этом станет **над-типовым**: в комнате 101 окажутся группы, купившие разные типы, и свободное место
в ней перестанет быть свойством одного типа.

Поэтому корень — пул. Сегодня это различение без разницы в данных, но с разницей в модели: агрегат с
самого начала устроен как «комнаты плюс *множество* типов, которые из них селятся», а не «комнаты
одного типа».

Комната как корень слишком мелка: проверка вместимости требует всех её жильцов, а массовые операции
(`UnOccupyRoomType`) трогают десятки комнат разом — с корнем-комнатой они так и остались бы циклом с
N сохранениями. Проект как корень не выражает ни одного из трёх инвариантов и превращает границу
агрегата в «всё поселение», где каждая операция вольна трогать что угодно; по объёму он приемлем
(комнат на крупном проекте десятки, в пределе единицы сотен), так что схлопнуть план до проектного
корня, если понадобится, можно правкой загрузчика.

Каждая операция целиком помещается внутрь одного агрегата — исключений из этого правила ADR не
вводит. Для дефекта №3 это значит следующее.

`UnOccupyRoomType` **становится атомарной даром**: тип принадлежит одной категории, значит все его
группы живут в комнатах одного пула, и выселение — это одна загрузка плана, один проход по группам,
один `SaveChanges`. Та же форма, что у `UnOccupyRoom`, просто строк трогается больше; чтобы получить
сегодняшние N сохранений, мутацию пришлось бы специально звать в цикле.

`UnOccupyAllRooms` пересекает пулы и **остаётся циклом** — по мутации и транзакции на категорию.
Единственной транзакции на проект ради неё не делаем: это стоило бы отдельного входа в props-сервис
по `ProjectIdentification` и того самого исключения «операция над несколькими агрегатами», ради
редкой административной кнопки. Частичное выполнение здесь не страшно, потому что **выселение
идемпотентно**: повторный запуск дочищает остаток, а цикл и сегодня отбирает только заселённые
комнаты (`Where(r => r.Inhabitants.Any())`). Цена решения — мастер может увидеть проект наполовину
выселенным и должен нажать кнопку ещё раз.

### 2. Задел на разделение `AccommodationType` и `RoomCategory`

Планируемое (не в этом ADR и не в ближайших PR) разделение: **категория комнат** — «Люкс» с
комнатами 101, 102; **типы проживания** — «Люкс с пятницы», «Люкс с четверга», «Люкс с пятницы на
одного», каждый со своей ценой, и все трое селятся из общего пула комнат категории «Люкс».

Вместимостей при этом станет две, и они про разное:

| | Чьё свойство | Смысл | Где лежит сегодня |
|---|---|---|---|
| физическая | категория | сколько мест в комнате этой категории | `ProjectAccommodationType.Capacity` |
| продаваемая | тип проживания | скольких мы селим в комнату по этому типу | та же колонка |

Сегодня это одно число в одной колонке, поэтому доменная модель просто читает его дважды: как
вместимость пула и как вместимость типа. В день разделения числа разъедутся, и оба уже будут на
своих местах.

Схема БД и `ProjectInfo` под категории сейчас не меняются. Меняется форма доменной модели — так,
чтобы разделение потом было расширением, а не переделкой. Отсюда четыре решения.

**1. План ключуется категорией, и конвертации id в домене нет.** Заводится
`RoomCategoryIdentification`. Публичной фабрики «id типа → id категории» быть не должно: знание о
том, к какой категории относится тип, — настройка мастера, то есть свойство
`AccommodationTypeInfo.RoomCategoryId` (по критерию ADR015 — там же, где имя и цена). Кому нужен пул
по типу, спрашивает `projectInfo.AccommodationSettings.GetTypeById(typeId).RoomCategoryId`; у кого на
руках категория — конвертировать нечего.

Полностью фикция не исчезает: колонки за `RoomCategoryId` пока нет, поэтому маппер метаданных
заполняет его из `ProjectAccommodationType.Id`, а write-репозиторий кладёт `categoryId.RoomCategoryId`
обратно в существующую колонку `ProjectAccommodation.AccommodationTypeId`. Оба места — в DAL, где
отображение «id ↔ колонка» и так живёт для каждой сущности. Конвертацию нельзя устранить без
миграции, но можно не выносить её в домен публичным API.

Остаточный риск: числа совпадают, поэтому собранный вручную
`new RoomCategoryIdentification(projectId, <число типа>)` сработает молча и сломается в день
разделения. Исчезает не возможность так сделать, а повод. Страховка — тест-страж в
`JoinRpg.Dal.Impl.Test`, фиксирующий сегодняшнее равенство
`type.RoomCategoryId.RoomCategoryId == type.Id.AccommodationTypeId` со ссылкой на этот раздел: в день
разделения он падёт первым и приведёт разработчика сюда, а не в случайное место, где id разъехались.

**2. План держит коллекцию типов, а не один тип.** Сегодня в ней ровно один элемент.

**3. Физическая вместимость — свойство пула, а не комнаты и не типа.** `RoomCategoryPlan.RoomCapacity`
говорит, сколько мест в каждой комнате этого пула; `AccommodationTypeInfo.Capacity` остаётся
продаваемой вместимостью. У `RoomInfo` вместимости нет вовсе: комнаты внутри категории одинаковы —
именно это и делает их взаимозаменяемым пулом.

**4. Группа знает свой тип.** `AccommodationGroupInfo` несёт `AccommodationTypeIdentification` —
сегодня он у всех групп плана одинаковый и выводится из плана, завтра станет существенным.

#### Правило свободного места

Тип, под которым группу поселили, ограничивает комнату целиком, а не одно место: если в «Люкс» (две
койки) въехала группа типа «Люкс на одного», комната занята полностью. Отсюда правило:

```
effective(room)       = min(plan.RoomCapacity, min по типам жильцов)
freeSpace(room, type) = min(effective(room), type.Capacity) − occupancy(room)
```

Сегодня тип в пуле один, а `RoomCapacity` и `type.Capacity` — одно и то же число, поэтому формула
вырождается в нынешнее `type.Capacity − occupancy`: поведение не меняется.

Что это даёт помимо задела:

- **«Сколько всего мест» перестаёт зависеть от типа** — `Rooms.Count * RoomCapacity` по пулу. Сегодня
  `RoomTypeViewModelBase.TotalCapacity` считает `RoomsCount * Capacity`, а
  `AccommodationListViewModel` суммирует это по типам проекта; на общем пуле такая сумма посчитала бы
  одни и те же комнаты дважды.
- **«Комната заполнена» остаётся определённым и после разделения** —
  `occupancy >= effective(room)`. `AccommodationRepositoryImpl.FullyOccupiedRoomsCount` сегодня
  сравнивает занятость с вместимостью типа; после разделения это потеряло бы смысл.

Что при разделении придётся решить дополнительно: как назвать две вместимости в UI мастера, чтобы их
не путали, и как перенести `RoomCategory` в `ProjectInfo` (по критерию ADR015 её место рядом с
`AccommodationTypeInfo` в `AccommodationSettings`).

### 3. Сами типы проживания в агрегат не копируются

`RoomCategoryPlan` держит ссылку на `ProjectInfo` и на `AccommodationTypeInfo` из его
`AccommodationSettings` — ровно как `CharacterInfo` держит `ProjectInfo`, а не пересобирает поля
проекта. Это прямое следствие ADR015: единственный источник правды о типе — метаданные проекта,
и второго не заводим. Вместимость и цена для расчётов берутся оттуда же.

Следствие: инвариант ссылочного равенства из ADR013 действует и здесь — каждый элемент
`Plan.AccommodationTypes` обязан быть тем же экземпляром, что лежит в
`Plan.ProjectInfo.AccommodationSettings`, — и межзапросного кеша у плана нет.

### 4. Заявку на проживание делят два агрегата — и делят по колонкам

`AccommodationRequest` уже принадлежит character-агрегату: `ClaimServiceImpl.SetAccommodationType` и
`LeaveAccommodationGroupAsync` создают и расформировывают группу через
`ICharacterPropsService.ChangeClaim` ([ADR014](adr014-claim-props-service.md)), а существование
типа проверяется по метаданным — `ctx.ProjectInfo.AccommodationSettings`.

Поэтому владение разделяется по колонкам одной строки:

| Что | Кто владеет | Чем меняется |
|---|---|---|
| `AccommodationTypeId`, состав `Subjects`, создание и удаление строки | character-агрегат | `ICharacterPropsService.ChangeClaim` |
| `AccommodationId` — в какой комнате живёт группа | **`RoomCategoryPlan`** | `IAccommodationPropsService` (этот ADR) |
| `IsAccepted` (приглашения) | `AccommodationInviteServiceImpl` | не трогаем, см. §13 |

`RoomCategoryPlan` видит группы как read-only состав (`Subjects` — список `ClaimIdentification`) и
пишет ровно одно поле — номер комнаты. Обратное тоже верно: character-путь не должен менять
`AccommodationId` иначе как через расформирование группы, и сегодня он этого и не делает
(`ConsiderLeavingRoom` только шлёт письмо).

### 5. Деньги в агрегат не входят

Страница комнат показывает по каждому жильцу «не оплачено X из Y». Складывать финансовые поля в
`RoomCategoryPlan` — значит завести третью модель расчёта баланса рядом с `Claim` и
`CharacterClaimInfo`. Вместо этого план несёт `ClaimIdentification` жильцов, а вью-сервис страницы
догружает их bulk'ом через существующий `ICharacterInfoRepository.GetCharacterInfos(ids)` и считает
баланс уже принятым способом — `FinanceExtensions.CalculateClaimBalance(CharacterInfo, CharacterClaimInfo, ProjectInfo)`.
Это один дополнительный запрос на страницу, не N+1. Имена игроков — тем же путём, что в ADR013:
`UserInfoHeader` bulk'ом, не `Include` на каждого.

### 6. Альтернативы и возражения

**Именованные загрузчики на хэндле `IProjectPropsService`** (путь из PR #4843): комнаты в
`ProjectInfo` не попадут, поэтому их пришлось бы догружать через хэндл, как claim-контур догружает
сюжеты (`ctx.LoadDirectPlotsForCharacter`). Отвергнуто: это работает, когда догружаемое — **деталь**
операции над корнем, и разваливается, когда догружаемое **является** предметом операции. `ChangeProjectPropertiesAsync` для переименования комнаты
пересобирал бы `ProjectInfo` и прогревал кеш метаданных на каждое действие с комнатой — при том, что
метаданные не менялись. Плюс правило ADR009 «метаданные меняются только через props-сервис»
превратилось бы в «а ещё через него меняется всё, что удалось догрузить», и граница, проведённая
ADR015, стёрлась бы обратно.

**Всё поселение одним агрегатом, включая типы** — рассмотрено и отвергнуто в ADR015; типы остаются
в `ProjectInfo`.

**Возражение ADR015 про дублирование машинерии.** ADR015 отверг отдельный корень тем, что пришлось
бы дублировать машинерию ADR009: права, активность проекта, логирование, инвалидацию кеша. Для типов
это верно, для комнат — нет. Инвалидация кеша не нужна вовсе: план не меняет метаданные, значит ни
`handle.Refresh()`, ни `PrimeCache` не требуются, — а это и был главный пункт возражения. Второго
кеша не возникает: у `RoomCategoryPlan`, как и у `CharacterInfo`, межзапросного кеша нет. Остаются
права, активность и логирование — про них см. «Последствия».

Подробности
==

### 7. Типизированные идентификаторы комнаты и категории

У комнаты типизированного id нет — добавляется в существующий файл
`src/JoinRpg.DomainTypes/Characters/Claims/Accommodation/AccommodationIdentifications.cs`, рядом с
остальными идентификаторами поселения:

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
/// Пока категория и тип проживания не разделены (ADR018, «Задел на разделение»), своей таблицы у
/// категории нет. Получить категорию по типу проживания можно только через метаданные —
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

### 8. Доменные типы

Файлы: новые, `src/JoinRpg.DomainTypes/Accommodation/`.

```csharp
/// <summary>
/// План поселения одной категории комнат: её комнаты, типы проживания, которые из них селятся,
/// и группы жильцов.
/// Привязан к конкретному экземпляру <see cref="ProjectInfo"/>, кешированию между запросами
/// не подлежит.
/// </summary>
public record class RoomCategoryPlan
{
    public RoomCategoryIdentification Id { get; }
    public ProjectInfo ProjectInfo { get; }

    /// <summary>Типы проживания, селящиеся из этого пула</summary>
    public IReadOnlyCollection<AccommodationTypeInfo> AccommodationTypes { get; }

    /// <summary>
    /// Физическая вместимость комнаты этого пула — комнаты внутри категории одинаковы.
    /// Не путать с <see cref="AccommodationTypeInfo.Capacity"/>: та говорит, скольких мы селим
    /// в комнату по данному типу проживания.
    /// </summary>
    public int RoomCapacity { get; }

    public IReadOnlyCollection<RoomInfo> Rooms { get; }

    /// <summary>Все группы этого пула, включая ещё не расселённые</summary>
    public IReadOnlyCollection<AccommodationGroupInfo> Groups { get; }

    public IEnumerable<AccommodationGroupInfo> UnassignedGroups { get; }

    /// <summary>Сколько всего мест в пуле</summary>
    public int TotalCapacity { get; }

    public RoomInfo GetRoom(AccommodationRoomIdentification roomId);
    public AccommodationGroupInfo GetGroup(AccommodationRequestIdentification groupId);
    public AccommodationTypeInfo GetAccommodationType(AccommodationTypeIdentification typeId);

    /// <summary>
    /// Сколько человек вмещает комната при нынешних жильцах: физический предел, ужатый
    /// вместимостями типов тех, кто уже въехал.
    /// </summary>
    public int GetEffectiveCapacity(AccommodationRoomIdentification roomId);

    public bool IsFull(AccommodationRoomIdentification roomId);

    /// <summary>
    /// Сколько человек ещё можно поселить в комнату под данным типом проживания.
    /// Правило — ADR018, «Правило свободного места».
    /// </summary>
    public int GetFreeSpace(AccommodationRoomIdentification roomId, AccommodationTypeIdentification typeId);
}

/// <summary>
/// Комната. Вместимости у комнаты нет — она общая для пула,
/// см. <see cref="RoomCategoryPlan.RoomCapacity"/>.
/// </summary>
public record class RoomInfo(
    AccommodationRoomIdentification Id,
    string Name,
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
    public int SubjectsCount => Subjects.Count;
}
```

Инварианты — в конструкторе `RoomCategoryPlan`, как это делают `CharacterTypeInfo` и `CharacterInfo`:

- `Id.ProjectId == ProjectInfo.ProjectId`, и все `AccommodationTypes` — те самые экземпляры, что
  лежат в `ProjectInfo.AccommodationSettings` (`ReferenceEquals`);
- у всех комнат и групп `ProjectId` совпадает с `Id.ProjectId`;
- `RoomInfo.Inhabitants ⊆ Groups`, и группа с непустым `RoomId` присутствует ровно в одной комнате;
- `Groups[i].AccommodationTypeId ∈ AccommodationTypes` — группа куплена по типу, селящемуся из
  этого пула.

Чего среди инвариантов сознательно нет:

- «типов ровно один» — сегодня это правда, но проверку пришлось бы снимать при разделении;
- «вместимость не превышена» — сегодняшние данные могли переполниться (вместимость типа уменьшили
  после заселения), и загрузка плана не должна падать на таком проекте. Это проверка операции, а не
  инварианта загрузки.

`GetRoomFreeSpace(ProjectAccommodation)` и `IsOccupied(ProjectAccommodation)` из
`src/JoinRpg.Domain/AccommodationExtensions.cs` заменяются на `RoomCategoryPlan.GetFreeSpace` и
`RoomInfo.IsOccupied` и после миграции удаляются. Свободное место перестаёт быть методом комнаты —
это и есть след, который будущее разделение типа и категории оставляет в сегодняшнем коде. Остальные
методы файла (`GetClaimNeighbours`, `GetRoomFreeSpace(AccommodationRequest)`) обслуживают контур
приглашений и карточку заявки — они вне скоупа и остаются.

### 9. Загрузка

Файл: новый, `src/JoinRpg.Data.Interfaces/Accommodation/IRoomCategoryPlanRepository.cs`.

```csharp
public interface IRoomCategoryPlanRepository
{
    Task<RoomCategoryPlan?> GetPlanOrDefault(RoomCategoryIdentification categoryId);
    Task<IReadOnlyCollection<RoomCategoryPlan>> GetAllPlans(ProjectIdentification projectId);

    /// <summary>
    /// План пула, из которого селится данный тип проживания. Пока тип и категория не разделены,
    /// это тот же план, что <see cref="GetPlanOrDefault"/> по одноимённой категории; после
    /// разделения один план будут возвращать несколько типов.
    /// </summary>
    Task<RoomCategoryPlan?> GetPlanForTypeOrDefault(AccommodationTypeIdentification typeId);
}
```

Реализация — `src/JoinRpg.Dal.Impl/Repositories/Accommodation/RoomCategoryPlanLoader.cs`, по
образцу `CharacterInfoLoader` (ADR014): ядро принимает готовый `ProjectInfo`, чтобы им могли
пользоваться и кеширующий репозиторий, и write-хэндл, и инвариант ссылочного равенства не ломался.
Берёт `MyDbContext` напрямую, **не** наследует `GameRepositoryImplBase` (прогрев контекста всем
проектом — причина TTFB из ADR011). Один EF6-запрос с проекцией в приватные row-типы, группы —
вложенным `Select`, не `Include`. Капканы EF6 те же, что перечислены в ADR013 §5.

### 10. Мутации

Хэндл (в терминах [ADR009](adr009-project-props-service.md) — пара «трекаемая EF-сущность плюс
согласованный с ней доменный снимок», выдаваемая из `IUnitOfWork`) и props-сервис поверх него
повторяют схему ADR009/ADR014.

Внутренний props-сервис (`JoinRpg.Services.Impl/Accommodation/`), один метод:

```csharp
internal interface IAccommodationPropsService
{
    Task<TResult> ChangePlan<TArgs, TResult>(
        RoomCategoryIdentification categoryId,
        Permission requiredPermission,
        ProjectActiveRequirement activeRequirement,
        TArgs arguments,
        Func<RoomCategoryPlanMutationContext<TArgs>, TResult> action,
        [CallerMemberName] string operationName = "");
}
```

Плюс перегрузка без результата. Входа по `ProjectIdentification` **нет**: `UnOccupyAllRooms`
перебирает категории проекта и зовёт `ChangePlan` для каждой (§1).

`RoomCategoryPlanMutationContext` отдаёт: трекаемые EF-сущности комнат и групп, доменный снимок
`RoomCategoryPlan` **до** мутации, делегаты `AddEntity`/`RemoveEntity` (не `DbSet` наружу — как в
ADR014, это делает контекст подделываемым в юнит-тестах) и `AddLegacyEmail` для писем о заселении.

Write-хэндл `IRoomCategoryPlanWriteRepository` берётся **только** из
`IUnitOfWork.GetRoomCategoryPlanWriteRepository()`, а не из DI: `MyDbContext` зарегистрирован
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

    Task UnOccupyGroup(AccommodationRequestIdentification groupId);
    Task UnOccupyRoom(AccommodationRoomIdentification roomId);

    /// <summary>
    /// Выселить все группы данного типа проживания. Сегодня это всё население пула; после
    /// разделения типа и категории соседи из братских типов останутся в комнатах.
    /// </summary>
    Task UnOccupyRoomType(AccommodationTypeIdentification typeId);

    Task UnOccupyAllRooms(ProjectIdentification projectId);
}
```

`GetRoomTypeAsync` уходит: его единственный вызывающий — страница комнат (`RoomTypeDetails`), которая
переезжает на `IRoomCategoryPlanRepository`. Классы-запросы `OccupyRequest`, `UnOccupyRequest`,
`UnOccupyAllRequest`, `UnOccupyRoomTypeRequest` (mutable, с `int`-полями, один из них вообще без
вызывающих) удаляются.

`UnOccupyGroup` принимает id группы, а пул в нём не закодирован — хэндл определяет корень одним
лёгким запросом «какого типа эта группа» перед загрузкой плана.

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

### 11. Письма

`OccupyRoomEmail`/`UnOccupyRoomEmail` (`RoomEmailBase`) несут EF-сущности `ProjectAccommodation Room`
и `Claim[] Changed`, а `EmailServiceImpl` зовёт по ним `GetAllInhabitants()`. Переписывать почтовый
контур этот ADR не берётся: письма отправляются через `ctx.AddLegacyEmail(...)` — тот же механизм,
которым ADR014 сохранил легаси-письма заявок. Единственное изменение по существу: письмо уходит
**после** успешного `SaveChanges`, один раз на операцию, а не по письму на комнату внутри цикла.

### 12. Потребители

| Потребитель | Сейчас | Станет |
|---|---|---|
| `AccommodationTypeController.RoomTypeDetails` (был `EditRoomTypeRooms`) | `accommodationService.GetRoomTypeAsync` + ручная сверка `ProjectId` | `IRoomCategoryPlanRepository.GetPlanForTypeOrDefault` |
| `RoomTypeViewModel(ProjectAccommodationType, …)` | EF-сущность с `Include(ProjectAccommodations)` и `Include(Desirous)` — навигация «заявки на проживание, желающие этот тип» | `RoomCategoryPlan`; EF-конструктор удаляется, остаётся уже существующий поверх `AccommodationTypeInfo` (ADR015) |
| `RoomViewModel(ProjectAccommodation, …)` | EF-сущность | `RoomInfo` |
| `AccRequestViewModel(AccommodationRequest, …)` + `RequestParticipantViewModel(Claim, …)` | EF-сущности, `Claim.ClaimTotalFee`/`ClaimFeeDue` (оба `[Obsolete]`) | `AccommodationGroupInfo` + bulk `CharacterInfo`, `CalculateClaimBalance` поверх агрегата |
| `AccommodationTypeController` (Occupy/UnOccupy/AddRoom/EditRoom/DeleteRoom) | `int`-параметры, `catch`-всё | типизированные id, доменные исключения |
| `SmokeProjectFixture` (интеграционные тесты) | `AddRooms(projectId.Value, roomTypeId.AccommodationTypeId, "1,2")` | `AddRooms(projectInfo.AccommodationSettings.GetTypeById(roomTypeId).RoomCategoryId, "1,2")` |
| `AccommodationPrintController`, `AccomodationReportExporter` | `IAccommodationRepository.GetClaimAccommodationReport` | не меняются — это отчёт, плоские строки, агрегат ему не нужен |
| `AccommodationInviteServiceImpl`, `ClaimAccommodationViewModel` | `AccommodationExtensions` поверх EF | не меняются, см. §13 |

`IAccommodationRepository.GetRoomTypesForProject` (строки со счётчиками занятости для страницы
«Типы проживания») остаётся: это сводка по всему проекту, и гонять ради неё полный план каждого
типа незачем.

### 13. Что вне скоупа

- **Любые изменения схемы БД.** Ни новых таблиц, ни колонок, ни миграций; `db-schema.md` не
  меняется.
- **Приглашения** (`AccommodationInvite`, `AccommodationInviteServiceImpl`) и **выбор типа игроком**
  (`SetAccommodationType`, `LeaveAccommodationGroupAsync`). Они уже живут в character-контуре
  (ADR014) либо ждут собственной миграции; здесь меняется только колонка «в какой комнате».
- **Почтовые модели** `RoomEmailBase` и `EmailServiceImpl` — остаются на EF-сущностях.
- **Автозаселение** (`OccupyAll` с `//TODO: Implement mass occupation`) — не реализовано сегодня,
  не реализуется и здесь. Агрегат делает его дешевле: вместимость и свободные группы уже собраны.
- **Само разделение `AccommodationType` и `RoomCategory`** — здесь только форма доменной модели под
  него: таблицы, миграции, UI категорий и перенос `RoomCategory` в `ProjectInfo` — отдельная работа
  и, вероятно, отдельный ADR.

### 14. План миграции

0. **PR 0** — этот ADR и ссылка на него в [docs/README.md](README.md).
1. **PR 1** — ✅ сделано. `AccommodationRoomIdentification`, `RoomCategoryIdentification`,
   `AccommodationTypeInfo.RoomCategoryId` с маппингом; `RoomCategoryPlan`, `RoomInfo`,
   `AccommodationGroupInfo` с инвариантами; юнит-тесты в `JoinRpg.DomainTypes.Test` — в том числе
   тест на план с двумя типами в одном пуле и разной вместимостью, который сегодня не собирается из
   БД, но обязан собираться из модели. Потребителей не трогали.

   Четыре уточнения по факту реализации:

   - Юнит-тесты плана живут не на `MockedProject` (он в `JoinRpg.DataModel.Mocks`, а
     `JoinRpg.DomainTypes.Test` на DAL не ссылается), а на тамошнем `ProjectInfoFixture`, куда
     добавлен `MakeAccommodationType`. На `MockedProject` остался тест-страж — он и должен быть
     в `JoinRpg.Dal.Impl.Test`, потому что проверяет маппер.
   - Промахи `GetRoom`/`GetGroup`/`GetAccommodationType` кидают доменные исключения —
     `AccommodationRoomNotFoundException`, `AccommodationGroupNotFoundException` и
     `AccommodationTypeNotFoundException` (последнее заведено #5088, два первых — этим PR). Все три
     наследуют `JoinRpgProjectException`. `JoinRpgEntityNotFoundException` не годится: он лежит в
     `JoinRpg.Data.Interfaces`, куда домен не смотрит.
   - `GetFreeSpace` не бывает отрицательным: комната могла оказаться переполненной (вместимость
     типа уменьшили после заселения — тот самый случай, ради которого «вместимость не превышена»
     сознательно не инвариант), и результат в этом случае ноль, а не минус.
   - `RoomCategoryIdentification` и `AccommodationRoomIdentification` дописаны в
     `ProjectEntityIdParser.TryParseId`: полиморфный разбор идентификаторов покрыт общим тестом
     `IdentificationCommonTest`, и без регистрации новые id его не проходят.
2. **PR 2** — ✅ сделано. `IRoomCategoryPlanRepository` + загрузчик + тесты маппинга в
   `JoinRpg.Dal.Impl.Test`, включая тест-страж: `plan.GetFreeSpace(room, type)` совпадает с
   `AccommodationExtensions.GetRoomFreeSpace` на том же наборе данных. Потребителей не трогали.

   Четыре уточнения по факту реализации:

   - Загрузчик разложен на те же четыре файла, что и `CharacterInfo` (ADR013):
     `RoomCategoryPlanRows` (проекция), `RoomCategoryPlanMapper` (чистое преобразование),
     `RoomCategoryPlanLoader` (запрос, принимает `ProjectInfo` параметром),
     `RoomCategoryPlanRepository` (берёт `ProjectInfo` из `IProjectMetadataRepository`).
     Мапперу отдельный файл нужен ровно затем, зачем он нужен `CharacterInfoMapper`:
     чтобы тесты маппинга шли без базы.
   - Запрос строится **от таблицы типов проживания**: своей таблицы у категории нет, поэтому
     ряд `ProjectAccommodationType` играет роль ряда категории, а комнаты и группы берутся из
     его навигаций `ProjectAccommodations` и `Desirous` вложенными `Select`. Это даёт один
     запрос на план (и один на все планы проекта) и естественный `null` для несуществующей
     категории. Конвертация «id категории ↔ колонка id типа» живёт только в загрузчике и
     помечена комментарием.
   - `RoomCapacity` читается из `ProjectAccommodationType.Capacity` тем же запросом, а не из
     `AccommodationTypeInfo.Capacity` метаданных: числу и так предстоит разъехаться надвое
     (§2, пункт 3), и пусть у физической вместимости с самого начала будет своё чтение.
     Типы проживания при этом, наоборот, **не** читаются из БД вовсе — они берутся из
     `ProjectInfo.AccommodationSettings` по `RoomCategoryId`, иначе сломался бы инвариант
     ссылочного равенства (§3).
   - `JoinRpg.Dal.Impl.Test` получил ссылку на `JoinRpg.Domain` — только ради тест-стража,
     которому нужен legacy `AccommodationExtensions`. Ссылка уйдёт вместе с ним в PR 6.
     Тест-страж сравнивает не буквально: legacy умеет уходить в минус на переполненной
     комнате, а доменный `GetFreeSpace` — нет (см. уточнение к PR 1), поэтому сверка идёт
     с `Math.Max(0, …)`.
3. **PR 3** — ✅ сделано. Страница комнат на план: экшен страницы комнат, вью-модели, деньги bulk'ом
   через `ICharacterInfoRepository`. `GetRoomTypeAsync` удалён из `IAccommodationService` и
   `AccommodationServiceImpl`.

   Пять уточнений по факту реализации:

   - Загрузка страницы вынесена из контроллера в `RoomTypeRoomsViewService`
     (`JoinRpg.WebPortal.Managers/Accommodation/`) — по structure.md контроллер только зовёт
     вью-сервис. Промах по типу проживания даёт `NotFound`, как у соседних экшенов, а не
     `Forbid` (§12).
   - `ICharacterInfoRepository.GetCharacterInfos` берёт `CharacterIdentification`, а план несёт
     жильцов как `ClaimIdentification` (§5) — прямого соответствия нет. Чтобы не делать два
     запроса («сначала персонажи по заявкам, потом сами персонажи»), заведён
     `GetCharacterInfosByClaims(IReadOnlyCollection<ClaimIdentification>)` — тот же загрузчик
     `CharacterInfo` с предикатом по заявкам. Запрос на страницу остаётся один, как и обещано
     в §5. Имена игроков отдельного запроса не требуют вовсе: `CharacterClaimInfo.Player` —
     это `UserInfoHeader`, он уже внутри агрегата (ADR013).
   - `RequestParticipantViewModel` перестал носить EF-сущности `Claim` и `User`: вместо
     `Claim.ProjectId` шаблон отображения берёт `ClaimId.ProjectId`. `[Obsolete]`-методов
     `ClaimTotalFee`/`ClaimFeeDue` на этой странице больше нет — баланс считает
     `FinanceExtensions.CalculateClaimBalance(CharacterInfo, CharacterClaimInfo, ProjectInfo)`.
   - Экшен страницы переименован из `EditRoomTypeRooms` в `RoomTypeDetails` (по ревью): имя
     не совпадало ни с маршрутом `~/{projectId}/rooms/{roomTypeId}/details`, ни с сутью —
     страница не «редактирует тип», а показывает комнаты и жильцов. Вместе с экшеном
     переименована вьюха (`Views/AccommodationType/RoomTypeDetails.cshtml`), поправлены
     `RedirectToAction` и обе ссылки `Url.Action`. **Маршрут не менялся** — это внешний URL.
   - Интеграционный тест на страницу есть и был до этого PR:
     `AccommodationPagesLazyLoadsScenario.RoomTypeDetails_ListsInhabitants` — ходит мастером на
     `{projectId}/rooms/{roomTypeId}/details`, ждёт 200 и проверяет, что на странице видны все
     жильцы из `SmokeProjectFixture`. Этим PR он дополнен проверкой, что отрисованы и сами
     комнаты (строки с атрибутом `roomId` совпадают с `SmokeProjectFixture.RoomNames`) — иначе
     тест прошёл бы и на странице, собранной мимо плана. Кроме него маршрут меряет
     `AllGetPagesSmokeScenario` и снапшот ленивых загрузок `lazy-loads-baseline.json`.
4. **PR 4.** Write-хэндл, `IAccommodationPropsService`, вынос `ProjectOperationGuard` (если
   наберётся), перевод `AddRooms`/`RenameRoom`/`DeleteRoom` — права, активность, типизированный
   проект. Закрывает дефекты 1, 2, 4 для управления комнатами.
5. **PR 5.** Перевод `OccupyRoom`/`UnOccupy*` — один `SaveChanges` на операцию, проверка пула,
   явный подсчёт мест, письма через `AddLegacyEmail`. Закрывает дефекты 5, 6, 7 и половину
   третьего — `UnOccupyRoomType`.
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
- **Выселение типа становится атомарным**, выселение всего проекта — нет: оно остаётся циклом по
  категориям, по транзакции на каждую. Осознанный размен: правило «операция не выходит за границу
  агрегата» остаётся без исключений, а страховкой служит идемпотентность выселения.
- **Props-сервисов становится три** (проект, персонаж, поселение). Цикл «право → активность →
  мутация → сохранение → лог» повторяется в третий раз, и его пора вынести в общий
  `ProjectOperationGuard` — проверка права с admin-bypass, `ProjectActiveRequirement`, структурный
  лог операции с аргументами (`Information` при успехе, `Warning` при провале).
- **Правило владения `AccommodationRequest` зафиксировано по колонкам.** Граница неизящная, её
  придётся держать в голове при следующей правке claim-контура — зато она явная, а не выведенная из
  того, кто первым дотянулся до сущности.
- **Модель готова к разделению типа и категории раньше, чем БД** — ценой лишнего идентификатора,
  коллекции из одного элемента и одного числа, прочитанного из колонки дважды.
- **Переходный период**: пока PR 3–6 не влиты, план сосуществует со старым `IAccommodationService`.

Открытые вопросы
==

- ~~Сколько комнат на крупном проекте~~ — **закрыт**: десятки, в пределе единицы сотен
  (@leotsarev) — объём, при котором ни загрузка плана, ни выселение категории одной транзакцией не
  создают проблемы.
- ~~Правило свободного места в комнате со смешанными типами~~ — **закрыт** (@leotsarev): тип
  занимает комнату целиком, «Люкс на одного» делает «Люкс» полным. Формула — в §2.
- ~~Заводить ли таблицу `RoomCategory` сразу~~ — **закрыт: не заводим** (@leotsarev); обоснование и
  остаточный риск — в §2.
- **`ProjectOperationGuard`** — выносить или нет, решается фактом при реализации PR 4.

Статус
==

Принят. Issue #5037. Ход реализации отмечается в §14.
