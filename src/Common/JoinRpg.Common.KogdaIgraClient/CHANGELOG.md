# Изменения JoinRpg.Common.KogdaIgraClient

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

## 2026.9.533 — 2026-10-29

### Ломающие изменения
- `KogdaIgraGameInfo`: новый последний позиционный параметр `IsDeleted` — игра удалена на КогдаИгре. Переход: в вызовы конструктора и позиционную деконструкцию добавить `IsDeleted`. ([#5966](https://github.com/joinrpg/joinrpg-net/pull/5966))
- `GetGameInfo`: на пустом и битом ответе — `KogdaIgraEmptyResponseException` и `KogdaIgraParseException` вместо `JsonException` и `Exception`. Переход: вместо `JsonException` ловить исключения клиента. ([#5977](https://github.com/joinrpg/joinrpg-net/pull/5977))

### Добавлено
- Пакет: описание и адрес репозитория в метаданных, README дополнен, CHANGELOG.md. ([#5948](https://github.com/joinrpg/joinrpg-net/pull/5948), [#5953](https://github.com/joinrpg/joinrpg-net/pull/5953))

## 2026.9.438

### Изменено
- Зависимости `Microsoft.Extensions.*` — 10.0.14. ([#5868](https://github.com/joinrpg/joinrpg-net/pull/5868))

## 2026.9.363

Видимых изменений нет.

Более ранние версии — [история коммитов](https://github.com/joinrpg/joinrpg-net/commits/master/src/Common/JoinRpg.Common.KogdaIgraClient).
