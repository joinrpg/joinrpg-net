# Схема базы данных

Обзор схемы основной БД joinrpg.ru. Источник истины — классы в `src/JoinRpg.DataModel`
и fluent-конфигурация в [`src/JoinRpg.Dal.Impl/MyDbContext.cs`](../src/JoinRpg.Dal.Impl/MyDbContext.cs).
Основная БД — **MS SQL Server, Entity Framework 6** (не EF Core). Миграции —
`src/JoinRpg.Dal.Impl/Migrations`, см. [ef-migrations.md](ef-migrations.md).

Помимо основной БД есть две отдельные PostgreSQL-БД на EF Core — очередь уведомлений
и трекинг ночных джоб, они описаны [в конце](#отдельные-бд-postgresql).

> **Документ поддерживается вручную.** Как его перегенерировать после изменения модели —
> см. [db-schema-regenerate.md](db-schema-regenerate.md).

## Как читать

Схема большая (≈40 таблиц), поэтому она разложена на **обзорную карту** и **диаграммы по доменам**
с полным составом полей. Соглашения:

* Имена таблиц — фактические имена в БД (`dbo.*`), имена полей — фактические колонки.
* `PK` / `FK` / `UK` — первичный ключ / внешний ключ / уникальный индекс.
* Комплексные типы EF6 (`MarkdownDbValue`, `IntList`) раскладываются в колонки
  `{Property}_Contents` и `{Property}_ListIds` — так они и показаны.
* Почти все FK на `Projects`/`Users` — с `NO ACTION` (каскад выключен явно, см. `ConfigureCascadeDeleteOff`).
  На диаграммах каскадность не отражена, чтобы не удваивать число линий.
* Показаны все колонки, которые реально есть в базе, — включая осиротевшие: колонка осталась
  от старой миграции, а модель её уже не отображает. Такие помечены `legacy` в комментарии.
* Сверка документа с реальной схемой автоматическая — тест `DbSchemaDocumentationScenario`
  в `JoinRpg.IntegrationTest` поднимает базу миграциями и сравнивает её с этим файлом.

---

## Обзорная карта

Только «хабовые» таблицы и связи между доменами. Поля опущены.

```mermaid
erDiagram
    Projects ||--|| ProjectDetails : "1:1 настройки"
    Projects ||--o{ ProjectAcls : "права мастеров"
    Projects ||--o{ ProjectFields : "поля"
    Projects ||--o{ CharacterGroups : "дерево групп"
    Projects ||--o{ Characters : "роли"
    Projects ||--o{ Claims : "заявки"
    Projects ||--o{ PlotFolders : "сюжеты"
    Projects ||--o{ PaymentTypes : "типы оплаты"
    Projects ||--o{ ProjectAccommodationTypes : "поселение"
    Projects }o--o{ KogdaIgraGames : "привязка к КогдаИгре"

    Users ||--o{ ProjectAcls : "мастер в проектах"
    Users ||--o{ Claims : "игрок"
    Users ||--o{ Comments : "автор"

    Characters ||--o{ Claims : "заявки на роль"
    Characters |o--o| Claims : "ApprovedClaim"
    CharacterGroups ||--o{ ForumThreads : "форум группы"

    Claims ||--|| CommentDiscussions : "обсуждение"
    Claims ||--o{ FinanceOperations : "платежи"
    Claims |o--o| AccommodationRequests : "поселение"

    CommentDiscussions ||--o{ Comments : "комментарии"
    Comments ||--o| FinanceOperations : "1:1 финоперация"

    PlotFolders ||--o{ PlotElements : "элементы"
    PlotElements }o--o{ Characters : "адресаты"
    PlotElements }o--o{ CharacterGroups : "адресаты"

    ProjectFields ||--o{ ProjectFieldDropdownValues : "варианты"
    ProjectAccommodationTypes ||--o{ ProjectAccommodations : "комнаты"
    AccommodationRequests }o--o| ProjectAccommodations : "размещение"
```

Ключевая особенность модели: **значения полей персонажей и заявок не нормализованы** —
они лежат JSON-ом в `Characters.JsonData` / `Claims.JsonData`, а `ProjectFields` описывает
только метаданные полей. Точно так же денормализовано дерево групп: родители хранятся
строкой id через запятую в `ParentGroupsImpl_ListIds` (с 2016 года, миграция `NewParentGroups`),
а не таблицей связи.

---

## 1. Проект и его настройки

```mermaid
erDiagram
    Projects {
        int ProjectId PK
        string ProjectName
        datetime CreatedDate
        bool Active
        bool IsAcceptingClaims
        datetime CharacterTreeModifiedAt "obsolete, заменяется версией метаданных"
    }

    ProjectDetails {
        int ProjectId PK "он же FK на Projects, 1:1"
        string ClaimApplyRules_Contents "markdown"
        string ProjectAnnounce_Contents "markdown"
        bool IsPublicProject
        bool EnableManyCharacters
        bool AllowSecondRoles
        bool AutoAcceptClaims
        bool PublishPlot
        bool ScheduleEnabled
        bool EnableCheckInModule
        bool CheckInProgress
        bool EnableAccommodation
        bool FinanceWarnOnOverPayment
        bool PreferentialFeeEnabled
        string PreferentialFeeConditions_Contents "markdown"
        int CharacterNameField_ProjectFieldId FK "null = имя игрока"
        int CharacterDescription_ProjectFieldId FK "null = нет описания"
        int DefaultTemplateCharacterId FK "шаблон новой роли"
        int DefaultProjectRolesListId FK "сетка ролей по умолчанию"
        int ClonedFromProjectId FK "откуда клонирован"
        enum ProjectCloneSettings
        bool DisableKogdaIgraMapping
        string FieldsOrdering "порядок полей, строка id"
        string PlotFoldersOrdering
        enum RequireRealName "MandatoryStatus"
        enum RequireTelegram
        enum RequireVkontakte
        enum RequirePhone
        enum RequirePassport
        enum RequireRegistrationAddress
    }

    ProjectAcls {
        int ProjectAclId PK
        int ProjectId FK
        int UserId FK
        guid Token "токен-приглашение мастера"
        bool IsOwner
        bool CanGrantRights
        bool CanChangeFields
        bool CanChangeProjectProperties
        bool CanManageClaims
        bool CanEditRoles
        bool CanManageMoney
        bool CanSendMassMails
        bool CanManagePlots
        bool CanManageAccommodation
        bool CanSetPlayersAccommodations
    }

    CaptainAccessRuleEntities {
        int CaptainAccessRuleEntityId PK
        int ProjectId FK
        int CharacterGroupId FK "группа, которой командует капитан"
        int CaptainUserId FK
        bool CanApprove
    }

    ProjectRolesLists {
        int ProjectRolesListId PK
        int ProjectId FK
        string Name
        int CharacterGroupId FK "корень сетки, null = весь проект"
        bool PublicMode
        string FieldsImpl_ListIds "показываемые поля, строка id"
        enum ContactsColumn "ProjectRolesListVisibilityMode"
        enum GroupsColumn
        enum GroupsViewMode "RolesGridGroupsViewMode"
        enum ShowRolesFilter
    }

    GameReport2DTemplate {
        int GameReport2DTemplateId PK
        string GameReport2DTemplateName
        int ProjectId FK
        int FirstCharacterGroupId FK "ось X"
        int SecondCharacterGroupId FK "ось Y"
        datetime CreatedAt
        int CreatedById FK
        datetime UpdatedAt
        int UpdatedById FK
    }

    KogdaIgraGames {
        int KogdaIgraGameId PK "ид на стороне КогдаИгры, не автоинкремент"
        string Name
        bool Active
        string RegionName
        datetime Begin
        datetime End
        string MasterGroupName
        string SiteUri
        string VkClub
        string LjComm
        string TelegramChannel
        string JsonGameData "сырой ответ КогдаИгры"
        datetimeoffset UpdateRequestedAt "UpdateRequestedAt больше LastUpdatedAt = пора обновить"
        datetimeoffset LastUpdatedAt
    }

    KogdaIgraGameProjects {
        int KogdaIgraGame_KogdaIgraGameId PK "FK"
        int Project_ProjectId PK "FK"
    }

    AdvertisementLogEntries {
        int AdvertisementLogEntryId PK
        int ScheduleId "расписание захардкожено, FK нет (ADR010)"
        int Method "канал"
        int ProjectId FK
        int CharacterId FK "null = реклама игры, не роли"
        int Status
        datetimeoffset SentAt
    }

    Projects ||--|| ProjectDetails : ""
    Projects ||--o{ ProjectAcls : ""
    Projects ||--o{ CaptainAccessRuleEntities : ""
    Projects ||--o{ ProjectRolesLists : ""
    Projects ||--o{ GameReport2DTemplate : ""
    Projects ||--o{ AdvertisementLogEntries : ""
    Projects ||--o{ KogdaIgraGameProjects : ""
    KogdaIgraGames ||--o{ KogdaIgraGameProjects : ""
    ProjectDetails }o--o| Projects : "ClonedFromProject"
```

`ProjectAcls.Token` — токен-приглашение мастера, генерируется при создании записи.
`CaptainAccessRuleEntities` — отдельный от ACL механизм: капитан группы, который может
согласовывать заявки в свою группу. Таблица `AdvertisementLogEntries` отображается
с класса `AdvertisementLogEntryEntity` (имя таблицы задано явно в `MyDbContext`).

---

## 2. Метаданные полей

```mermaid
erDiagram
    ProjectFields {
        int ProjectFieldId PK
        int ProjectId FK
        string FieldName
        enum FieldType "ProjectFieldType: String, Text, Dropdown, Multiselect, Number, Checkbox, Header и др."
        enum FieldBoundTo "Character или Claim"
        enum MandatoryStatus "Optional, Recommended, Required"
        string Description_Contents "markdown, для игрока"
        string MasterDescription_Contents "markdown, для мастера"
        bool IsPublic
        bool CanPlayerView
        bool CanPlayerEdit
        bool IsActive "soft-delete"
        bool WasEverUsed "если true, физически удалить нельзя"
        bool ValidForNpc
        bool IncludeInPrint
        bool ShowOnUnApprovedClaims
        int Price "цена поля, участвует в расчёте взноса"
        string ValuesOrdering "порядок вариантов, строка id"
        int CharacterGroupId FK "спецгруппа: заполнил поле — попал в группу"
        string ProgrammaticValue "внешний код для интеграций"
        string AviableForImpl_ListIds "группы, для которых поле доступно"
    }

    ProjectFieldDropdownValues {
        int ProjectFieldDropdownValueId PK
        int ProjectFieldId FK
        int ProjectId FK
        string Label
        string Description_Contents "markdown"
        string MasterDescription_Contents "markdown"
        int Price "цена варианта"
        bool PlayerSelectable
        bool IsActive "soft-delete"
        bool WasEverUsed
        int CharacterGroupId FK "спецгруппа варианта"
        string ProgrammaticValue
    }

    ProjectFields ||--o{ ProjectFieldDropdownValues : "варианты"
    Projects ||--o{ ProjectFields : ""
    Projects ||--o{ ProjectFieldDropdownValues : ""
    ProjectFields }o--o| CharacterGroups : "спецгруппа"
    ProjectFieldDropdownValues }o--o| CharacterGroups : "спецгруппа"
```

Самих **значений** полей в схеме нет: они в `Characters.JsonData` и `Claims.JsonData`
(словарь `ProjectFieldId → значение`). Поэтому изменение метаданных поля не требует
миграции данных, но и запросить «все персонажи со значением X» SQL-ом напрямую нельзя.

---

## 3. Персонажи, группы, заявки

```mermaid
erDiagram
    CharacterGroups {
        int CharacterGroupId PK
        int ProjectId FK
        string CharacterGroupName
        bool IsRoot "корень дерева, ровно один на проект"
        bool IsSpecial "спецгруппа поля или варианта"
        bool IsPublic
        bool IsActive "soft-delete"
        string Description_Contents "markdown"
        string ParentGroupsImpl_ListIds "родители, строка id через запятую"
        int ResponsibleMasterUserId FK
        string ChildCharactersOrdering
        string ChildGroupsOrdering
        datetime CreatedAt
        int CreatedById FK
        datetime UpdatedAt
        int UpdatedById FK
    }

    Characters {
        int CharacterId PK
        int ProjectId FK
        string CharacterName
        enum CharacterType "Player, NonPlayer, Slot"
        string JsonData "значения полей персонажа"
        string Description_Contents "markdown, только в дереве ролей"
        string ParentGroupsImpl_ListIds "группы персонажа, строка id"
        bool IsPublic
        bool IsActive "soft-delete"
        bool IsHot "горячая вакансия (ADR010)"
        bool InGame "играет прямо сейчас"
        bool HidePlayerForCharacter
        bool AutoCreated
        bool IsAcceptingClaims "obsolete, см. CharacterType"
        int ApprovedClaimId FK "принятая заявка"
        int CharacterSlotLimit "для CharacterType = Slot"
        int OriginalCharacterSlot_CharacterId FK "из какого слота создан"
        string PlotElementOrderData
        datetime CreatedAt
        int CreatedById FK
        datetime UpdatedAt
        int UpdatedById FK
    }

    Claims {
        int ClaimId PK
        int ProjectId FK
        int CharacterId FK "корень агрегата — Character (ADR014)"
        int PlayerUserId FK
        enum ClaimStatus "AddedByUser, AddedByMaster, Discussed, Approved, CheckedIn, DeclinedByUser, DeclinedByMaster, OnHold"
        enum ClaimDenialStatus "ClaimDenialReason, null если не отказана"
        string JsonData "значения полей заявки"
        int CommentDiscussionId FK "1:1 обсуждение"
        int ResponsibleMasterUserId FK
        datetime CreateDate
        datetime LastUpdateDateTime
        datetime PlayerAcceptedDate
        datetime PlayerDeclinedDate
        datetime MasterAcceptedDate
        datetime MasterDeclinedDate
        datetime CheckInDate
        datetimeoffset LastMasterCommentAt
        int LastMasterCommentBy_Id FK
        datetimeoffset LastVisibleMasterCommentAt
        int LastVisibleMasterCommentBy_Id FK
        datetimeoffset LastPlayerCommentAt
        int CurrentFee "взнос, выставленный мастером вручную; null = из ProjectFeeSettings"
        bool PreferentialFeeUser
        bool PlayerAllowedSenstiveData "игрок открыл паспорт и адрес"
        int AccommodationRequest_Id FK
    }

    Projects ||--o{ CharacterGroups : ""
    Projects ||--o{ Characters : ""
    Projects ||--o{ Claims : ""
    Characters ||--o{ Claims : "CharacterId"
    Characters |o--o| Claims : "ApprovedClaimId"
    Characters |o--o| Characters : "OriginalCharacterSlot"
    Users ||--o{ Claims : "PlayerUserId"
    Users ||--o{ Claims : "ResponsibleMasterUserId"
    Users ||--o{ CharacterGroups : "ResponsibleMasterUserId"
    CommentDiscussions ||--|| Claims : ""
```

Дерево групп и принадлежность персонажей группам — **не FK**, а строки id
(`ParentGroupsImpl_ListIds`). Ссылочной целостности на уровне БД тут нет,
за консистентностью следит домен; актуальное дерево читается через
`IProjectMetadataRepository` (кешируется на запрос).

---

## 4. Сюжеты

```mermaid
erDiagram
    PlotFolders {
        int PlotFolderId PK
        int ProjectId FK
        string MasterTitle
        string MasterSummary_Contents "markdown"
        string TodoField "пока не пусто — папка не завершена"
        string ElementsOrdering "TODO, не используется"
        bool IsActive "soft-delete"
        datetime CreatedDateTime
        datetime ModifiedDateTime
    }

    PlotElements {
        int PlotElementId PK
        int ProjectId FK
        int PlotFolderId FK
        enum ElementType "PlotElementType: RegularPlot, Handout"
        bool IsCompleted
        bool IsMasterOnly
        bool IsActive "soft-delete"
        int Published "опубликованная игрокам версия текста; null = не опубликовано"
        datetime CreatedDateTime
        datetime ModifiedDateTime
    }

    PlotElementTexts {
        int PlotElementId PK "составной ключ"
        int Version PK "составной ключ"
        string Content_Contents "markdown"
        string TodoField
        int AuthorUserId FK
        datetime ModifiedDateTime
    }

    PlotElementCharacters {
        int PlotElement_PlotElementId PK "FK"
        int Character_CharacterId PK "FK"
    }

    PlotElementCharacterGroups {
        int PlotElement_PlotElementId PK "FK"
        int CharacterGroup_CharacterGroupId PK "FK"
    }

    ProjectItemTags {
        int ProjectItemTagId PK
        string TagName UK "уникален глобально, max 400"
    }

    PlotFolderProjectItemTags {
        int PlotFolder_PlotFolderId PK "FK"
        int ProjectItemTag_ProjectItemTagId PK "FK"
    }

    Projects ||--o{ PlotFolders : ""
    Projects ||--o{ PlotElements : ""
    PlotFolders ||--o{ PlotElements : ""
    PlotElements ||--o{ PlotElementTexts : "версии текста"
    PlotElements ||--o{ PlotElementCharacters : ""
    Characters ||--o{ PlotElementCharacters : ""
    PlotElements ||--o{ PlotElementCharacterGroups : ""
    CharacterGroups ||--o{ PlotElementCharacterGroups : ""
    PlotFolders ||--o{ PlotFolderProjectItemTags : ""
    ProjectItemTags ||--o{ PlotFolderProjectItemTags : ""
    Users ||--o{ PlotElementTexts : "AuthorUser"
```

Текст элемента сюжета вынесен в отдельную таблицу с версионированием
(`PlotElementTexts`, ключ `PlotElementId + Version`) — чтобы грузить пачку элементов
без текстов. `PlotElements.Published` указывает на опубликованную версию.

---

## 5. Комментарии и форум

```mermaid
erDiagram
    CommentDiscussions {
        int CommentDiscussionId PK
        int ProjectId FK
    }

    Comments {
        int CommentId PK
        int ProjectId FK
        int CommentDiscussionId FK
        int ParentCommentId FK "ответ на комментарий"
        int AuthorUserId FK
        datetime CreatedTime "в модели CreatedAt"
        datetime LastEditTime
        bool IsCommentByPlayer
        bool IsVisibleToPlayer "дочерний не может быть видимее родителя"
        enum ExtraAction "CommentExtraAction: смена статуса, финансы и т.п."
    }

    CommentTexts {
        int CommentId PK "он же FK, 1:1"
        string Text_Contents "markdown"
    }

    ForumThreads {
        int ForumThreadId PK
        int ProjectId FK
        int CharacterGroupId FK "форум привязан к группе"
        int CommentDiscussionId FK "1:1 обсуждение"
        string Header
        int AuthorUserId FK
        bool IsVisibleToPlayer
        datetime CreatedAt
        datetime ModifiedAt
    }

    UserForumSubscriptions {
        int UserForumSubscriptionId PK
        int ForumThreadId FK
        int UserId FK
    }

    ReadCommentWatermarks {
        int ReadCommentWatermarkId PK
        int ProjectId FK
        int UserId FK
        int CommentDiscussionId FK
        int CommentId FK "до какого комментария дочитано"
    }

    Projects ||--o{ CommentDiscussions : ""
    CommentDiscussions ||--o{ Comments : ""
    Comments ||--|| CommentTexts : ""
    Comments |o--o{ Comments : "ParentComment"
    CommentDiscussions ||--|| ForumThreads : ""
    CharacterGroups ||--o{ ForumThreads : ""
    ForumThreads ||--o{ UserForumSubscriptions : ""
    Users ||--o{ UserForumSubscriptions : ""
    CommentDiscussions ||--o{ ReadCommentWatermarks : ""
    Comments ||--o{ ReadCommentWatermarks : ""
    Users ||--o{ ReadCommentWatermarks : ""
    Users ||--o{ Comments : "Author"
```

`CommentDiscussions` — общая сущность обсуждения: ровно одно у каждой заявки
и ровно одно у каждой ветки форума. Текст комментария вынесен в `CommentTexts`
по той же причине, что и текст сюжета: комментарии часто грузят пачкой
(анализ финансов, поиск проблем), а текст нужен только на странице заявки.
Подписки на форум (`UserForumSubscriptions`) в коде пока никогда не создаются.

---

## 6. Финансы

```mermaid
erDiagram
    FinanceOperations {
        int CommentId PK "первичный ключ = Id комментария, 1:1 с Comments"
        int ProjectId FK
        int ClaimId FK
        int MoneyAmount "знак зависит от OperationType"
        enum OperationType "FinanceOperationType: Submit, Online, Refund, TransferTo, TransferFrom, PreferentialFeeRequest"
        enum State "FinanceOperationState: Proposed, Approved, Declined"
        int PaymentTypeId FK "обязателен для Submit, Online, Refund"
        int LinkedClaimId FK "для переводов между заявками"
        int RefundedOperationId FK "какую операцию вернули"
        int RecurrentPaymentId FK
        string ReccurrentPaymentInstanceToken "YYYYMM, уникальность списания"
        datetime OperationDate
        datetime Created
        datetime Changed
    }

    FinanceOperationBankDetails {
        int CommentId PK "он же FK на FinanceOperations, 1:1"
        string BankOperationKey "id операции в банке"
        string BankRefundKey "id возврата в банке"
        string QrCodeLink
        string QrCodeMeta
    }

    PaymentTypes {
        int PaymentTypeId PK
        int ProjectId FK
        string Name
        enum TypeKind "PaymentTypeKind: Custom, Cash, Online, OnlineSubscription"
        int UserId FK "ответственный мастер"
        bool IsActive "только soft-delete, физически не удаляется"
        bool IsDefault
    }

    ProjectFeeSettings {
        int ProjectFeeSettingId PK
        int ProjectId FK
        int Fee
        int PreferentialFee
        datetime StartDate "взнос действует с этой даты"
    }

    RecurrentPayments {
        int RecurrentPaymentId PK
        int ProjectId FK
        int ClaimId FK
        int PaymentTypeId FK
        enum Status "RecurrentPaymentStatus"
        int PaymentAmount "сумма первого платежа"
        int PaymentId
        string BankRecurrencyToken
        string BankParentPayment "Id финоперации с ведущими нулями"
        datetimeoffset CreateDate
        datetimeoffset CloseDate
    }

    MoneyTransfers {
        int Id PK
        int ProjectId FK
        int SenderId FK "мастер-отправитель"
        int ReceiverId FK "мастер-получатель"
        int Amount
        enum ResultState "MoneyTransferState"
        datetimeoffset OperationDate
        datetimeoffset Created
        int CreatedById FK
        datetimeoffset Changed
        int ChangedById FK
    }

    TransferTexts {
        int MoneyTransferId PK "он же FK, 1:1"
        string Text_Contents "markdown"
    }

    Comments ||--|| FinanceOperations : "CommentId"
    FinanceOperations ||--o| FinanceOperationBankDetails : ""
    Claims ||--o{ FinanceOperations : ""
    Claims |o--o{ FinanceOperations : "LinkedClaim"
    FinanceOperations |o--o{ FinanceOperations : "RefundedOperation"
    PaymentTypes |o--o{ FinanceOperations : ""
    RecurrentPayments |o--o{ FinanceOperations : ""
    Claims ||--o{ RecurrentPayments : ""
    PaymentTypes ||--o{ RecurrentPayments : ""
    Projects ||--o{ PaymentTypes : ""
    Projects ||--o{ ProjectFeeSettings : ""
    Projects ||--o{ MoneyTransfers : ""
    MoneyTransfers ||--|| TransferTexts : ""
    Users ||--o{ MoneyTransfers : "Sender, Receiver, CreatedBy, ChangedBy"
    Users ||--o{ PaymentTypes : "ответственный"
```

Главная неочевидность: **у `FinanceOperations` первичный ключ — `CommentId`**.
Каждая финансовая операция физически является комментарием к заявке, отдельного
автоинкрементного Id у неё нет. `FinanceOperationBankDetails` продолжает ту же
1:1-цепочку по тому же ключу.

Взнос игрока считается так: `Claims.CurrentFee`, если он задан вручную; иначе
подходящая по дате запись `ProjectFeeSettings`; плюс сумма цен заполненных полей
(`ProjectFields.Price`, `ProjectFieldDropdownValues.Price`).

---

## 7. Поселение

```mermaid
erDiagram
    ProjectAccommodationTypes {
        int Id PK
        int ProjectId FK
        string Name
        string Description_Contents "markdown"
        int Cost
        int Capacity "вместимость одной комнаты"
        bool IsPlayerSelectable
        bool IsInfinite "не реализовано"
        bool IsAutoFilledAccommodation "не реализовано"
    }

    ProjectAccommodations {
        int Id PK
        int ProjectId FK
        int AccommodationTypeId FK
        string Name "имя конкретной комнаты"
    }

    AccommodationRequests {
        int Id PK
        int ProjectId FK
        int AccommodationTypeId FK "какой тип хотят"
        int AccommodationId FK "куда поселили, null = ещё нет"
        enum IsAccepted "InviteState"
    }

    AccommodationInvites {
        int Id PK
        int ProjectId FK
        int FromClaimId FK "кто приглашает"
        int ToClaimId FK "кого приглашают"
        enum IsAccepted "InviteState"
        enum ResolveDescription "почему принято или отклонено"
        bool IsGroupInvite "legacy: колонка есть в базе с 2018 года, модель её не отображает"
    }

    Projects ||--o{ ProjectAccommodationTypes : ""
    Projects ||--o{ ProjectAccommodations : ""
    Projects ||--o{ AccommodationRequests : ""
    Projects ||--o{ AccommodationInvites : ""
    ProjectAccommodationTypes ||--o{ ProjectAccommodations : "комнаты"
    ProjectAccommodationTypes ||--o{ AccommodationRequests : "желающие"
    ProjectAccommodations |o--o{ AccommodationRequests : "жильцы"
    AccommodationRequests |o--o{ Claims : "Subjects, через Claims.AccommodationRequest_Id"
    Claims ||--o{ AccommodationInvites : "From и To"
```

Заявка на поселение — **групповая**: одна `AccommodationRequest` объединяет несколько
`Claims` (связь идёт от заявки: `Claims.AccommodationRequest_Id`). `AccommodationInvites` —
приглашения «поселись со мной», между заявками.

---

## 8. Пользователи

```mermaid
erDiagram
    Users {
        int UserId PK
        string UserName
        string Email
        string PasswordHash
        string BornName "имя"
        string FatherName "отчество"
        string SurName "фамилия"
        string PrefferedName "как обращаться"
        bool VerifiedProfileFlag
        int SelectedAvatarId FK
    }

    UserAuthDetails {
        int UserId PK "он же FK, 1:1"
        bool EmailConfirmed
        bool IsAdmin
        datetime RegisterDate
        datetimeoffset LastLoginDate
        string AspNetSecurityStamp
    }

    UserExtras {
        int UserId PK "он же FK, 1:1"
        byte GenderByte "enum Gender"
        byte Gender "legacy: колонка есть в базе с 2015 года, модель её не отображает"
        string PhoneNumber
        string Telegram
        string Vk
        bool VkVerified
        string Livejournal
        string Nicknames
        string GroupNames
        datetime BirthDate
        enum SocialNetworksAccess "ContactsAccessType"
        bool EnableTelegramPlayerDigestNotification
        string PassportData "видно мастерам только при PlayerAllowedSenstiveData"
        string RegistrationAddress "то же"
    }

    AllrpgUserDetails {
        int UserId PK "он же FK, 1:1"
        int Sid "id в allrpg.info"
        string JsonProfile
        bool PreventAllrpgPassword "obsolete"
    }

    UserExternalLogins {
        int UserExternalLoginId PK
        int UserId FK
        string Provider "telegram, Vkontakte и др., max 450"
        string Key
    }

    UserAvatars {
        int UserAvatarId PK
        int UserId FK
        enum AvatarSource
        string ProviderId "Google, VKontakte и др."
        string OriginalUri
        string CachedUri "копия в нашем хранилище"
        bool IsActive
    }

    UserSubscriptions {
        int UserSubscriptionId PK
        int UserId FK
        int ProjectId FK
        int CharacterGroupId FK "ровно один из трёх"
        int CharacterId FK "ровно один из трёх"
        int ClaimId FK "ровно один из трёх"
        bool ClaimStatusChange
        bool Comments
        bool FieldChange
        bool MoneyOperation
        bool AccommodationChange
    }

    Users ||--|| UserAuthDetails : ""
    Users ||--|| UserExtras : ""
    Users ||--|| AllrpgUserDetails : ""
    Users ||--o{ UserExternalLogins : "уникально по UserId и Provider"
    Users ||--o{ UserAvatars : ""
    Users |o--o| UserAvatars : "SelectedAvatar"
    Users ||--o{ UserSubscriptions : ""
    Projects ||--o{ UserSubscriptions : ""
    CharacterGroups |o--o{ UserSubscriptions : ""
    Characters |o--o{ UserSubscriptions : ""
    Claims |o--o{ UserSubscriptions : ""
```

`UserSubscriptions` — подписка на события; ровно одна из трёх ссылок
(`CharacterGroupId`, `CharacterId`, `ClaimId`) должна быть заполнена, это проверяется
в `IValidatableObject`, а не в БД.

Два виртуальных пользователя зарезервированы по email: `payments@joinrpg.ru`
(онлайн-платежи) и `robot@joinrpg.ru` (фоновые джобы).

---

## Отдельные БД (PostgreSQL)

Это **не** часть основной БД: отдельные подключения, EF Core, свои миграции.
FK на `Users`/`Projects` тут нет физически — только id и индексы.

```mermaid
erDiagram
    NotificationMessages {
        int NotificationMessageId PK
        string Header "max 1024"
        string Body
        int InitiatorUserId "индекс, FK нет — другая БД"
        int RecipientUserId "индекс, FK нет — другая БД"
        string EntityReference "ссылка на проект, заявку и т.п."
        bool SkipSignature
        datetimeoffset CreatedAt
    }

    NotificationMessageChannels {
        int NotificationMessageChannelId PK
        int NotificationMessageId FK
        enum Channel "NotificationChannel: Email, Telegram и др."
        string ChannelSpecificValue "адрес в канале, max 1024"
        enum NotificationMessageStatus
        datetimeoffset SendAfter "не раньше этого момента следующая попытка"
        int Attempts
    }

    NotificationMessages ||--o{ NotificationMessageChannels : "по одному на канал"
```

```mermaid
erDiagram
    DailyJobRuns {
        int DailyJobRunId PK
        string JobName UK "уникально вместе с DayOfRun"
        date DayOfRun UK
        enum JobStatus "DailyJobStatus: Started, Succeed, Failed"
        string MachineName "имя пода, где запущена джоба"
    }
```

Очередь уведомлений (`JoinRpg.Dal.Notifications`) — [ADR003](adr003-notifications.md);
выборка следующего сообщения идёт сырым SQL с `LIMIT 1 FOR UPDATE SKIP LOCKED`.
Трекинг джоб (`JoinRpg.Dal.JobService`) — [ADR002](adr002-dailyjobs.md); уникальный индекс
`(JobName, DayOfRun)` и есть механизм «не чаще раза в сутки».

---

## Если диаграммы всё равно перегружены

Что можно убрать, по убыванию выигрыша:

1. **Аудиторские поля** — `CreatedAt` / `CreatedById` / `UpdatedAt` / `UpdatedById`
   есть у `Characters`, `CharacterGroups`, `GameReport2DTemplate`, и вместе с ними
   приходят 6 линий на `Users`. Их можно свернуть в одну фразу «у этих таблиц есть
   стандартный аудит» и не рисовать.
2. **Связи с `Projects`** — почти каждая таблица имеет `ProjectId`. Если договориться,
   что «всё, у чего есть `ProjectId`, принадлежит проекту», из доменных диаграмм уходит
   по 3–7 линий каждая.
3. **Связи с `Users`** — то же самое: ответственный мастер, автор, создатель.
   Оставить только смысловые (`Claims.PlayerUserId`, `Comments.AuthorUserId`).
4. **Флаги** — `ProjectAcls` это 11 булевых прав, `ProjectDetails` — два десятка
   переключателей. Их можно свернуть в одну строку `bool Can* "11 флагов прав"`.
5. **Таблицы связей m2m** — `PlotElementCharacters`, `KogdaIgraGameProjects` и прочие
   можно нарисовать как прямое `}o--o{` без промежуточной сущности.
6. **Служебные домены** — `AdvertisementLogEntries`, `GameReport2DTemplate`,
   `AllrpgUserDetails`, `ProjectItemTags` можно вообще не показывать: они почти
   не связаны с остальной схемой.

Минимальная полезная версия — обзорная карта плюс домены 2, 3 и 6
(поля, персонажи-заявки, финансы): в них живёт почти вся неочевидная логика.
