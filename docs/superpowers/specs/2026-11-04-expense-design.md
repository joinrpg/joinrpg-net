# Расходы проекта — дизайн

Дата: 2026-11-04
Статус: предложено (ожидает одобрения)

## Зачем

В проектах joinrpg деньги сейчас учитываются только в одну сторону: взносы игроков
(`FinanceOperation`, привязан к заявке). Организаторам нужно учитывать и затраты
(аренда базы, закупка антуража, прокат инвентаря — пример дерева категорий:
«Аренда базы\Жилые корпуса», «Антураж\Оружейка\Макеты»),
планировать их и согласовывать с финансовым администратором.

## Цели

- Сущность «Расход» в рамках проекта: план (количество × цена) и факт (количество × цена, null пока не совершён).
- Согласование расхода финансовым администратором: состояние «на рассмотрении / одобрен / отклонён» + история (кто/когда). Одобрять можно и плановый, и фактический расход.
- Иерархические категории расходов; у расхода снапшот пути — ссылки на все Id категорий от корня до листа.
- Расход привязан к мастеру, его понёсшему; одобренный факт уменьшает «Денег на руках» этого мастера,
  плановые расходы показаны отдельной колонкой «Запланировано» в балансах мастеров.
- Общий грид расходов проекта и грид «мои расходы» на странице «Мои деньги».
- Учёт расходов в финансовой сводке проекта.

## Не-цели

- Расходы не связаны с заявками и взносами (на расходы не пишется `FinanceOperation`).
- Не делаем бюджетные лимиты, уведомления, экспорт (если не понадобится по ходу).

## Анти-цели

- Не расширять `FinanceOperation` новыми типами — он жёстко привязан к заявке и его модерация не про количество × цену.
- Не выводить русские строки из сервисного/доменного слоя (все UI-строки — в Web-слое, на русском).

## Принятые решения (обсуждено)

1. **Scope — проект.** `Expense` и `ExpenseCategory` — `IProjectEntity`, права через `ProjectAcl`, всё внутри проекта, как остальные сущности.
2. **Одобрение — три-стейт + история.** Состояния `Proposed / Approved / Rejected`; каждый переход фиксируется (пользователь, время), хранится история.
3. **Подход — отдельная ветка сущностей, UI на Blazor-островах.** Бэкенд — новые сущности по конвенциям репозитория; гриды и формы — Blazor-компоненты (ADR001), встроенные в MVC-страницы через `render-mode`.
4. **Привязка к мастеру и балансы.** `Expense.MasterId` — мастер, понёсший расход: по умолчанию автор,
   при создании/редактировании сменяется выпадающим списком мастеров проекта. Одобренный факт
   вычитается из «Денег на руках» понёсшего мастера (колонка «Расходы» в балансах, заготовка
   `ExpensesBalance` в `MasterBalanceViewModel`), плановые и ещё не одобренные расходы — колонкой
   «Запланировано» (`ActualCost ?? PlannedCost` по неотклонённым, не входящим в «Расходы»;
   на итог не влияют). Решение @leotsarev, 2026-11-05.

## Отвергнутые альтернативы

- **Расширить `FinanceOperation`.** Отвергнуто: обязательный `ClaimId`, валидация по типам операций, модель «сумма» вместо «количество × цена» — пришлось бы ломать существующее.
- **UI на MVC.** Отвергнуто: пользователь выбрал Blazor; к тому же интерактивные гриды с фильтрами на Blazor проще.

## Как строим

### DataModel (`JoinRpg.DataModel`, namespace `JoinRpg.DataModel.Finances`)

```csharp
public class ExpenseCategory : IProjectEntity, IDeletableSubEntity
{
    public int ExpenseCategoryId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public required string Name { get; set; }          // данные, вводимые пользователем
    public int? ParentCategoryId { get; set; }
    public virtual ExpenseCategory? ParentCategory { get; set; }
    public IntList ParentCategoriesImpl { get; set; } = new(); // путь Id от корня, паттерн CharacterGroup.ParentGroupsImpl
    // IDeletableSubEntity: IsActive + CanBePermanentlyDeleted
    public bool IsActive { get; set; } = true;   // операций удаления на первом этапе нет
    public bool CanBePermanentlyDeleted => /* нет дочерних категорий и расходов */;
}
```

