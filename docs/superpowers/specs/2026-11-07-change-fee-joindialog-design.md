# ChangeFee → JoinDialog + WebComponents (краткий дизайн)

## Задача

Форма «Изменить вручную…» (смена базового взноса заявки) на странице заявки
(`ClaimFinanceOperations.cshtml`, модалка `#changeFeeModal`, кнопка в `_AdminFunctionsPartial.cshtml`)
переделывается с Bootstrap-модалки + MVC-формы на Blazor-компонент с `JoinDialog`
из `JoinRpg.Common.WebComponents`, по образцу `FinanceModerationButton` (#3366).

## Почему

Страница заявки уходит от Bootstrap-модалок и анонимных POST-форм к типизированным
Blazor-клиентам; FinanceModerationButton — установленный образец этого паттерна.

## Что делаем

1. **Контракт**: `IChangeFeeClient` в `JoinRpg.Web.Claims/Finance/`
   (`Task ChangeFee(ClaimIdentification claimId, ChangeFeeRequest request)`;
   `record ChangeFeeRequest(int FeeValue)`).
2. **Blazor-клиент**: `JoinRpg.Blazor.Client/ApiClients/ChangeFeeClient.cs`
   (CSRF-токен, `EnsureSuccessOrThrowProblem`), регистрация в `HttpClientRegistration`.
3. **Серверная реализация**: viewservice в `JoinRpg.WebPortal.Managers/Claims/`,
   регистрация в `Registration.cs` (как `FinanceModerationViewService`).
4. **WebApi-контроллер**: `JoinRpg.Portal/Controllers/WebApi/ChangeFeeController.cs`,
   роут `/webapi/{projectId}/claims/{claimId}/change-fee`, `[RequireMaster]`,
   вызывает `IFinanceService.ChangeFee`; ожидаемые отказы сервиса → `BadRequest` с текстом
   для мастера (как в FinanceModerationController).
5. **Компонент** `ChangeFeeButton.razor` в `JoinRpg.Web.Claims/Finance/`:
   кнопка XS (как соседние в таблице) → `JoinDialog` Caption «Изменение суммы взноса»,
   то же содержимое: инфо-алерт, при `HasFieldsWithFee` предупреждение, `EditForm`
   с полем суммы и `datalist` вариантов. Свой футер (как FinanceModerationButton):
   «Отмена» и «Изменить» (Info), диалог не закрывается при отказе сервера,
   ошибка показывается `JoinAlert`. После успеха `NavigationManager.Refresh(forceReload: true)`.
6. **View-модель параметров**: компонент принимает `ChangeFeeDialogView`-модель
   (ProjectId, ClaimId, BaseFee, FeeVariants, HasFieldsWithFee). Прокидывается
   через `FinanceOperationAdminFunctionsViewModel` (расширить полями) → `_AdminFunctionsPartial`.
7. **Удаляем**: модалку `#changeFeeModal` из `ClaimFinanceOperations.cshtml`,
   кнопку в `_AdminFunctionsPartial.cshtml` заменяем на `<component ... render-mode="WebAssemblyPrerendered">`,
   мёртвый `ClaimController.ChangeFee` (подтвердить через `find_references`).

## Не-цели

- Модалка `#markAsPreferential` не трогается (отдельная задача).
- Перевод оплаты, онлайн-оплата, подписки — не трогаются.

## Тесты

- bUnit-тест компонента в `JoinRpg.Web.Claims.Test` (по образцу `FinanceModerationButtonTest`):
  рендер, открытие диалога, отправка значения, показ ошибки сервера.
- Юнит-тест контроллера при наличии образцов (проверить наличие тестов WebApi-контроллеров).

## Проверка

`dotnet build`, `dotnet format --verify-no-changes --severity warn`,
`dotnet test` по затронутым юнит-проектам (не интеграционные).
