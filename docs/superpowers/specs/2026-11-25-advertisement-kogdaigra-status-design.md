# Реклама: не рекламировать проекты с не-Ок статусом игры на КогдаИгре

## Зачем

Статус игры с КогдаИгры уже парсится и хранится (`KogdaIgraGames.Status`,
см. [2026-11-25-kogdaigra-game-status-design.md](2026-11-25-kogdaigra-game-status-design.md)),
но при выборе кандидатов на рекламу не учитывается: отменённую или отложенную
игру мы сейчас спокойно рекламируем, если дата у неё в будущем.

## Цели

- В проверках кандидатов на рекламу (`AdvertisementJob.ValidateForAdvertisement`)
  учитывать статус карточки КогдаИгры: рекламируем только карточки со статусом
  `Ok` (0). NULL (статус неизвестен — удалённые игры и пр.) трактуем как «не Ок»
  и не рекламируем (решение @leotsarev).
- Если ближайшая будущая карточка проекта не Ок, но есть более поздняя будущая
  с Ок — рекламируем проект по ней (решение @leotsarev).

## Не-цели

- Не меняем выборку кандидатов в репозитории (`GetPublicProjectsOpenForHotRoleAdvertisement`
  и т.п.) — фильтр остаётся на уровне `ProjectDetails`, как и остальные проверки
  джобы.
- Не трогаем `KogdaIgraGame.Active` и синк с КогдаИгры.

## Подходы

1. **Фильтр по статусу в `ProjectDetails` (выбрано).** Свойство
   `NearestFutureKogdaIgraCard` выбирает ближайшую будущую активную карточку
   со статусом `Ok`. Свойство используется только рекламой
   (`AdvertisementJob`, `TelegramSingleHotRoleSender`), поэтому меняем
   семантику и переименовываем в `NearestFutureOkKogdaIgraCard`, чтобы имя
   не врало.
2. Отдельная проверка статуса в `AdvertisementJob` рядом с существующей —
   отклонено: тогда «ближайшая Ок-карточка» пришлось бы искать вторым
   проходом там же, дублируя логику выбора карточки.

## Части

### DomainTypes

- `KogdaIgraGameData` получает `KogdaIgraGameStatus? Status` (опциональный
  параметр в конец record — существующие конструкторы в тестах не ломаются).
- `ProjectDetails`: свойство переименовывается в `NearestFutureOkKogdaIgraCard`
  и дополнительно фильтрует по `Status == KogdaIgraGameStatus.Ok`.

### DAL

- `KogdaIgraRepository.TryConvert` маппит `(KogdaIgraGameStatus?)g.Status`
  в `KogdaIgraGameData`.

### Реклама (`JoinRpg.Services.Advertisement`)

- `AdvertisementJob.ValidateForAdvertisement` и дайджест используют
  переименованное свойство; текст лог-сообщения про отсутствие карточки
  дополняется «со статусом Ок».

### Тесты

- Юнит-тест на выбор карточки в `ProjectDetails`: ближайшая не-Ок карточка
  пропускается в пользу более поздней Ок; все не-Ок/NULL → null.
- Существующие тесты, конструирующие `KogdaIgraGameData`
  (`HotRoleAdvertisementMessageBuilderTests`, `AdminNotificationServiceImplTest`),
  продолжают компилироваться за счёт опционального параметра; где по смыслу
  нужен Ок-статус (проверки рекламы) — проставить явно.

## Открыто / на усмотрение исполнителя

- Точный текст лог-сообщений.
