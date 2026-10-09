# ADR021: ClaimInfo — заявка вместе с персонажем и игроком как доменный тип

Статус: предложен.

Проблема:
==

Агрегат персонажа (ADR013) хранит заявки как `CharacterInfo.Claims`, а корнем на запись
остаётся `Character` (ADR014). У отдельной заявки поэтому нет своего доменного типа.
`CharacterClaimInfo` — это элемент коллекции, а не самостоятельный объект: персонажа он не знает,
а `ProjectInfo` и поля доступны только через агрегат.

На практике почти каждому, кто работает с одной заявкой, нужна тройка «персонаж, одна из его
заявок, профиль игрока». Сейчас эту тройку собирают вручную и каждый раз по-своему:

- **Ровно эта тройка уже существует** — `ClaimProblemContext` (`src/JoinRpg.Domain/Problems/`).
  Конструктор проверяет, что заявка лежит в `character.Claims`, а профиль принадлежит
  `claim.PlayerId`. Но тип живёт в `JoinRpg.Domain` и называется так, будто нужен только для
  фильтров проблем.
- **Эту тройку сейчас собирают руками:**
  - `CheckInController.ShowCheckInForm` (Portal): `GetClaimById(claimId).PlayerId` →
    `GetRequiredUserInfo` → `CheckInClaimModel`, а модель внутри снова строит `ClaimProblemContext`.
  - `XGameApi/CheckInController.PrepareClaimFoCheckIn`: грузит EF-`Claim` только ради
    идентификаторов, потом `CharacterInfo`, `GetClaimById` и `GetRequiredUserInfo`.
  - `ClaimServiceImpl.CheckInClaim` и `MoveByMaster` (до ADR021):
    `ctx.CharacterInfo + ctx.ClaimInfo + GetRequiredUserInfo(ctx.ClaimInfo.PlayerId)`.
  - `ClaimViewModel` (строка ~144) получает `CharacterInfo` и `UserInfo` отдельными параметрами
    рядом с EF-`Claim`. Контроллер (`ClaimController`) грузит их отдельными вызовами.
  - `ApiInfoBuilder.CreatePlayerInfo(CharacterInfo, CharacterClaimInfo, UserInfo)` — та же тройка
    без названия.
- **Пакетной загрузкой тройки сейчас занимается только `ClaimProblemContextLoader`**
  (`WebPortal.Models/ClaimList`). Рядом (до ADR021) было четыре почти одинаковых метода `LoadPlayers`
  (`CharacterListController`, `ProjectRoleGridViewService`, `CharacterApiViewService` и
  выгрузка в `ClaimListController`). Каждый заново выбирает `PlayerId` заявок и вызывает
  `GetRequiredUserInfos`. Экспорт списка заявок
  загружает игроков второй раз, хотя `ClaimProblemContextLoader` уже загрузил их для той же страницы.
- **Пара «персонаж + заявка» тоже передаётся разрозненно:**
  - `CalculateClaimBalance(this CharacterInfo, CharacterClaimInfo, ProjectInfo)`: `ProjectInfo`
    здесь избыточен, он есть в агрегате;
  - `AccessArgumentsFactory.Create(CharacterInfo, CharacterClaimInfo?, …)`;
  - `RequestParticipantViewModel(CharacterInfo, CharacterClaimInfo, ProjectInfo)`;
  - `CharacterNavigationViewModel.HasAccessToClaim(claim, character, user)`;
  - write-хэндл `IClaimUpdateHandle` и `ClaimMutationContext`. В них отдельными полями лежат
    `CharacterInfo` и `ClaimInfo`, а согласованность пары держится только на комментарии
    «это тот же экземпляр».

Пока данные идут отдельными параметрами, их можно передать несогласованными: заявку одного персонажа
вместе с чужим персонажем или профиль не того игрока. `ClaimProblemContext` закрыл эту дыру для
проблем. Во всех остальных местах она по-прежнему открыта.

Решение:
==

### 1. Два доменных типа в `DomainTypes`

Файлы: `src/JoinRpg.DomainTypes/Characters/Claims/ClaimInCharacter.cs` и `ClaimInfo.cs`.
Новых ссылок проекту не нужно: `CharacterInfo`, `CharacterClaimInfo` и `UserInfo` уже лежат
в `DomainTypes`.

