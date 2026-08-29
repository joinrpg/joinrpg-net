# ADR013: CharacterInfo — доменный агрегат персонажа

Проблема:
==

Данные о персонаже размазаны по нескольким несовместимым read-моделям, и каждая из них течёт
EF-сущностями наружу:

- `UgDto` / `UgClaim` (`src/JoinRpg.Data.Interfaces/IUnifiedGridRepository.cs`) — DTO объединённой
  сетки заявок, внутри которого лежит полная EF-сущность `Claim`;
- `CharacterView` (`src/JoinRpg.Data.Interfaces/ICharacterRepository.cs`) — mutable-класс с
  `required`-сеттерами и полем `Claim? ApprovedClaim`;
- `CharacterItem(Character, ParentGroups)` (`src/JoinRpg.Domain/CharacterBulkLoader.cs`) — обёртка,
  заведённая ради фильтров проблем, с кешем по `CharacterId` без инвалидации;
- сама сущность `Character` плюс ~10 методов-расширений в `JoinRpg.Domain`
  (`CharacterParentGroupExtensions`, `CustomFieldsExtensions`, `ClaimAcceptOrMoveValidationExtensions`,
  `BusyStatusExtensions`, …).

Это ровно тот legacy-паттерн, от которого уходит [structure.md](structure.md): «вместо использования
сущностей БД с методами расширения в доменной логике надо использовать доменные объекты».

Практические следствия видны в [ADR011](adr011-roles-grid-payload.md): сетка ролей грузит
`Player.Extra` + `ExternalLogins` через `Include` на каждого персонажа, а
`ProjectRepository.GetCharacterByGroups` прогревает контекст всем проектом целиком
(`LoadProjectFields`, `LoadProjectClaimsAndComments`, `LoadMasters`) независимо от запрошенной
группы — отсюда TTFB 14,5 с.

Решение:
==

Завести один immutable доменный тип **`CharacterInfo`** — аналог `ProjectInfo`, но для персонажа,
и загрузчик к нему.

Тип должен быть достаточен, чтобы поверх него:

- считать проблемы персонажа,
- рисовать сетку ролей и гриды персонажей,
- заменить `UgDto`,
- решать «можно ли подать заявку на этого персонажа».

Полные данные о комментариях в заявках и о сюжетах в него **не входят**.

Ключевые решения:

- **`CharacterInfo` user-independent, без `AccessArguments` внутри.** Он несёт полную правду о
  персонаже (все поля всех слоёв, все заявки). Фильтрация по доступу — снаружи, через метод
  `GetFieldLayers(AccessArguments access, ClaimIdentification? claimId = null)`, отдающий готовый
  существующий `CharacterFieldLayers`. Так тип остаётся кешируемым и легко тестируемым — как
  `ProjectInfo`.
- **`CharacterInfo` держит ссылку на `ProjectInfo`.** Это бесплатно (`FieldLayerContainer` её уже
  держит), и без неё половина производных свойств невозможна.
- **Собственный репозиторий `ICharacterInfoRepository`**, а не расширение `ICharacterRepository`.
  [project-entities.md](project-entities.md) прямо запрещает класть персонажей и заявки в
  `ProjectInfo`, значит нужен отдельный загрузчик.
- **Потребители мигрируются отдельными PR.** Первый PR — только тип, загрузчик и тесты.

Подробности
==

### 1. Состав `CharacterInfo`

Файл: новый, `src/JoinRpg.DomainTypes/Characters/CharacterInfo.cs`. Паттерн — как `ProjectInfo`:
`record class` с явным конструктором и get-only свойствами, `Lazy<>` для производных.

```csharp
public record class CharacterInfo
{
    // ключи и контекст
    public CharacterIdentification Id { get; }
    public ProjectInfo ProjectInfo { get; }

    // настройки персонажа
    public string CharacterName { get; }
    public CharacterTypeInfo CharacterTypeInfo { get; }
    public bool HidePlayerForCharacter { get; }
    public bool IsActive { get; }
    public bool InGame { get; }
    public bool AutoCreated { get; }
    public MarkdownString Description { get; }
    public CharacterIdentification? OriginalCharacterSlotId { get; }

    // структура
    public IReadOnlyCollection<CharacterGroupIdentification> DirectGroupIds { get; }

    // поля
    public FieldLayerContainer CharacterFields { get; }

    // заявки
    public IReadOnlyCollection<CharacterClaimInfo> Claims { get; }
    public ClaimIdentification? ApprovedClaimId { get; }

    // аудит
    public DateTime CreatedAt { get; }  public UserIdentification CreatedById { get; }
    public DateTime UpdatedAt { get; }  public UserIdentification UpdatedById { get; }
}
```

