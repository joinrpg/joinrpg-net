# Дата первого открытия заявок (ClaimsOpenedDate)

## Зачем

В проекте нужно хранить дату, когда заявки были открыты в первый раз. Поле доступно в
`ProjectInfo` (слой доменных типов) и в БД (таблица `Projects`).

## Семантика (решено с заказчиком)

- Дата фиксируется **один раз** — при первом открытии заявок. Повторные открытия/закрытия её не меняют.
- Заполняется при переключении настроек: в `ProjectService.EditProject` при переходе
  `IsAcceptingClaims` из `false` в `true`, если поле ещё пустое — записывается текущее время.
- `NULL` означает: заявки ещё ни разу не открывались.

## Изменения

1. **DataModel**: `Project.ClaimsOpenedDate` (`DateTime?`), nullable. Согласовано с @leotsarev.
2. **EF-миграция**: добавить колонку `ClaimsOpenedDate` (nullable) с бэкфиллом одним SQL:
   - дата первой отправленной заявки проекта (`MIN(Claims.CreateDate)`);
   - если заявок нет, но `IsAcceptingClaims = 1` — дата выполнения миграции;
   - иначе `NULL`.
3. **DomainTypes**: в `ProjectClaimSettings` добавить `DateOnly? ClaimsOpenedDate`
   (рядом с `IsAcceptingClaims`). `ProjectInfo` собирается в `ProjectRepository` — пробросить поле туда.
4. **ProjectService.EditProject**: при `IsAcceptingClaims == true && ClaimsOpenedDate == null`
   установить `ClaimsOpenedDate = now` (по аналогии с `CreatedDate = ctx.Now.UtcDateTime`).

## Не делаем

- Не показываем дату в UI настроек (поле техническое, для будущего использования).
- Не обновляем дату при повторных открытиях и при подаче первой заявки.

## Проверка

- Юнит-тест: `EditProject` с включением приёма заявок заполняет `ClaimsOpenedDate`;
  повторное сохранение с включённым приёмом его не меняет; выключение не затирает.
- Миграция применяется на чистой БД; `DbSchemaDocumentationScenario` — при необходимости
  обновить `docs/db-schema.md`.