```csharp
/// Заявка в составе своего агрегата: персонаж и одна из его заявок.
public record class ClaimInCharacter
{
    public CharacterInfo Character { get; }
    public CharacterClaimInfo Claim { get; }
    public ProjectInfo ProjectInfo => Character.ProjectInfo;
    public ClaimIdentification ClaimId => Claim.ClaimId;

    public ClaimInCharacter(CharacterInfo character, ClaimIdentification claimId);
    public ClaimInCharacter(CharacterInfo character, CharacterClaimInfo claim); // проверка «claim ∈ character.Claims»
}

/// Заявка, её персонаж и профиль игрока. Бывший ClaimProblemContext.
public record class ClaimInfo
{
    public ClaimInCharacter ClaimInCharacter { get; }
    public UserInfo Player { get; }

    public CharacterInfo Character => ClaimInCharacter.Character;
    public CharacterClaimInfo Claim => ClaimInCharacter.Claim;
    public ProjectInfo ProjectInfo => Character.ProjectInfo;

    public ClaimInfo(ClaimInCharacter claim, UserInfo player); // проверка Player.UserId == Claim.PlayerId
}
```

**Почему два уровня, а не один.** Профиль игрока стоит отдельного запроса, а значительной части
потребителей он не нужен: write-хэндл, баланс, `AccessArguments`, проживание, навигация.
Если сделать профиль обязательным, эти места будут грузить его зря. Если сделать его nullable,
пропадёт инвариант, ради которого `ClaimProblemContext` его и требует: проблема «в профиле не
хватает контактов» не должна исчезать молча из-за того, что профиль забыли загрузить. Поэтому
два типа с разным контрактом, а не один с необязательным полем.

**Почему `ClaimInfo` включает `ClaimInCharacter`, а не наследует его.** Тип хранит пару
внутри себя и не является ею. Так один и тот же `ClaimInCharacter` можно передать дальше без
профиля, а равенство record не смешивает два типа.

`ProjectInfo` отдельным полем не хранится ни в одном из типов: как и раньше, это защита от второго
канала тех же метаданных. Инвариант ссылочного равенства `ProjectInfo` из ADR013 продолжает
действовать — на стороне записи оба типа строятся из `CharacterInfo` хэндла, а не из кешированного.

### 2. Поведение переезжает на тип

Методы, которые сейчас принимают пару отдельными параметрами, получают перегрузку на
`ClaimInCharacter`. Старые сигнатуры помечаются `[Obsolete]` (счётчик предупреждений служит
burndown-метрикой) и удаляются, когда у них не остаётся вызывающих.

| Сейчас | Станет |
|---|---|
| `character.CalculateClaimBalance(claim, projectInfo)` | `claim.CalculateBalance()` |
| ~~`character.GetFieldLayers(access, claimId)`~~ | ~~`claim.GetFieldLayers(access)`~~ |
| ~~`AccessArgumentsFactory.Create(character, claim, user)`~~ | ~~`AccessArgumentsFactory.Create(claim, user)`~~ |
| ~~`CharacterNavigationViewModel.HasAccessToClaim(claim, character, user)`~~ | ~~метод над `ClaimInCharacter`~~ |

Пересмотрено в шаге 5: из этого списка в боевом коде пара реально передавалась только в расчёт
баланса. `GetFieldLayers` и `AccessArgumentsFactory.Create` с конкретной заявкой зовут лишь
тесты и сам `CharacterInfo`, а `HasAccessToClaim` — приватный хелпер одного файла. Перегрузки
без вызывающих не заводим. Старая сигнатура баланса не помечена `[Obsolete]`, а удалена: после
перевода вызывающих у неё не осталось ни одного.

На `ClaimInfo` переезжает то, чему нужен профиль: проблемы (`IClaimProblemValidator`,
`IClaimProblemFilter`), `ClaimCheckInValidator`, `ApiInfoBuilder.CreatePlayerInfo`.

Фильтры и валидатор проблем остаются в `JoinRpg.Domain`. Переезжает только тип, а
переносить фильтры вместе с ним этот ADR не предлагает.

### 3. Загрузчик

`ClaimProblemContextLoader` превращается в репозиторий `IClaimInfoRepository`
(`Data.Interfaces/Characters/`, реализация в `Dal.Impl`). Он собран из `ICharacterInfoRepository`
и `IUserRepository`, своего SQL у него нет.

Это именно отдельный интерфейс, а не методы-расширения над парой репозиториев. Так вызывающему
нужна одна зависимость, а не две, и в юнит-тестах загрузку можно подделать одним фейком.

```csharp
Task<ClaimInfo?> GetClaimInfoOrDefault(ClaimIdentification claimId);
Task<IReadOnlyDictionary<ClaimIdentification, ClaimInfo>> GetClaimInfos(IReadOnlyCollection<ClaimIdentification> claimIds);

/// Утверждённые заявки: персонажи без утверждённой заявки в словарь не попадают.
Task<IReadOnlyDictionary<CharacterIdentification, ClaimInfo>> GetApprovedClaimInfos(IReadOnlyCollection<CharacterInfo> characters);
```