**Почему `HidePlayerForCharacter` хранится отдельно, хотя есть `CharacterTypeInfo`.**
`ToCharacterTypeInfo()` (`src/JoinRpg.DataModel/Extensions/CharacterExtensions.cs`) схлопывает
`(IsPublic = false, HidePlayerForCharacter = любой) → CharacterVisibility.Private` — исходную пару
из `CharacterVisibility` не восстановить. А `ProjectRoleGridViewModelBuilder` читает флаг напрямую.
Раз тип — «сырая правда», нужны оба представления.

### 2. `CharacterClaimInfo`

Файл: новый, `src/JoinRpg.DomainTypes/Characters/Claims/CharacterClaimInfo.cs` (namespace
`JoinRpg.DomainTypes.Characters.Claims` — рядом с `ClaimStatus` и `ClaimIdentification`).

```csharp
public record class CharacterClaimInfo(
    ClaimIdentification ClaimId,
    UserInfoHeader Player,     // PlayerId остался производным свойством, см. уточнение в п.4
    ClaimStatus Status,
    ClaimDenialReason? DenialStatus,
    UserIdentification ResponsibleMasterId,
    DateTime CreateDate,
    DateTime LastUpdateDateTime,
    DateTime? CheckInDate,
    DateTimeOffset? LastPlayerCommentAt,
    DateTimeOffset? LastMasterCommentAt,
    DateTimeOffset? LastVisibleMasterCommentAt,
    int? CurrentFee,
    bool PreferentialFeeUser,
    int FeePaid,
    int AccommodationFee,
    FieldLayerContainer Fields)
{
    public bool IsApproved => Status is ClaimStatus.Approved or ClaimStatus.CheckedIn;
    public bool IsActive => Status.IsActive();
    public bool IsInDiscussion => Status is ClaimStatus.AddedByMaster or ClaimStatus.AddedByUser or ClaimStatus.Discussed;
    public bool IsPending => Status is not (ClaimStatus.DeclinedByMaster or ClaimStatus.DeclinedByUser);
}
```

Обоснование нетривиальных полей:

- **три скаляра `Last*CommentAt`** — ровно то, что читает `ClaimListBuilder.GetLastCommentTime`.
  Это не «полные данные о комментариях»: сущностей `Comment` / `CommentDiscussion` здесь нет.
  `LastMasterCommentAt` (невидимый игроку) и `LastVisibleMasterCommentAt` различаются, а выбор между
  ними делается по `AccessArguments` снаружи — поэтому нужны оба.
- **`CurrentFee`, `PreferentialFeeUser`, `FeePaid`, `AccommodationFee`, `Fields`** — полный вход
  `FinanceExtensions.CalculateClaimBalance` без обращения к EF. Список `FinanceOperation` не тащим:
  нужна только сумма approved-операций, ровно как это уже делает `UgClaim.FeePaid`.
- **`DenialStatus`** — вход `ClaimStatusBuilders.CreateFullStatus`; фильтруется снаружи по
  `AccessArguments.CanViewDenialStatus`.

**Слой полей (`Fields`) грузим у каждой заявки, а не только у утверждённой.** Иначе
`GetFieldLayers(access, claimId)` для заявки в обсуждении невозможен, а `ClaimFieldsFee` для
неутверждённых заявок посчитается неверно. Стоимость нулевая: `Claim.JsonData` лежит в той же
строке, которую мы и так селектим, — новых join'ов не добавляется.

### 3. Производные свойства и инварианты

