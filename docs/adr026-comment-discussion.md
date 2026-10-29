# ADR026: Обсуждение (комментарии) — доменный снимок и Razor-компоненты

Статус: принят, реализован — весь «План миграции» влит (PR 0–9, последний — снос легаси). Что
осталось открытым — в разделе «Статус» в конце.

Проблема:
==

Обсуждение показывают две страницы: заявка (`Claim/Edit`) и тема форума (`Forum/ViewThread`).
Обе рисуют одно и то же дерево: `CommentsListPartial.cshtml` → `<vc:comment>`
(`CommentViewComponent`) → `Views/Shared/Components/Comment/Comment.cshtml`, рекурсивно для ответов.
Модель — `CommentViewModel` (`WebPortal.Models/Comments`), и она целиком стоит на EF-сущностях:

| Где | Что делает с EF |
|---|---|
| `CommentViewModel` (конструктор) | строит дерево ответов рекурсией по `parent.Comments` (квадратично); читает `comment.Author`, `comment.CommentText`, `comment.Finance`; наружу отдаёт EF-`User Author` и EF-`FinanceOperation Finance` |
| `CommentExtensions.IsReadByUser` | ходит в `comment.Discussion.Watermarks` и `Discussion.Comments` |
| `Comment.cshtml` | `Model.Finance.PaymentType.GetDisplayName()` — ленивая загрузка на каждую финоперацию |
| `AvatarViewComponent` | отдельный компонент на каждый комментарий; сам аватар берётся из кеша `AvatarCachedLoader`, но для автора без выбранного аватара на каждый запрос идёт `GetUserInfo` |
| `ClaimsRepositoryImpl.GetClaimWithDetails` | ради навигаций грузит все заявки проекта (`LoadProjectClaims`) и `Include` комментариев, но не `Watermarks` и не `Finance.PaymentType` |
| `ForumRepositoryImpl.GetThread` | `Include` текста и автора, но не `Finance` — N+1 по комментариям |
| `ProjectEntityExtensions.GetClaim()` / `GetForumThread()` | владельца обсуждения ищут перебором `Project.Claims` / `Project.ForumThreads` целиком (#4986): `CommentRedirectHelper`, `DiscussionRedirectController`, `ForumController.CreateComment` / `ConcealComment`, а также цепочка проверки доступа `HasAnyAccess(CommentDiscussion)` → `HasPlayerAccess(CommentDiscussion)` |
| `HasPlayerAccess(IForumThread)` | доступ игрока к теме — через `forumThread.Project.Claims` целиком |
| `IForumThreadListItem` | список тем отдаёт EF-`User` (`Topicstarter`, `LastMessageAuthor`) |

Следствия:

- ленивые загрузки на обеих страницах обсуждения и в переходах `goto/*` (снапшот: `claim/Edit` — 1
  из 6, `ViewThread` — 3, `createcomment`/`concealcomment`/`goto/*` — 9; на проде на `claim/Edit`
  это сотни в час, #4960);
- `ClaimViewModel` держит EF-`Claim` только ради комментариев, финансов и поселения
  ([ADR021](adr021-claim-info.md)); комментарии — первый из трёх хвостов;
- последний пункт плана [ADR001](adr001-blazor.md) — «Перенести комменты в Blazor» — упирается в
  то, что модель нельзя ни сериализовать, ни построить без контекста EF.

Попутно найдено:

- страница темы форума **не обновляет отметку прочтения**, поэтому «новые» на форуме не гаснут;
- на странице заявки ответы, судя по коду, не бывают «новыми»: корни дерева строятся сразу, а
  `ChildComments` — отложенный `Select`, который выполняется уже при рендере, то есть после
  `UpdateReadCommentWatermark` в `ShowClaim`, когда отслеживаемая отметка уже сдвинута. Руками не
  воспроизводилось — проверит сценарий шага 1;
- смоук-сид почти не содержит обсуждений: нет ответов, комментариев игрока, скрытых от игрока
  комментариев, неподтверждённой финоперации и вида страницы со стороны игрока. Нули в снапшоте
  на этих страницах ничего не доказывают;
- в «Моих заявках» на профиле (`User/Details` → `_MyClaimsList`) счётчик непрочитанных
  комментариев всегда 0: `MyClaimListViewModel` передаёт в `ItemViewModel` константу. Бейдж в
  разметке есть, данных нет. В этом ADR не чиним (список кросспроектный, общему выражению (§2)
  там нужен признак мастера по каждому проекту, а не один `bool`).

Решение:
==

### 1. Доменный снимок обсуждения

Обсуждение — отдельный агрегат. [ADR013](adr013-character-info.md) §4 уже вынес его из
`CharacterInfo`: там остались только три `Last*CommentAt`. Типы кладём в
`JoinRpg.DomainTypes/Comments`.

```csharp
public record CommentDiscussionInfo(
    CommentDiscussionId Id,
    CommentDiscussionOwner Owner,
    IReadOnlyList<CommentInfo> Comments);   // по CreatedAt, при равенстве — по Id (см. 2c)

public record CommentInfo(
    CommentId Id,
    CommentId? ParentCommentId,
    UserIdentification Author,
    DateTime CreatedAt,
    bool IsCommentByPlayer,
    bool IsVisibleToPlayer,
    CommentExtraAction? ExtraAction,     // JoinRpg.DomainTypes.Characters.Claims
    MarkdownString? Text,                // CommentText.Text бывает null
    CommentFinanceInfo? Finance);

public record CommentFinanceInfo(
    int MoneyAmount,
    FinanceOperationType OperationType,
    FinanceOperationState State,
    DateTime OperationDate,
    PaymentTypeIdentification? PaymentType);
```

`CommentDiscussionOwner` — закрытая иерархия из двух вариантов:
`ClaimDiscussionOwner(ClaimIdentification)` и
`ForumDiscussionOwner(ForumThreadIdentification, CharacterGroupIdentification, bool IsVisibleToPlayer)`.
Поля темы нужны для проверки доступа игрока без EF: тема видна игроку, если `IsVisibleToPlayer` и
у него есть принятая заявка на персонажа из группы (`IsPartOfGroup` по `CharacterInfo`, как уже
делает `ForumController.GetForumThread`). Доступ игрока к заявке — по `ClaimInfo`. Проект
владельца — из его идентификатора; если он не совпал с проектом в маршруте, ответ — 404: сейчас
`GetDiscussion(projectId, …)` фильтрует по проекту, а `CommentDiscussionId` глобально уникален, и
без этой проверки мастер проекта A прошёл бы проверку по обсуждению проекта B.

Id комментария в снимке — новый типизированный `CommentId(int Value)` (`[TypedEntityId]`, в
`JoinRpg.DomainTypes` рядом с `CommentDiscussionId`). Как и `CommentDiscussionId` (#5408), он не
`IProjectEntityId`: без владельца у комментария нет адреса, а id уникален в БД сам по себе. Адресные
id (`ClaimCommentIdentification`, `ForumCommentIdentification`), которыми пользуются `IUriLocator`
и острова, собирает владелец: `owner.GetCommentIdentification(CommentId)`. Голый `int` в снимке и
моделях не появляется — ни в `ParentCommentId`, ни в ключах словарей, ни в методах репозитория.

Правила — те же, что в ADR013/ADR021:

- **Снимок не зависит от пользователя.** Видимость (`IsVisibleToPlayer` против мастерского доступа
  по `ProjectInfo`) и «прочитано» считаются снаружи. Правила `CommentExtensions.IsVisibleTo` и
  `GetUnreadCount` переезжают в SQL-предикат (`CommentDiscussionPredicates`, шаг 7) и в построитель
  модели (`DiscussionViewModelBuilder`, шаг 5); сам `CommentExtensions` удалён в шаге 9.
- **Авторы — только `UserIdentification`.** Полный `UserInfo` (контакты, соцсети, признак
  админа) обсуждению не нужен — нужны имя и аватарка. Поэтому аватарка становится частью
  `UserInfoHeader`: `UserInfoHeader(UserIdentification, UserDisplayName, AvatarIdentification?
  Avatar)`, параметр **обязательный**, без значения по умолчанию — компилятор заставляет каждое
  место построения решить про аватарку явно, и «без аватарки» не появится молча.
  - `AvatarIdentification` уже лежит в `Common.PrimitiveTypes`, новых зависимостей нет.
  - Данные почти везде бесплатны: `User.SelectedAvatarId` — скалярный FK, а `User` в местах
    построения шапки (~12 в продакшене: `User.ToUserInfoHeader()`, `UserInfo`, текущий
    пользователь из cookie, проекции `UserInfoRepository`, `ProjectMetadataRepository` /
    `ProjectMasterInfo`, `CharacterInfoLoader`, `ClaimsRepositoryImpl`, `CharacterGroupRepository`,
    `PlotRows`, `CreateUpdateMarksMapping`, фейки) уже загружен или присоединён — одна колонка в
    проекции.
  - Хранится id, а не URI: аватарка по id неизменяема, поэтому шапка не устаревает, DAL не знает о
    S3, JSON в островах растёт на одно число (список мастеров).
  - Кешей между запросами у шапки нет; целые шапки нигде не сравниваются и не служат ключами.
    Фейки и `ICurrentUserAccessor` обязаны заполнять поле так же, как БД (до шага 2d при
    имперсонации приходит `null`).
  - **Аватарка у шапки есть всегда** — решение принято в ревью #5905, дыры закрывают шаги 2d и 2e.
    - *Текущий пользователь.* Claim `joinrpg.AvatarId` собирает `ClaimInfoBuilder`, и через
      `GetClaimsAsync` он уже попадает в cookie Portal и IdPortal и в JWT `XApiLoginController`.
      Cookie Portal пересобирается из БД за секунды (`SecurityStampValidator`, `ValidationInterval`
      3 с), а после выбора аватарки `AvatarController` делает `RefreshSignInAsync`. Дыры две:
      - access-токены MCP (`OAuthServerRegistration.AuthorizeMethod`, ADR012) несут только `sub`,
        scopes и `projects` — нет ни аватарки, ни `joinrpg.DisplayName`, так что
        `ICurrentUserAccessor.DisplayName`, а с ним и `ToUserInfoHeader()`, под MCP падает (сейчас не
        стреляет: MCP только читает). Claims `joinrpg.*` (аватарка, имя) кладутся в эти токены;
        устаревают они до повторной авторизации клиента — refresh переиспользует principal, а живёт
        90 дней;
      - `ImpersonatedUser` отдаёт жёсткий `null`. `StartImpersonate` получает аватарку вместе с
        именем; вызывающие передают её, где знают пользователя (джобы под роботом,
        `ClaimServiceImpl`), инструменты с синтетическим именем — `null`.

      Чтение из БД на каждый запрос вместо claim отклонено. Прежний id в JWT до истечения и в
      токенах MCP — допустимое устаревание: картинка по нему живая (аватарки неизменяемы,
      `LoadAvatar` не фильтрует по `IsActive`).
    - *Пользователь без аватарки.* При создании она уже назначается синхронно
      (`MyUserStore.CreateImpl` → `AddGravatar`, все пути регистрации идут через него), а удалить
      выбранную нельзя (`UserServiceImpl` бросает исключение). Дыры остаются там, где пользователь
      создаётся в обход `CreateImpl`: сид виртуальных пользователей (Robot, Online payments) в
      `Dal.Impl/Migrations/Configuration.cs` и сырой INSERT в `Tools.RestoreLostUsers`. Плюс
      намеренная catch-ветка `CreateImpl`: если аватарка не сохранилась, регистрация доходит до
      конца, а аватарку заводит `DefaultAvatarsBackfillJob`.

    `AvatarIdentification?` в шапке после этого остаётся nullable: перевод `SelectedAvatarId` в
    NOT NULL — изменение DataModel, по нему отдельное решение.

  Обсуждение берёт шапки авторов пачкой через `GetRequiredUserInfoHeaders`. URI строит
  `IAvatarLoader` (синглтон-кеш `AvatarCachedLoader`, БД — раз на аватарку за жизнь процесса;
  отсутствующую в БД аватарку не запоминают и перезапрашивают).
  Построитель берёт адреса всех аватарок обсуждения одним вызовом `GetAvatars`: кеш тот же вечный,
  а промахи грузятся одним запросом к БД, а не по запросу на аватарку (#5774). Аватарки, которой
  нет в БД, у автора просто не будет. Сбрасывать ли кеш после `RecacheAvatar` (он меняет
  закешированный адрес, а вечный кеш продолжит отдавать старый) — не решено, вопрос в #5774.
  Интерфейс переезжает из `Joinrpg.Web.Identity` в `JoinRpg.Interfaces`, чтобы построитель в
  `WebPortal.Managers` клал в модель готовый URI; реализация остаётся на месте. Признак «автор —
  мастер» берётся из `ProjectInfo`, без запроса.
- **Финоперация — мини-снимок, а не доменная лента финансов.** Полная лента — #5420, ждать её не
  стали. Способ оплаты хранится как id и разрешается через `ProjectInfo` (`PaymentTypeInfo`), что
  закрывает ленивую загрузку `Finance.PaymentType`. Когда #5420 появится, `CommentFinanceInfo`
  заменяется ссылкой на её тип; потребитель у мини-снимка один — построитель модели обсуждения.
- **Отметка прочтения — отдельный вход, не часть снимка.** `GetWatermark` сейчас считает максимум
  из отметок пользователя и его собственных комментариев. Отметка пользователя грузится отдельным
  точечным запросом (`CommentId?` по паре «обсуждение, пользователь»), а собственные комментарии уже есть
  в снимке (отметок на пару бывает несколько — берётся максимум). Семантика «прочитано»
  сохраняется, а снимок неизменяем, поэтому «новое» считается до обновления отметки — для ответов
  это изменение поведения (см. «Попутно найдено»).

### 2. Репозиторий

`ICommentDiscussionRepository` в `JoinRpg.Data.Interfaces`, реализация в `JoinRpg.Dal.Impl`: одна
выборка EF6 с проекцией в внутренние строки (`AsNoTracking`, вложенный `Select`, не `Include`), затем
чистый маппер. Ловушки EF6 — те же, что в ADR013 (строки с `init`, без позиционных record).

- `GetDiscussion(CommentDiscussionId)` — снимок целиком.
- `GetOwner(CommentDiscussionId)`, `GetOwnerByComment(CommentId)` — владелец без комментариев
  (#4986). Отдельный метод для финоперации не нужен: её ключ и есть id комментария. Владелец
  определяется обратным запросом от `Claims` / `ForumThreads` по `CommentDiscussionId`, а не
  перебором проекта. Доступ проверяют вызывающие (§1).
- `GetReadWatermark(CommentDiscussionId, UserIdentification)`.

Батчевого метода для отметок прочтения нет. Спискам нужна не отметка, а счётчик непрочитанного, а
посчитать его по пачке отметок можно, только загрузив ещё и все комментарии каждого обсуждения.
Поэтому списки считают счётчик в SQL одним запросом:

- **списки заявок** (`ClaimListController`, по проекту и по группе) — уже так:
  `IClaimsRepository.GetUnreadDiscussionsForClaims` отдаёт словарь «обсуждение → число» и не
  трогает EF-сущности. Остаётся одним SQL-запросом; в шаге 7 переходит на общее выражение (ниже),
  ключ словаря типизируется в `CommentDiscussionId`;
- **список тем форума** — шаг 7, сейчас грузит заголовки всех комментариев и все отметки всех
  пользователей ради одного `GetUnreadCount` в памяти.

Правило «непрочитано» (выше максимума из отметок пользователя и его собственных комментариев, с
учётом видимости игроку) сейчас записано дважды: в SQL для заявок и в `CommentExtensions`.
Оно выносится в одно LinqKit-выражение над `CommentDiscussion`, и оба списка его используют.
Построитель дерева (§3) считает то же по снимку и отметке в памяти; совпадение семантики
закрепляется тестом на одних данных.

Фейк для тестов — в `DataModel.Mocks/Fakes`.

### 3. Модель и компоненты

Новый проект `JoinRpg.Web.Comments`:

- `DiscussionViewModel` / `CommentViewModel` — сериализуемые record'ы без EF и без
  `MarkupString`: текст — уже отрендеренный HTML строкой (как в `Web.Plots`), автор —
  `UserLinkViewModel` + готовый URI аватара (`Uri`, не id — модель сериализуема и годится для WASM), `ExtraAction` — готовый текст (русские названия сейчас живут
  в `[Display]` у `JoinRpg.CommonUI.Models.CommentExtraAction`, и `Web.Comments` на него не
  ссылается), дерево ответов — готовое, глубина отступа посчитана;
- компоненты `Discussion.razor`, `CommentItem.razor`, `AddCommentForm.razor`; дата — существующий
  `EventTime`, автор — `UserLink` и `JoinAvatar`;
- `AddCommentForm` в ответе на скрытый комментарий ставит «скрыть от игрока» и не даёт её снять —
  как сейчас `Comment.cshtml` (`HideFromUser = !comment.IsVisibleToPlayer`) и
  `Views/Comments/_AddCommentPartial.cshtml` (отмеченная галочка выводится текстом и скрытым
  полем). Видимый ответ на скрытый сервис отклоняет (§5), а общая форма обеих страниц
  (`Forum/CreateComment`) при ошибке молча возвращает в обсуждение, так что без этого мастер
  терял бы набранный текст. Тест bUnit на это обязателен. Остров `FinanceModerationButton`
  (ответ на комментарий финоперации) выбор оставляет свободным: ошибку сервера он показывает,
  а не глотает.

Новый проект вносится в [structure.md](structure.md).

Построитель — `DiscussionViewModelBuilder` в `JoinRpg.WebPortal.Managers`: снимок + профили
авторов + `ProjectInfo` + текущий пользователь + отметка прочтения → модель. Дерево собирается
линейно (группировка по `ParentCommentId`), а не рекурсией по коллекции; порядок ответов внутри
группы — тот же, что в снимке (§1), группировка его не теряет. Текст рендерится тем же
`ToHtmlString()` без `ILinkRenderer`, что и сейчас: ссылки на сущности в комментариях — не задача
этого ADR.

**Рендеринг — статический.** Страница остаётся cshtml и вставляет `<component type="…"
render-mode="Static" />`. Формы добавления и ответа — обычные POST в существующие экшены, как
сейчас. Раскрытие формы ответа — нативный `<details>` (`JoinCollapsePanel`) вместо
`data-toggle="collapse"` и встроенного `toggleCommentEditor`. Статическая форма сама ставит
`<AntiforgeryToken />` (образец — `JoinMoveControlNonInteractive`, `CheckInOverridePanel`) и
сохраняет то, что сейчас даёт MVC-шаблон редактора: обязательность текста и кнопку «Отмена».
Интерактивны только точечные острова внутри статического дерева, по образцу `RoomTypeListPage` →
`UnOccupyAllButton`. Тот остров — `prerender: false`, а `FinanceModerationButton` сейчас
`WebAssemblyPrerendered`; вложенный пререндеренный остров в статическом компоненте прецедента не
имел, и это проверялось в шаге 8. Проверено: `ClaimDiscussion` ставит его как
`InteractiveWebAssemblyRenderMode(prerender: true)`, кнопка приходит в HTML сервера вместе с маркером
компонента (это закрепляет `CommentDiscussionPagesScenario`), а в браузере остров оживает —
открывает диалог и подтверждает операцию.

**Финблок — только у заявки.** У комментариев форума финопераций нет, а остров
`FinanceModerationButton` и его модель (`FinanceModerationOperationView`) живут в `Web.Claims`.
Поэтому `Web.Comments` от `Web.Claims` не зависит: `Discussion` принимает необязательный
`RenderFragment<CommentViewModel>? ExtraContent` (доп. содержимое каждого комментария), а
`Web.Claims` даёт обёртку `ClaimDiscussion.razor`, которая подставляет туда финблок с островом.
Данные финблока построитель кладёт в сам комментарий — `CommentViewModel.Finance`
(`CommentFinanceViewModel` из `Web.Comments`, у форума `null`): текст операции по типу и знаку суммы
(`null` при нулевой сумме — тогда, как и сейчас, ни текста, ни суммы), название типа операции, сумма,
дата, название способа оплаты, флаги «отклонена» / «ждёт подтверждения» и может ли текущий
пользователь её подтвердить (`RequireModeration` + право по `ProjectInfo`). Надписи бейджей
(«Мастер не подтверждает оплату», «Ждет одобрения мастером») рисует обёртка.
`FinanceModerationOperationView` обёртка собирает из этих полей сама: id операции — из
`ClaimIdentification` и `CommentId`. Владельца в модели обсуждения нет, поэтому `ClaimIdentification`
обёртка получает параметром от страницы заявки (шаг 8). Отдельный словарь `CommentId` → модель
финблока отклонён при реализации шага 5: данные и так приходят в слот вместе с комментарием, а
зависимости от `Web.Claims` у модели нет.

Почему не всё обсуждение островом WASM: на длинных заявках JSON модели огромен (тот же эффект, что
у сетки ролей, [ADR011](adr011-roles-grid-payload.md)). Ещё для этого понадобился бы клиент
комментариев форума, а выигрыш — только ответ без перезагрузки страницы. Форму-остров можно
сделать потом поверх готовых компонентов (#5883, потеря текста при отказе).

### 4. Отметка прочтения на форуме

`ViewThread` обновляет отметку так же, как `ClaimController.ShowClaim`. Метод
`IClaimService.UpdateReadCommentWatermark` к заявке не привязан — он работает по обсуждению,
поэтому переезжает в `ICommentDiscussionService` (§5) и вызывается с обеих страниц. Проверка
доступа в нём (#4967) — отдельная задача.

### 5. Запись: `ICommentDiscussionService`

Сейчас «оставить комментарий» — две операции: `IClaimService.AddComment` и
`IForumService.AddComment`, а `ForumController.CreateComment` выбирает между ними по владельцу,
найденному перебором проекта. Общий у них только `CommentHelper`.

Комментарий к заявке без действия **меняет заявку**, у темы форума такого нет:

- `LastUpdateDateTime`, `LastPlayerCommentAt` или `LastMasterCommentAt` / `LastMasterCommentBy`, а
  для видимого игроку — `LastVisibleMasterCommentAt` / `By` (`CommentHelper.SetClaimTimes`). На них
  стоят списки заявок, фильтры «ждёт ответа» и проблемы заявки;
- статус: `AddedByMaster` → `Discussed`, если пишет игрок; `AddedByUser` → `Discussed`, если мастер
  пишет видимое игроку (`ClaimStatusTransition.MarkDiscussed`);
- уведомление по заявке, а не форумное.

Поэтому операция над заявкой остаётся операцией агрегата персонажа: `ChangeClaim` с доступом
`MasterOrPlayer` и `AllowArchived` и одним сохранением ([ADR014](adr014-claim-props-service.md)).
Объединяется **вход**, а не реализация. `ICommentDiscussionService` получает
`AddComment(CommentDiscussionId, CommentId? parent, bool isVisibleToPlayer, string text)` и
`UpdateReadCommentWatermark(CommentDiscussionId, CommentId)`. По владельцу (§2) сервис передаёт
комментарий к заявке в операцию заявки, а к теме — в операцию форума. Общими становятся:

- проверка родителя («нет такого — отказ», #5853);
- правило «на скрытый комментарий нельзя ответить видимо»;
- правило «скрыть комментарий от игрока может только мастер». Сейчас оно живёт в вызывающих:
  `ForumController.CreateComment` и `ClaimCommentViewService.AddComment` проверяют
  `RequestMasterAccess` сами, а сервисы — нет.

Контроллер больше не выбирает сервис, а `IClaimService.AddComment` / `IForumService.AddComment`
уходят из публичных интерфейсов. Вызывающих у них больше одного:

- `ForumController.CreateComment` — форма, у неё уже есть `CommentDiscussionId`;
- `ClaimCommentViewService` — серверный `IClaimCommentClient` за `/webapi/claim-comment/AddComment`,
  через него идёт «Комментарий» острова `FinanceModerationButton`. Запрос
  `AddClaimCommentRequest` остаётся в терминах заявки (`ClaimIdentification`,
  `ClaimCommentIdentification?`), а `CommentDiscussionId` сервис берёт из снимка заявки (он там с
  #5408);
- тесты: `SmokeProjectFixture`, ~15 интеграционных сценариев, `BlockedProjectScenario` — их
  переписывают в том же PR.

`ConcealComment` и `UpdateReadCommentWatermark` [ADR014](adr014-claim-props-service.md) §8 уже
называет операциями обсуждения, а не заявки, поэтому `ConcealComment(CommentId)` тоже переезжает
в `ICommentDiscussionService`. Проект для правил ADR023 (в заблокированном проекте отметка
прочтения молча не пишется) и проверки активности сервис берёт у владельца (§2): в
`CommentDiscussionId` проекта нет.

Расхождения, которые при объединении надо решить явно, а не унаследовать молча:

| | Заявка | Форум | Решение |
|---|---|---|---|
| ответ видимо на скрытый | отказ (`PendingComment.SetParent`) | молча делается скрытым | **отказ везде** — для форума изменение поведения |
| архивный проект | можно (ADR014) | нельзя (`EnsureProjectActive`) | **разницу оставляем** |
| `CreatedAt` | полное время | только дата (`createdAt.Date` в `CreateCommentForForumThread`, попадает и в `LastEditTime`) | **везде полное время** — PR 2c (#5908) |
| кто «игрок» для типа операции | `Claim.PlayerUserId == CurrentUser`: мастер на своей заявке — игрок | `HasMasterAccess`; а `IsCommentByPlayer` на обеих сторонах — по `HasMasterAccess` | **разницу оставляем** |
| игрок просит скрыть | молча видимо (`PlayerChange`) | молча видимо (`PlayerChange`); отказ — только в вызывающих | **отказ в сервисе везде**; проверки `RequestMasterAccess` из `ForumController.CreateComment` и `ClaimCommentViewService.AddComment` переезжают в сервис |

Решения реализуются в PR шага 4a; каждое изменение поведения — с отдельной пометкой в описании.

План миграции:
==

Каждый шаг — отдельный PR, собирается и проходит тесты сам. Шаги 1–3 и 7 от остальных не
зависят и мерджатся в любом порядке.

1. **Сценарий с обсуждениями.** Свой сценарий (смоук-сид не трогаем): заявка с ответами,
   комментариями игрока и мастеров, скрытым от игрока комментарием, неподтверждённой финоперацией
   со способом оплаты; тема форума с ответами. Запросы и от мастера, и от игрока (`MyClaim`).
   Сценарий проверяет и пометку «новое» на ответах (см. «Попутно найдено»). Снапшот
   пересобирается полным прогоном ([lazy-loads-baseline](lazy-loads-baseline.md)) и фиксирует,
   сколько загрузок есть сейчас.
2. **Доменные типы + `ICommentDiscussionRepository` + фейк + тесты.** Потребителей нет.

   Рядом — self-contained PR на `master`, от остального плана не зависят:
   - 2a. **Аватарка в `UserInfoHeader`** (§1): обязательный параметр, все места построения,
     включая тесты и фейки; кейс с аватаркой в `UserInfoHeaderJsonTest`.
   - 2b. **`IAvatarLoader` в `JoinRpg.Interfaces`**, вместе с потребителями в IdPortal
     (`UserAvatar.razor`, `OAuthServerRegistration`, тесты) и строкой про `JoinRpg.Interfaces` в
     structure.md.
   - 2c. **Полное время у комментариев форума**: убрать `.Date` в
     `CommentHelper.CreateCommentForForumThread`. Уже записанные комментарии не трогаем — у них
     остаётся только дата, поэтому снимок (§1) упорядочивает комментарии по `(CreatedAt, CommentId)`:
     сейчас порядок внутри дня держится на неявном порядке выборки EF, вторым ключом он становится
     явным. Только по `CommentId` нельзя: комментарии, восстановленные DRP без исходного id
     (`InsertRestoredClaimComment`, ветка с `SCOPE_IDENTITY()`), получают новый id при старом
     `CreatedAt` и уехали бы в конец обсуждения.
   - 2d. **Шапка текущего пользователя из любого источника** (§1): claims `joinrpg.*` (аватарка,
     имя) в access-токенах MCP; `IImpersonateAccessor.StartImpersonate` принимает аватарку
     (вызывающие с синтетическим именем передают `null`). Тест на `CurrentUserAccessor` для cookie,
     JWT, MCP-токена и имперсонации. От остального не зависит.
   - 2e. **Аватарка у пользователей, созданных в обход `CreateImpl`** (§1): сид виртуальных
     пользователей и `Tools.RestoreLostUsers`. От остального не зависит.

   Шаг 5 зависит от 2a и 2b; 2d и 2e его не блокируют — авторы комментариев берутся из БД.
3. **Отметка прочтения на форуме** (§4). Независимо от остального, на текущей EF-модели. Модель
   дерева в этом PR строится целиком до обновления отметки, иначе форум получит ту же потерю
   «новых» ответов, что и заявка.
4. **Владелец обсуждения (#4986).** Методы `GetOwner*`, перевод `CommentRedirectHelper`,
   `DiscussionRedirectController`, `ForumController.CreateComment` / `ConcealComment` и проверки
   доступа (§1) вместо цепочки `HasAnyAccess(CommentDiscussion)` → `HasPlayerAccess(CommentDiscussion)`.
   После этого цепочка и `GetClaim()` / `GetForumThread()` мертвы и удаляются в этом же PR (проверить
   `find_references`). Зависит от шага 2.
   4a. **`ICommentDiscussionService`** (§5): единый вход `AddComment`, `ConcealComment` и
   `UpdateReadCommentWatermark`. `ForumController.CreateComment` перестаёт выбирать сервис,
   `ClaimCommentViewService` идёт через новый сервис. Интеграционные сценарии и
   `SmokeProjectFixture` переписываются на новый вход. Решения из таблицы расхождений (§5) — здесь
   же: из `ForumServiceImpl.AddComment` уходит `visibleToPlayerUpdated` (молчаливое скрытие
   ответа), тесты «видимый ответ на скрытый в теме → отказ» и «игрок просит скрыть → отказ».
   Зависит от шага 4. Если шаг 3 к этому
   моменту влит, отметка форума переезжает сюда же.
5. **`JoinRpg.Web.Comments`: модель, компоненты (bUnit), построитель (юнит-тесты), строка в
   structure.md.** Потребителей нет. Зависит от шага 2.
   5a. **Пакетная загрузка аватарок** — `IAvatarLoader.GetAvatars` через тот же вечный кеш,
   промахи одним запросом `IUserRepository.LoadAvatars`; построитель обсуждения переходит на неё
   (#5774). Зависит от шага 5.
6. **Страница темы форума** на новом дереве: `ForumController.ViewThread` грузит снимок через
   репозиторий, `ForumThreadViewModel` перестаёт строить `CommentViewModel`. `GetThread` с `Include`
   всех комментариев заменяется запросом заголовка темы (название, группа, `IsVisibleToPlayer`),
   иначе обсуждение грузится дважды. Зависит от шага 5. Форум идёт первым как пилот: на нём нет
   финблока.
7. **Список тем форума** — `IForumThreadListItem` отдаёт `UserIdentification`/`UserInfoHeader`
   вместо EF-`User` и готовый `UnreadCount`, посчитанный в запросе общим выражением (§2), вместо
   коллекций `Comments` и `Watermarks`. `GetUnreadDiscussionsForClaims` переходит на то же
   выражение. Выражение записано как доменное правило: видимый комментарий с id выше последнего
   своего комментария и выше последней своей отметки (два `Max`). Вариант с `NOT EXISTS` («нет
   более позднего своего комментария или отметки») эквивалентен, но читался тяжелее и был
   заменён по ревью #5929. Агрегаты в SQL коррелированы только с обсуждением, не с комментарием
   (EF6 дублирует каждый из-за `?? 0`), — та же форма, что была у `GetUnreadDiscussionsForClaims`
   на проде до шага 7. SQL и план сверить на большом проекте. Независимо.
8. **Страница заявки** на новом дереве: обёртка `ClaimDiscussion` с финблоком и островом,
   `ClaimController.ShowClaim` грузит снимок и отметку прочтения (через `DiscussionViewService`) и передаёт обёртке
   `ClaimIdentification` (для id финоперации в диалоге модерации); из `GetClaimWithDetails` уходит
   `Include` комментариев. Технически зависит только от шага 5; после шага 6 — чтобы пилот на
   форуме обкатал компоненты. Сделано так: снимок, модель и сдвиг отметки (строго после модели) делает
   `DiscussionViewService` в `WebPortal.Managers`, его зовут и `ShowClaim`, и `ForumController.ViewThread`
   — доступ каждый проверяет сам. Обсуждение — свойство `ClaimViewModel.Discussion`, а не отдельная
   модель страницы, как у форума: `Edit.cshtml` и его партиалы обращаются к `ClaimViewModel` почти
   сотней мест, а ссылка `WebPortal.Models` → `Web.*` допустима (запрещена обратная, structure.md).
   `ClaimViewModel` больше не реализует `IEntityWithCommentsViewModel`. Из
   `GetClaimWithDetails` ушёл и `LoadProjectClaims`: из всех заявок проекта странице был нужен только
   игрок этой заявки, он теперь в `Include`.
9. **Снос легаси:** `CommentViewModel`, `CommentViewModelExtensions`, `IEntityWithCommentsViewModel`
   (у `ClaimViewModel` реализацию и `RootComments` снял шаг 8, у `ForumThreadViewModel` — шаг 6) и
   конструктор `AddCommentViewModel(IEntityWithCommentsViewModel)`, `CommentViewComponent`,
   `Comment.cshtml`, `CommentsListPartial.cshtml`, `_CommentPartial.cshtml`,
   `_AddCommentFormPartial.cshtml`, `CommentExtensions.IsReadByUser`. Для каждого удаляемого члена
   — `find_references`. `_AddCommentPartial` остаётся: его переиспользует
   `_SubmitPaymentDialog.cshtml`. Его «Отмена» зовёт `toggleCommentEditor`, который определён только в
   `CommentsListPartial`, — поэтому уже в шаге 8 (когда страница заявки перестаёт подключать
   `CommentsListPartial`) кнопку из диалога оплаты надо убрать, иначе она бросит `ReferenceError`.
   В шаге 8 модель диалога ставит `AddCommentViewModel.HideCancelButton`, и своей «Отмены» в окне
   нет — оно закрывается крестиком в заголовке. Новых `data-dismiss` не
   заводим (#5304), а сам диалог на Bootstrap-модалке переводится отдельно. Ветка с
   `toggleCommentEditor` уходит вместе с `CommentsListPartial`.
   Сделано так: кроме перечисленного удалён `CommentExtensions` целиком (`GetUnreadCount` и
   `IsVisibleTo` в проде уже не вызывались, только как эталоны в тестах; правило «видимый и выше
   максимума из своих комментариев и своих отметок» переписано эталоном прямо в
   `CommentDiscussionPredicatesTest` и `UnreadCommentsCountScenario`), EF-интерфейсы `ICommentHeader`, `ICommentDiscussionHeader`,
   `IForumThread`, `AddCommentViewModel.HideCancelButton`/`AddButtonId`/`FormId` с шаблонами id,
   `ClaimViewModel.CommentDiscussionId`. `AvatarViewComponent` остаётся: он рисует аватарку в шапке
   (`_LoginPartial`) и в выборе аватарки (`_AvatarSelector`). `AddCommentViewModel.CommentDiscussionId`
   и `ParentCommentId` стали typed id (`CommentDiscussionId?`, `CommentId?`; форма постит числа,
   собирает их `TypeConverter`), как и параметры `DiscussionRedirectController.ToDiscussion`/`ToComment`.
   Финансовые формы, унаследованные от `AddCommentViewModel`, id обсуждения больше не постят: его
   никто не читал, а диалог оплаты клал туда id заявки.

Не делаем:
==

- **Обсуждение целиком островом WASM** и форму комментария островом (#5883) — см. §3. Компоненты
  к этому готовы, но это отдельная задача.
- **Доменную ленту финансов** (#5420) и ленивые загрузки финблока страницы заявки
  (`ClaimFeeViewModel`, #4960) — это финансы, не обсуждение.
- **Перевод записи на доменные типы.** Шаг 4a объединяет только вход (§5). Операции внутри
  по-прежнему работают с EF-сущностями write-хэндла, уведомления не меняются. Ленивый
  `parentComment.Author` в `ForumServiceImpl` и `PendingComment.SetParent` остаётся.
- **Рендер ссылок на сущности** в тексте комментария: сейчас его нет, и `JoinrpgMarkdownLinkRenderer`
  зависит от EF-`Project`.
- **Переписывание счётчиков непрочитанного в списках заявок** сверх общего выражения и типизации
  ключа (шаг 7): `GetUnreadDiscussionsForClaims` и так один SQL-запрос без EF-сущностей.
- **Новые поля в DataModel** не нужны: снимок строится из существующих таблиц.

Статус
==

Принят и реализован: план выполнен — PR 0 (#5903), 1 (#5916), 2 (#5917), 2a–2e (#5905, #5904, #5908,
#5919, #5918), 3 (#5920), 4 (#5921), 4a (#5932), 5 (#5940), 5a (#5944), 6 (#5941), 7 (#5929),
8 (#5963) и 9 — снос легаси (PR «снос legacy-дерева комментариев (ADR026, PR 9)»).

Осталось открытым:

- **Сброс кеша аватарок** после `RecacheAvatar` (#5774): вечный кеш `IAvatarLoader` не узнаёт о
  перекешировании.
- **«Мои заявки»** показывают счётчик непрочитанного 0.
- **Облегчённый `JoinCollapsePanel` для «Ответить»** — у каждого комментария сейчас полноценная панель.
- **Пустые `label` у `FormRow` без подписи** (строка с кнопками в `AddCommentForm`).
- **`claim/Edit` — 5 ленивых загрузок**, все в финблоке (`ClaimFeeViewModel`, #4960, #5420), не в
  обсуждении.
- `IClaimService.ModerateFinanceOperation` всё ещё принимает голый `int parentCommentId`; естественный
  тип — `FinanceOperationIdentification`, правка затрагивает сервис и десяток тестов.
