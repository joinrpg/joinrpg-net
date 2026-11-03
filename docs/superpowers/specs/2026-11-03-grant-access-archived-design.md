# Дизайн: доступ техподдержки в архивные проекты

Дата: 2026-11-03. Статус: предложено.

## Intent и зачем

Техподдержке (админу сайта) нужно разбирать обращения по архивным проектам. Сейчас
кнопка «Поставить себе доступ» (`AclController.ForceSet` → `IProjectAccessService.GrantFullAccess`)
на архивном проекте бросает `ProjectDeactivatedException`: `GrantFullAccess` внутри вызывает
общий `GrantAccess`, а тот идёт через `ChangeProjectProperties(..., ProjectActiveRequirement.AllowBlocked, ...)`
(ADR023). Кнопка в меню админа при этом показывается и в архиве — админ упирается в ошибку.

## Цели / нецели

- Цель: `GrantFullAccess` работает и над архивным проектом.
- Нецель: менять правила выдачи доступа для обычных мастеров — публичный `GrantAccess`
  в архивном проекте по-прежнему запрещён (тест `GrantAccess_ArchivedProject_Throws` остаётся).

## Подход (выбран пользователем)

Разрешить только админскому `GrantFullAccess`, не трогая `GrantAccess`:

1. В `ProjectAccessService` выделить приватное ядро с параметром `ProjectActiveRequirement`;
   публичный `GrantAccess` передаёт `AllowBlocked` (как сейчас), `GrantFullAccess` —
   `AllowArchivedOrBlocked`.
2. Тест: `GrantFullAccess` в архивном проекте под админом создаёт/реактивирует ACL
   и сохраняет изменения (`ProjectAccessServiceTest`, рядом с `GrantAccess_ArchivedProject_Throws`).
3. Правка ADR023: в таблице требований уточнить, что выдача прав мастерам — `AllowBlocked`,
   кроме `GrantFullAccess` админа сайта (`AllowArchivedOrBlocked`) — для техподдержки.

UI менять не нужно: кнопка «Поставить себе доступ» уже видна админу в архивных проектах.

## Открыто исполнителю

Форма факторинга ядра (отдельный метод vs optional-параметр) — по месту, в стиле файла.