Пакетные методы делают два запроса на весь список: персонажей одним
`GetCharacterInfosByClaims`, профили одним `GetRequiredUserInfos`. Это правило уже работает
в текущем загрузчике.

`GetApprovedClaimInfos` заменяет три копии `LoadPlayers` (сетка ролей, список персонажей, API персонажей; четвёртая — в выгрузке списка заявок, она уходит в шаге 6). Персонажи у этих потребителей уже
загружены, поэтому метод принимает готовые агрегаты и догружает только профили. Персонаж без
утверждённой заявки — нормальный случай, поэтому результат — словарь без такого ключа, а не
`ClaimInfo` с пустым игроком: инвариант «у заявки всегда есть игрок» сохраняется.

### 4. Сторона записи

`IClaimUpdateHandle` вместо пары `CharacterInfo` + `ClaimInfo` отдаёт
`ClaimInCharacter ClaimSnapshot` — снимок ДО, построенный из `CharacterInfo` хэндла.
`ClaimMutationContext` принимает его вместо пары и передаёт его персонажа базовому
`CharacterMutationContext` — второго параметра со снимком персонажа больше нет. Для операций, которым
нужен профиль, добавляется `Task<ClaimInfo> LoadClaimInfo(IUserRepository)`: репозиторий приходит
параметром, потому что сервисы берут репозитории из `IUnitOfWork`, а не из DI. Сейчас `ClaimInfo`
вручную собирает `CheckInClaim`.

`MoveByMaster` на `LoadClaimInfo` не переходит: ему нужен профиль игрока сам по себе, для правил
переноса на *целевого* персонажа (`EnsureCanMoveClaim`), а не заявка вместе со своим персонажем.

Свойство `ctx.ClaimInfo` (и одноимённое в `IClaimUpdateHandle`) означало `CharacterClaimInfo`.
Рядом с типом `ClaimInfo` это было бы двусмысленно, поэтому оно переименовано в
`CharacterClaimInfo` — механически, отдельным PR, и осталось сокращением для `ClaimSnapshot.Claim`.

Профиль в хэндл **не** добавляется: ADR014 намеренно держит хэндл узким, и большинству мутаций
профиль не нужен.

### 5. Именование

`ClaimInfo` встаёт в один ряд с `ProjectInfo`, `CharacterInfo` и `UserInfo`: это доменный снимок,
а не контекст операции. Слово «контекст» уже занято мутационными контекстами ADR009/ADR014
(`ClaimMutationContext`), поэтому `ClaimProblemContext` переименовывается.

Имя было занято публичным DTO x-game-api `XGameApi.Contract.ClaimInfo`. Он переименован в
`ClaimDetails`. JSON-ответ `GET x-game-api/{projectId}/claims/{claimId}` от этого не меняется,
меняется только имя схемы в Swagger. Пакетом контракт не публикуется (`IsPackable` не задан),
внешних сборок с этим типом нет.

`ClaimView` и `ClaimWithPlayer` заняты legacy-типами в `ICharacterRepository.cs`. `ClaimInCharacter`
ни с чем не пересекается.

Потребители:
==

По убыванию пользы:

1. **Собирают ровно эту тройку** — сразу переходят на `ClaimInfo` через репозиторий:
   - check-in в Portal (`CheckInController.ShowCheckInForm`, `CheckInClaimModel`);
   - check-in в x-game-api (`PrepareClaimFoCheckIn` и соседнее действие, которое грузит EF-`Claim`
     только ради 404) — EF отсюда уходит полностью;
   - `ClaimServiceImpl.CheckInClaim`.
2. **Список заявок и экспорт** — `ClaimListBuilder.BuildItem` перестаёт читать
   `claim.Character.CharacterName`, `claim.Player` и EF-баланс. `BuildItemForExport` берёт игрока
   из уже загруженного контекста, а `ClaimListController.LoadPlayers` удаляется.
3. **Утверждённая заявка пачкой** через `GetApprovedClaimInfos`:
   - сетка ролей (`ProjectRoleGridViewService`, `BuildPlayerCell`);
   - список персонажей (`CharacterListController`, `CharacterListItemViewModel`);
   - API персонажей (`CharacterApiViewService`, `ApiInfoBuilder`).
4. **Пара без профиля** — `ClaimInCharacter`:
   - баланс, `AccessArgumentsFactory`, `GetFieldLayers`;
   - проживание (`RequestParticipantViewModel`, `RoomTypeRoomsViewService`);
   - навигация персонажа;
   - write-хэндл.