```csharp
public bool IsPublic => CharacterTypeInfo.IsPublic;

public CharacterClaimInfo? ApprovedClaim { get; }        // резолвится в конструкторе
public IEnumerable<CharacterClaimInfo> ActiveClaims => Claims.Where(c => c.IsActive);
public int ActiveClaimsCount { get; }                    // Lazy
public bool HasActiveClaims => ActiveClaimsCount > 0;

public IReadOnlyCollection<CharacterGroupIdentification> ParentGroupIdsToTop { get; }  // Lazy
public IEnumerable<CharacterGroupInfo> ParentGroupsToTop { get; }
public IEnumerable<CharacterGroupInfo> DirectGroups => ProjectInfo.GetGroupsById(DirectGroupIds);
public IEnumerable<CharacterGroupInfo> IntrestingGroupsForDisplay { get; }

public UserIdentification ResponsibleMasterId { get; }
public CharacterClaimInfo GetClaimById(ClaimIdentification claimId);
public CharacterFieldLayers GetFieldLayers(AccessArguments access, ClaimIdentification? claimId = null);
```

`GetFieldLayers` — центральный метод, будущая замена трёх фабрик
`src/JoinRpg.Domain/CharacterFields/CharacterFieldLayersBuilder.cs`:

- `claimId == null` → `ClaimLayer = ApprovedClaim?.Fields` (поведение `FromCharacter` / `FromCharacterView`);
- `claimId != null` → `ClaimLayer = GetClaimById(claimId).Fields` (поведение `FromClaim`);
- `CharacterLayer = CharacterFields`, `AccessArguments = access`;
- `KeyNotFoundException`, если такой заявки нет среди `Claims`.

Возвращается **существующий** `CharacterFieldLayers` — новый тип не заводим.

Инварианты проверяются в конструкторе (как `CharacterTypeInfo` уже бросает на «NPC не может быть
горячим»):

- `Id.ProjectId == ProjectInfo.ProjectId`;
- у всех заявок `CharacterId` и `ProjectId` совпадают с `Id`;
- `ApprovedClaimId`, если задан, присутствует среди `Claims` и у него `IsApproved`;
- не более одной утверждённой заявки;
- `CharacterFields.ProjectInfo` и `Claims[i].Fields.ProjectInfo` — тот же **экземпляр**, что
  `ProjectInfo` (`ReferenceEquals`), иначе `ArgumentException`.

Последний инвариант означает, что **`CharacterInfo` привязан к конкретному экземпляру `ProjectInfo`**,
и кешировать его между запросами без синхронной инвалидации обоих нельзя. Это зафиксировано в
XML-doc типа.

### 4. Что НЕ включаем

| Не включаем | Почему |
|---|---|
| `CommentDiscussion`, `Comment[]` | отдельный агрегат; в `CharacterClaimInfo` есть только три скаляра «когда был последний комментарий» |
| Сюжеты (`PlotElement`, `PlotElementOrderData`) | отдельный агрегат, есть `CharacterPlotViewService` и `PlotAccessArguments` |
| `FinanceOperation[]`, `RecurrentPayment[]` | нужна только сумма `FeePaid` |
| `AccommodationRequest` целиком | нужна только `Cost` |
| `UserSubscription[]` | своя ручка `IUserSubscribeRepository` |
| Контакты, аватар и соцсети игрока (`UserExtra`, `ExternalLogins`) | только `UserInfoHeader` — id и отображаемое имя (см. уточнение ниже). Остальной профиль меняется независимо от персонажа, а его включение раздуло бы агрегат — см. ADR011, где контакты составляют заметную долю payload сетки. Отображение — bulk через `IUserRepository.GetUserInfoHeaders(ids)` |
| `AccessArguments` | тип user-independent (см. «Решение») |
| Заявки игрока в других персонажах | `AddClaimForbideReason.OnlyOneCharacter` требует данных по всему проекту — приходят параметром в `UserInfo` |

Вне `DomainTypes` остаются: `GetBusyStatus` (возвращает UI-enum), `ValidateIfCanAddClaim` (нужен
`UserInfo`), `CalculateClaimBalance`, фильтры проблем.

#### Уточнение: отображаемое имя игрока входит в агрегат

Изначально в `CharacterClaimInfo` лежал только `UserIdentification PlayerId`. Практика показала,
что этого мало: когда в проекте не настроено поле-имя, **имя персонажа записывается по имени
игрока** (`SaveToCharacterAndClaimStrategy`), то есть отображаемое имя нужно не для показа, а для
доменной операции. Протаскивать его мимо агрегата отдельным параметром — значит завести ещё один
канал данных о персонаже помимо `CharacterInfo`, ровно от чего этот ADR и уходит.