```csharp
public class Expense : IProjectEntity, ICreatedUpdatedTrackedForEntity
{
    public int ExpenseId { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int MasterId { get; set; }             // мастер, понёсший расход (по умолчанию — автор, сменяемый в форме)
    public virtual User Master { get; set; } = null!;
    public int CategoryId { get; set; }
    public virtual ExpenseCategory Category { get; set; }
    public IntList CategoryPathImpl { get; set; } = new(); // снапшот всех Id категорий пути на момент создания/смены категории

    public int PlannedQuantity { get; set; }      // звонок: целые единицы; дробные количества, если понадобятся, — отдельной задачей
    public int PlannedPrice { get; set; }         // звонок: цена за единицу в рублях (int), как везде в FinanceOperation.MoneyAmount
    public int? ActualQuantity { get; set; }      // null => расход только спланирован
    public int? ActualPrice { get; set; }
    public DateTime? SpentAt { get; set; }        // дата, когда деньги реально потрачены; вводится при заполнении факта

    public ExpenseState State { get; set; }       // Proposed / Approved / Rejected
    public int CreatedById { get; set; }
    public virtual User CreatedBy { get; set; } = null!;
    // ICreatedUpdatedTrackedForEntity требует также UpdatedById
    public int UpdatedById { get; set; }
    public virtual User UpdatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int PlannedCost => PlannedQuantity * PlannedPrice;
    public int? ActualCost => ActualQuantity * ActualPrice; // null, если факт не заполнен
}

// enum ExpenseState в DataModel не дублируется — обе сущности используют JoinRpg.DomainTypes.Finances.ExpenseState напрямую
// (референс DataModel → DomainTypes уже есть, прецедент — enum'ы в FinanceOperation/PaymentType); в Web — View-копия ExpenseStateView

public class ExpenseApproval
{
    public long ExpenseApprovalId { get; set; }
    public int ExpenseId { get; set; }
    public virtual Expense Expense { get; set; }
    public ExpenseState State { get; set; }       // состояние, в которое перевели
    public int ApprovedById { get; set; }         // финансовый администратор
    public DateTime Timestamp { get; set; }
    public string? Comment { get; set; }
}
```

Плюс `DbSet`-ы в `MyDbContext`, миграция EF и обновление `docs/db-schema.md` (её сверяет CI-тест `DbSchemaDocumentationScenario` — прогонять через `--filter` при изменении схемы).

### Слои

По [docs/structure.md](../../structure.md) (уточнить детали при планировании):