5. **Частично**:
   - `ClaimViewModel` и `SecondRoleViewModel` перестают читать через EF-`Claim` то, что знает
     агрегат: имя персонажа, `IsActive`, утверждённую заявку, ответственного мастера, проект.
     `Claim.HasAccess` получает доменную перегрузку на `ClaimInCharacter`.
   - EF-`Claim` в этих вьюмоделях остаётся ради комментариев, финансовых операций и
     `ClaimAccommodationViewModel`. В `CharacterClaimInfo` их нет, и этот ADR их туда не добавляет.

Что сознательно НЕ делаем:
==

- **Уведомления.** `ClaimEmailModel` и `FieldsChangedEmail` построены на EF-`Claim` целиком.
  Их перевод на `ClaimInfo` — отдельная большая миграция, а не попутная замена типа.
- **`ClaimValidator.EnsureCanAddClaim/EnsureCanMoveClaim`.** Они работают с *целевым* персонажем
  и `UserClaimInfo`, а не с заявкой в её собственном персонаже, и под эту форму не подходят.
- **`ClaimCreationContext`.** На момент создания заявки ещё нет.
- **Профиль внутри агрегата персонажа.** ADR013 держит в агрегате только `UserInfoHeader`
  (имя — да, контакты — нет). `ClaimInfo` не меняет это правило. Профиль — отдельный слой,
  который загружается по требованию.
- **MCP.** Инструментов про заявки там пока нет, так что переводить нечего.

Порядок работ:
==

0. **Освободить имя.** `XGameApi.Contract.ClaimInfo` → `ClaimDetails`; `ctx.ClaimInfo` /
   `IClaimUpdateHandle.ClaimInfo` → `CharacterClaimInfo`.
1. **Перенос и переименование.** `ClaimProblemContext` → `ClaimInfo` + `ClaimInCharacter`
   в `DomainTypes`. Фильтры, валидатор и тесты переименовываются. Тесты конструктора остаются
   в `JoinRpg.Domain.Test` ради `MockedProject`: в `JoinRpg.DomainTypes.Test` моков нет. Поведение
   не меняется.
2. **Репозиторий.** `IClaimInfoRepository` вместо `ClaimProblemContextLoader`. На него
   переходят check-in в Portal и x-game-api и список заявок.
3. **Сторона записи.** `IClaimUpdateHandle.ClaimSnapshot` и `ClaimMutationContext.LoadClaimInfo(IUserRepository)`;
   на него переходит `ClaimServiceImpl.CheckInClaim`.
4. **Утверждённые заявки пачкой.** `GetApprovedClaimInfos`; удаление копий `LoadPlayers` в
   сетке ролей, списке персонажей и API.
5. **Методы пары.** Баланс считается от `ClaimInCharacter` (`CalculateBalance()`), старая
   сигнатура с лишним `ProjectInfo` удалена. Остальные перегрузки из §2 не понадобились.

   Следом — исправление расхождения с EF, которое нашлось при переводе списка заявок:
   неутверждённая заявка не платит за непубличные поля персонажа (её игрок их не видит), а
   доменный расчёт со времён ADR013 брал все поля. Правило живёт в
   `ClaimInCharacter.GetAllFields()` поверх `CharacterFieldLayers.ForUnapprovedClaim`. Затрагивает
   всё, что уже считало баланс по агрегату: проблемы заявки, регистрацию, API персонажей,
   конверты, капитанскую сетку, жильцов поселения.
6. **Вьюмодели заявки.** `ClaimViewModel`, `SecondRoleViewModel`, `ClaimListBuilder` перестают
   читать через EF то, что есть в `ClaimInfo`. Вместе с `BuildItemForExport` на `ClaimInfo`
   переходит и выгрузка списка заявок: пока ей нужен только профиль, загружать ради неё ещё и
   персонажей было бы дороже, чем сейчас.

Шаг 0 — два независимых PR (DTO вместе с этим ADR, свойство контекста отдельно). Шаг 1 требует
только освобождённого имени DTO. Шаги 2–6 не зависят друг от друга и идут после первого; шагу 3
нужно ещё переименованное свойство контекста.

Последствия:
==

- Согласованность «заявка — персонаж — игрок» проверяется в одном месте для всех потребителей,
  а не только для проблем.
- Пропадают ручные `GetClaimById` + `GetRequiredUserInfo` и четыре копии `LoadPlayers`.
- Сигнатуры становятся короче на один–два параметра, а избыточный `ProjectInfo` из них уходит.
- Цена: новый тип там, где раньше хватало двух параметров. Для одноразовых вызовов это
  церемония, поэтому старые сигнатуры удаляются только после переезда последнего вызывающего,
  а не одним махом.

---
*Создано: 08.10.2026*