Поэтому `PlayerId` заменён на `UserInfoHeader Player` (сам `PlayerId` остался производным
свойством, так что потребители не изменились). Цена — один join к `User` за пять колонок имени
и email в проекции загрузчика, без N+1. Граница остаётся прежней: **имя — да, контакты,
телефон, соцсети и аватар — нет.**

### 5. Загрузчик

Файл: новый, `src/JoinRpg.Data.Interfaces/Characters/ICharacterInfoRepository.cs`.

```csharp
public interface ICharacterInfoRepository
{
    Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId);
    Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(IReadOnlyCollection<CharacterIdentification> characterIds);
    Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(ProjectIdentification projectId, IReadOnlyCollection<CharacterGroupIdentification> groupIds);
    Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId);
}
```

**Почему не расширяем `ICharacterRepository`**: тот интерфейс — legacy (`IDisposable`, 13 методов,
все отдают EF-сущности `Character`, часть помечена `[Obsolete]`), а его реализация наследует
`GameRepositoryImplBase` с прогревом контекста всем проектом. Смешивание утащило бы прогрев в новый
код и помешало потом удалить старое одним куском.

Спецификации выборки заявок в первой версии нет — грузим все заявки всегда: тип несёт полную правду,
фильтрация заявок ломала бы инвариант `ApprovedClaimId ∈ Claims`, а заявок на персонажа единицы.
Фильтры `UgStatusSpec` появятся в PR миграции объединённой сетки; предикаты
`CharacterPredicates.ByUgStatus` / `ClaimPredicates.ByUgStatus` для этого уже есть.

Реализация: `src/JoinRpg.Dal.Impl/Repositories/CharacterInfoRepository.cs`, берёт `MyDbContext`
напрямую и **не** наследуется от `GameRepositoryImplBase` (см. ADR011). Образец — `UnifiedGridRepository`:
один EF6-запрос с проекцией в приватные row-типы, заявки — вложенным `Select` (не `Include`), затем
чистый маппинг `CharacterInfoMapper.Map(row, projectInfo)`. `ProjectInfo` берётся из
`IProjectMetadataRepository` (кеш).

Капканы EF6, у каждого есть прецедент в коде:

- `Sum` по пустой коллекции требует каста `(int?)` перед `Sum` (`UnifiedGridRepository.cs:46`), иначе
  `InvalidOperationException` при материализации;
- nullable-навигации (`OriginalCharacterSlot`, `AccommodationRequest`) — каст `(int?)` даёт LEFT JOIN
  без NRE (`UnifiedGridRepository.cs:38`);
- `ParentGroupsImpl` (`IntList`) и `Description` (`MarkdownDbValue`) — `[ComplexType]`, проецируются
  целиком;
- row-типы — `internal sealed class` с `init`-свойствами, вложенная коллекция объявляется как
  `IEnumerable<>`: позиционные record'ы в проекции EF6 не умеет;
- никаких `IAsyncEnumerable` — только `await query.ToListAsync()`.

Кеш-декоратора в Portal не заводим: данные персонажа мутируют при любом действии с заявкой, а
`PerRequestCache` выигрывает только при повторном чтении того же персонажа в одном запросе.

### 6. Достаточность модели

Таблица — доказательство, что тип закроет потребителей при последующей миграции.

