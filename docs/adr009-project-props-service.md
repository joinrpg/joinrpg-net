# ADR009. ProjectPropsService — единая точка изменения метаданных проекта

## Контекст

Метаданные проекта представлены иммутабельным `ProjectInfo`
(`JoinRpg.DomainTypes/ProjectMetadata/ProjectInfo.cs`), который собирается из EF-сущности
`Project` методом `ProjectMetadataRepository.CreateInfoFromProject`.

Любой сервис, меняющий что-то из `ProjectInfo` (настройки проекта, поля, группы, ACL),
повторяет один и тот же цикл: **загрузить `Project` → проверить права → мутировать сущность →
`SaveChanges`**. Из этого вытекают три проблемы:

1. **Дублирование.** Цикл load/check/mutate/save копируется в каждом сервисе. В `ProjectService`
   он частично вынесен в приватный `ChangeProjectProperties(projectId, Action<Project>)`, но это
   локальное решение одного сервиса.
2. **Рассинхрон `Project` ↔ `ProjectInfo`.** После мутации EF-сущности уже загруженный
   `ProjectInfo` (в т.ч. лежащий в per-request кэше `PerRequestCache`, у которого нет
   инвалидации) становится устаревшим. Нужна гарантия, что `Project` и `ProjectInfo` всегда
   согласованы — и до, и после сохранения.
3. **Неявность допустимости операции над неактивным проектом.** Сейчас каждый метод сам решает,
   можно ли менять закрытый проект (`SetPublishSettings` — можно, остальные — нет), и в сигнатуре
   это не видно. Как правило операция над неактивным проектом недопустима — это должно требоваться
   явно.

## Решение

### 1. `IProjectMetadataWriteRepository` (слой `JoinRpg.Data.Impl`)

Репозиторий загружает **трекаемый** `Project` со всеми связями и собирает из него согласованный
`ProjectInfo`, возвращая `IProjectMetadataUpdateHandle` — пару `(Project, ProjectInfo)`. Хэндл —
интерфейс (в `JoinRpg.Data.Interfaces`); его конкретная реализация спрятана внутри `Dal.Impl`, что
позволяет напрямую вызывать `CreateInfoFromProject` без проброса делегата через границу сборок, а в
тестах — подменять хэндл фейком. Хэндл умеет пересобрать `ProjectInfo` из текущего состояния
`Project` (`Refresh()`), давая снимок ПОСЛЕ мутации без второго запроса в БД. Конвертация —
существующий `internal static CreateInfoFromProject` (единственный источник истины
`Project` → `ProjectInfo`), переиспользуемый и в рантайме, и в тестах.

Важно: write-репозиторий **обязан** работать с тем же `DbContext`, через который потом вызывается
`SaveChanges`. Поэтому он не регистрируется в DI отдельным транзиентом (это дало бы другой
экземпляр `MyDbContext`), а выдаётся из `IUnitOfWork.GetProjectMetadataWriteRepository()` — как и
остальные репозитории записи. Иначе мутация трекается в одном контексте, а сохранение происходит в
другом, и изменения теряются (этот сценарий ловит интеграционный тест `AcceptInvitationScenario`).

### 2. `IProjectPropsService` (слой `JoinRpg.Services.Impl`)

Единственная точка изменения метаданных:

```csharp
Task<TResult> ChangeProjectProperties<TArgs, TResult>(
    ProjectIdentification projectId,
    Permission requiredPermission,
    ProjectActiveRequirement activeRequirement,
    TArgs arguments,
    Func<Project, ProjectInfo, TArgs, TResult> action,
    [CallerMemberName] string operationName = "");
```

(плюс перегрузка с `Action<Project, ProjectInfo, TArgs>` для операций без результата.)

Сервис централизует:

- **права** — `RequestMasterAccess(requiredPermission)` с bypass для админа;
- **активность** — явный параметр `ProjectActiveRequirement` (`MustBeActive` / `AllowInactive`),
  по умолчанию значения нет: вызывающий обязан указать намерение явно;
- **логирование** — имя операции (`[CallerMemberName]`) и её аргументы (`TArgs`) пишутся в лог
  **после** операции: `Information` при успехе, `Warning` (с исключением) при провале, после чего
  исключение пробрасывается дальше. Аргументы передаются явным параметром `arguments`, а не
  захватываются замыканием, чтобы их можно было залогировать структурно;
- исполнение мутации, `SaveChanges`;
- пересборку `ProjectInfo` (`handle.Refresh()`) и обновление per-request кэша (`PrimeCache`),
  устраняя рассинхрон.

Отдельного «системного» пути не нужно: фоновые джобы выполняются под роботом (`JobRunner`
подставляет `RobotUser` с его флагом `IsAdmin`), а у робота админские права — значит admin-bypass
проверки прав срабатывает сам собой. Так на `ChangeProjectProperties` переведён и
`CloseProjectAsStale` (из `ProjectPerformCloseJob`).

### 3. Нотификации

Сервис нотификации не диспатчит — это ответственность вызывающего **после** успешного `await`
(как `ProjectService.CloseProject` сегодня шлёт письмо через `MasterEmailService`). Для
diff-сценариев `ProjectPropsService` располагает снимками `ProjectInfo` ДО (`handle.ProjectInfo`)
и ПОСЛЕ (`handle.Refresh()`); при необходимости перегрузку можно расширить, вернув пару
(before, after), из которой вызывающий строит `NotificationEvent`
(`INotificationService.QueueNotification`, см. ADR про нотификации) — аналогично claim-флоу с
`FieldWithPreviousAndNewValue`.

## Последствия

- Единый чокпойнт для прав, согласованности `Project`/`ProjectInfo` и кэша.
- Конвертация `Project` → `ProjectInfo` переиспользуется в тестах: результат операции валидируется
  через пересобранный `ProjectInfo`, а не через сырую EF-сущность.
- Сервисный слой становится unit-тестируемым без БД: достаточно фейков
  `IProjectMetadataWriteRepository` (поверх `MockedProject`) и `IUnitOfWork`.
- Аудит (`MarkChanged`) централизуется в будущем для операций над под-сущностями (группы/поля);
  для настроек проекта помечать на уровне `Project` нечего — он не реализует
  `ICreatedUpdatedTrackedForEntity`.
- Миграция существующих сервисов на `IProjectPropsService` — постепенная. Первыми переведены
  группа settings-методов `ProjectService`, `CloseProject` и `CloseProjectAsStale` (последний — под
  роботом-админом; нотификации остаются в вызывающем). `IProjectPropsService` регистрируется явно в
  `Services.AddJoinDomainServices`.