- **DomainTypes** — `ExpenseIdentification`, `ExpenseCategoryIdentification` (`[TypedEntityId]`-record'ы `: IProjectEntityId`, по образцу `CharacterIdentification`): сущности DataModel в контрактах сервисов и Web-слоя не используются — передаём идентификаторы и DTO. Плюс доменные типы данных (паттерн `RoomInfo`/`AdvertisementChannelInfo`): `ExpenseInfo` (все поля расхода + вычисляемые `PlannedCost`/`ActualCost`), `ExpenseCategoryInfo` (`CategoryId`, `Name`, `ParentCategoryId`, `IsActive`), `ExpenseCategoryBriefInfo` (сегмент снапшота пути) и единственный `enum ExpenseState` — DataModel использует его напрямую (референс уже есть), в Web — View-копия `ExpenseStateView` с Display-именами.
- **DataModel** — сущности выше.
- **Репозитории** — интерфейсы в `JoinRpg.Data.Interfaces`, реализация в `JoinRpg.Dal.Impl` (запросы через LINQ + LinqKit, [docs/linq-queries.md](../../linq-queries.md)): список расходов проекта, мои расходы, дерево категорий. Репозиторий возвращает доменные `*Info`-типы, не DataModel; преобразование — в internal-маппере в Dal (паттерн `RoomCategoryPlanMapper`, ADR018), покрытом юнит-тестом без БД.
- **Сервисы** — `IExpenseService` в `JoinRpg.Services.Interfaces`, реализация в `JoinRpg.Services.Impl` (регистрация в `AddJoinDomainServices`, MS DI): CRUD расхода, заполнение факта, `Approve/Reject` с записью в историю, разрешение пути категории (find-or-create по сегментам). Проверки прав: создавать/править — любой мастер (`MasterAuthorize`), одобрять — `ProjectAcl.CanManageMoney`. Сигнатуры — на идентификаторах DomainTypes, не на сущностях DataModel и не на голых int.

  **Машина состояний одобрения (решено):** состояния `Proposed / Approved / Rejected`. Любая правка расхода (в т.ч. заполнение факта) сбрасывает состояние в `Proposed` — одобрение нужно заново. Переходы `Approve`/`Reject` доступны из любого состояния, каждый переход пишется в `ExpenseApproval`. Самоодобрение своего расхода разрешено, если у пользователя есть `CanManageMoney` (как в модерации `FinanceOperation`).
- **Web (Blazor)** — новый проект `JoinRpg.Web.Finances` (+ `.Test`) по образцу `JoinRpg.Web.AdminTools` (назван по области UI, как `Web.Claims`/`Web.Plots`: расходы — первые компоненты раздела «Финансы», будущие финансовые острова лягут сюда же; фиче-папка внутри — `Expenses/`): компоненты-гриды и формы. Компоненты размещаются в Razor-страницах/вьюхах портала через `<component ... render-mode="WebAssemblyPrerendered" />` (пример: `JoinRpg.Portal/Pages/Admin/ComponentTestPage.cshtml`), пререндер на сервере, интерактив на клиенте. Интерактивные WASM-острова не ходят в сервисы напрямую: нужен слой typed HttpClients в `JoinRpg.Blazor.Client/ApiClients`, бьющий в `/webapi/*`-контроллеры портала (пример: `JoinRpg.Portal/Controllers/WebApi/ClaimListController.cs`), плюс референсы нового проекта из Portal и Blazor.Client.

### UI

Все строки — на русском, в Web-слое.

1. **«Финансы → Расходы»** (пункт в меню мастера рядом с «Мои деньги»): общий грид расходов проекта — колонки: название, категория (путь «Аренда базы\Жилые корпуса»), плановая стоимость, фактическая стоимость, состояние, мастер (понёсший), кто создал; фильтры по категории и состоянию. Кнопки: создать, править, заполнить факт, одобрить/отклонить (последние — только для `CanManageMoney`).
2. **Страница «Мои деньги»** (`FinancesController.ByMaster`, MVC): под существующим блоком — Blazor-остров «Мои расходы»: расходы, понёсшие этим мастером (`MasterId`), с созданием и заполнением факта прямо из грида. Поле «Мастер» в форме по умолчанию — текущий пользователь, но сменяется выпадающим списком мастеров проекта (можно внести расход за другого мастера).
3. **Категория — текстовое поле, а не отдельное управление деревом.** В форме расхода — одно поле «Категория» для полного пути через «\» («Аренда базы\Жилые корпуса») с автодополнением по существующим путям. Сервис разбивает путь на сегменты и делает find-or-create (сравнение сегментов без учёта регистра внутри одного родителя), затем записывает у расхода `CategoryId` и снапшот пути `CategoryPathImpl`. Новую категорию так может неявно создать любой мастер, редактирующий расход. Явных операций с деревом (переименовать/переместить/удалить) на первом этапе нет ни в UI, ни в API — появятся позже, если дерево потребует ухода.
4. **Сводка** (`FinancesController.MoneySummary`): блок «Расходы» — итоги по дереву категорий, план/факт, разбивка одобрено / на рассмотрении. Одобренный факт показывается строкой «Потрачено» и вычитается из «Итого остатка» сводки; план и неодобренное — только информативно. **Балансы мастеров учитывают расходы:** из «Денег на руках» (`MasterBalanceViewModel.Total`) понёсшего мастера вычитается одобренный факт (колонка «Расходы», заполняется существующая заготовка `ExpensesBalance`), рядом колонка «Запланировано» — сумма `ActualCost ?? PlannedCost` по неотклонённым расходам мастера, не входящим в «Расходы» (план без факта плюс софактовавшие, но не одобренные; на итог не влияет). Решено, см. «Принятые решения» №4.

## Что оставлено исполнителю (мелочи)

- Точные сигнатуры репозиториев и сервиса, разбиение на компоненты.
- Единые компоненты из `JoinRpg.Common.WebComponents` по возможности переиспользовать.
- Юнит-тесты: доменная логика расчёта стоимостей, переходы состояний одобрения (в т.ч. «не финансовый админ не может одобрить»), find-or-create пути категории (без дублей, регистронезависимо, корректные `ParentCategoriesImpl`). Тестовый проект — `JoinRpg.Services.Impl.Test` / `JoinRpg.Domain.Test`, новый `JoinRpg.Web.Finances.Test` при необходимости.

## Открытые вопросы (звонки автора)

1. Деньги: количество и цена — целые (int, рубли), как принято в `FinanceOperation`. Если нужны копейки/дробные количества — скажите, закладываем decimal сразу.

(Вопросы про вычитание одобренного факта из сводки и про «мои расходы» решены — см. «Принятые решения» №4 и UI п.2: «мои расходы» = понёсшие я, `MasterId`.)

## Решено после builder-check (2026-11-04)

- Поле `SpentAt` («дата расхода») — добавляем в DataModel (согласовано с @leotsarev в ходе обсуждения).
- Правка расхода сбрасывает одобрение в `Proposed`; переходы Approve/Reject из любого состояния, история в `ExpenseApproval`; самоодобрение разрешено при наличии `CanManageMoney`.
- Исправлено по коду: `IDeletableSubEntity` требует `IsActive`/`CanBePermanentlyDeleted`; `ICreatedUpdatedTrackedForEntity` требует `UpdatedById`; добавлен слой ApiClients/`/webapi/*` для интерактивных островов; регистрация сервисов — в `AddJoinDomainServices` (MS DI), Autofac-регистрация устарела (`Services.cs` помечен «ничего нового сюда не добавлять»).
- Отдельная страница управления категориями не делается: категория вводится текстовым полем полного пути через «\» в форме расхода, сервис find-or-create'ит сегменты (решение @leotsarev, 2026-11-05).
- Расход привязан к мастеру, его понёсшему (`MasterId`; по умолчанию — автор, сменяется выпадающим списком в форме). Одобренный факт вычитается из «Денег на руках» понёсшего мастера (колонка «Расходы», заготовка `ExpensesBalance`), плановые расходы без факта — колонка «Запланировано» (на итог не влияет). «Мои расходы» = понёсшие я. (решение @leotsarev, 2026-11-05).