| Потребитель | Чем закрывается | Дырки |
|---|---|---|
| `UgDto` | `CharacterTypeInfo`, `CharacterName`, `ApprovedClaim?.PlayerId`, `IsActive`, `HasActiveClaims`, `Id`, `Claims` | нет |
| `UgClaim(Claim, FeePaid)` | `CharacterClaimInfo` целиком | нет |
| `AccessArgumentsFactory.Create(UgDto, …)` | `ApprovedClaim?.PlayerId`, `IsPublic` | нет |
| `BusyStatusExtensions.GetBusyStatus` ×3 | `CharacterTypeInfo`, `ApprovedClaimId is not null`, `HasActiveClaims` | нет |
| `UnifiedGrid/ItemBuilder` | `PlayerId`, `Status` / `DenialStatus`, `CreateDate`, `CheckInDate`, `ResponsibleMasterId`, финансы, `Last*CommentAt` ×3, `Player` | нет |
| `FinanceExtensions.CalculateClaimBalance` | `FeePaid`, `CurrentFee`, `PreferentialFeeUser`, `Fields`, `AccommodationFee`, `ProjectInfo.ProjectFinanceSettings` | **была дырка**: расписание взносов в `ProjectFinanceSettings` отсутствовало, добавлено (см. «Статус»). Попутно уходит мутирующий кеш `Claim.FieldsFee` |
| `CharacterView` | `Id`, `UpdatedAt`, `IsActive`, `IsPublic`, `InGame`, `CharacterTypeInfo`, `ApprovedClaim`, `Claims`, `DirectGroups`, `CharacterFields`, `CharacterName`, `Description` | `GroupHeader.ParentGroupIds` — уже в `ProjectInfo.Groups` |
| `ProjectRoleGridViewModelBuilder` | всё выше + `HidePlayerForCharacter`, `ActiveClaimsCount`, `IntrestingGroupsForDisplay`, `GetFieldLayers` | контакты игрока — bulk (это улучшает ADR011) |
| `CharacterListItemViewModel` | то же + `ResponsibleMasterId`, `ParentGroupsToTop` | `User` → `UserInfoHeader` bulk |
| `BrokenCharactersFilter` | `ParentGroupsToTop` (те же `CharacterGroupInfo`) | нет |
| `FieldNotSetFilter` / `InActiveVariantsFilter` | `CharacterTypeInfo.CharacterType` + `ParentGroupIdsToTop`: `CharacterInfo` **сам является** готовым `CharacterItem`, обёртка и `CharacterBulkLoader` станут не нужны | нет |
| `ProblemValidator.GetFields` | `GetFieldLayers(access).GetAllFieldsForEdit()` + `Where(BoundTo == Character \|\| ApprovedClaimId is not null)` | нет |
| `ValidateIfCanAddClaim` | `ProjectInfo.ProjectStatus`, `ApprovedClaimId`, `IsActive`, `CharacterTypeInfo`, `Claims.Any(c => c.PlayerId == u && c.IsActive)` | `OnlyOneCharacter` и контакты приходят в `UserInfo` (сознательно) |
| `CharacterHeader` / XGameApi | `Id`, `UpdatedAt`, `IsActive` | нет |

Единственный систематический пробел — отображаемые данные пользователей (имя, контакты, аватар).
Это осознанное решение, см. таблицу «что не включаем».

### 7. Порядок работ

0. Этот ADR.
1. Перенести `ClaimStatus.IsActive()` из `src/JoinRpg.DataModel/ClaimExtensionsTemp.cs` (у файла
   собственный TODO «Move this down and merge with predicates») в
   `src/JoinRpg.DomainTypes/Characters/Claims/`. Иначе `CharacterClaimInfo.IsActive` станет
   четвёртой копией правила. Обе версии одновременно оставлять нельзя — CS0121.
2. Вынести чистое ядро маппинга `(IsPublic, HidePlayerForCharacter) → CharacterVisibility` из
   `CharacterExtensions.ToCharacterTypeInfo` в `DomainTypes` и звать из обоих мест, чтобы маппер
   репозитория не завёл вторую копию.
3. `CharacterClaimInfo` + `CharacterInfo`.
4. Тесты в `JoinRpg.DomainTypes.Test` (предварительно вынести `MakeProject` / `MakeField` из
   `FieldLayerContainerTest` в общую фикстуру и дополнить её словарём `Groups`).
5. `ICharacterInfoRepository`.
6. Row-типы, `CharacterInfoMapper`, `CharacterInfoRepository`.
7. Тесты маппера в `JoinRpg.Dal.Impl.Tests` — включая два теста-стража: маппинг visibility совпадает
   с `ToCharacterTypeInfo`, а `CharacterClaimInfo.IsActive` совпадает с
   `ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active)`.
8. Регистрация в `Registraton.cs`.

Шаги 1–2 — подготовительные, отдельными коммитами.

Последствия
==

- **Единый вход в данные персонажа.** `UgDto`, `CharacterView`, `CharacterItem` и прямое
  использование `Character` в чтении становятся излишними и удаляются по мере миграции.
- **Загрузка без прогрева проекта.** Новый репозиторий не наследует `GameRepositoryImplBase`, что
  снимает основную причину TTFB из ADR011.
- **Контакты игроков грузятся bulk'ом**, а не `Include`-ом на каждого персонажа — это уменьшает и
  запрос, и payload сетки ролей.
- **`CharacterBulkLoader` и его кеш без инвалидации уйдут** вместе с миграцией фильтров проблем.
- **Ограничение**: `CharacterInfo` привязан к экземпляру `ProjectInfo`, поэтому межзапросное
  кеширование потребует синхронной инвалидации обоих. Пока кеша нет вовсе.
- **Переходный период**: пока потребители не мигрированы, старые read-модели сосуществуют с новым
  типом.

Статус
==

Принят.

Подготовительные шаги 1–2 сделаны и влиты в `master` (#4581): `ClaimStatus.IsActive()` живёт в
`JoinRpg.DomainTypes.Characters.Claims.ClaimStatusExtensions`, маппинг флагов персонажа — в
`CharacterTypeInfo.Create` / `CharacterTypeInfo.GetVisibility`. Фабрика сделана статическими
методами на самом `CharacterTypeInfo`, а не отдельным классом `CharacterTypeInfoFactory`, как
предполагалось в п. 7: так инварианты остаются в конструкторе рядом с фабрикой и не появляется
лишнего типа.

Шаги 3–8 сделаны: `CharacterClaimInfo` + `CharacterInfo` с тестами,
`ICharacterInfoRepository` + `CharacterInfoRepository` / `CharacterInfoMapper` с тестами маппинга,
регистрация в DI.

Первый мигрированный потребитель — `GET /x-game-api/{projectId}/characters/{id}/`
(`CharacterApiController.GetOne`). Выбран как проверка агрегата: у него уже был сквозной
интеграционный тест на настоящем MS SQL (Testcontainers), внешний контракт даёт с чем сверяться, и
он не тянет общих вью-моделей. Заодно ушла фабрика `CharacterFieldLayersBuilder.FromCharacterView`
(других вызовов у неё не было).

Что вскрыла эта проверка:

- **Расписание взносов не входило в `ProjectInfo`.** Таблица достаточности утверждала, что
  `CalculateClaimBalance` закрывается `ProjectFinanceSettings`, но там лежали только типы оплаты, а
  сами `ProjectFeeSettings` читались из EF-сущности `Project` по ленивой навигации. Добавлен
  `ProjectFeeSettingInfo`; `FinanceExtensions.ProjectFeeForDate` переключён на него, чтобы правило
  выбора действующей строки не размножилось.
- **Через API был недостижим статус `Discussed`.** `GetCharacterViewAsync` грузил в `Claims` только
  утверждённые заявки, поэтому персонаж с заявкой в обсуждении выглядел как «нет заявок». Агрегат
  несёт все заявки, статус чинится сам собой.
- **`ToPlayerContacts(UserInfo)` отдавал неподтверждённый VK**, в отличие от версии для `User` и
  вопреки контракту `PlayerContacts`. Всплыло при переходе на данные игрока из `IUserRepository`.

Ручная проверка плана запроса из раздела «Проверка» по-прежнему не сделана: интеграционные тесты
доказали, что EF6-запрос работает, но не то, что он один и без N+1.

Остальные потребители не мигрированы — это следующие PR.

---
*Создано: 14.08.2026*
*Обновлено: 18.08.2026 — шаги 1–2 завершены (#4581), уточнено размещение фабрики.*
*Обновлено: 18.08.2026 — реализованы шаги 3–8: тип, репозиторий, маппер, тесты, регистрация в DI.
Не проверено на живой БД: EF6-запрос покрыт только компиляцией, план запроса и отсутствие N+1
надо подтвердить вручную (см. «Проверка»).*
*Обновлено: 19.08.2026 — мигрирован первый потребитель (x-game-api GetOne), запрос выполнен на
настоящей БД под интеграционными тестами. Исправлена ошибка в таблице достаточности: расписание
взносов пришлось добавить в `ProjectInfo`.*
