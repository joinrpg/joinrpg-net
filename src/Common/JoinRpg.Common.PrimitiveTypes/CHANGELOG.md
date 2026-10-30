# Изменения JoinRpg.Common.PrimitiveTypes

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

## 2026.9.533 — 2026-10-29

### Ломающие изменения
- `UserInfoHeader`: обязательный параметр `AvatarIdentification? Avatar`. Переход: передавать id аватарки или `null`. ([#5905](https://github.com/joinrpg/joinrpg-net/pull/5905))

### Добавлено
- `Email`, `PhoneNumber`, `VkId`, `LiveJournalId`, `BornName`, `SurName`, `FatherName`, `PrefferedName`, `MarkdownString`: константы `MinLength` и `MaxLength`. ([#5885](https://github.com/joinrpg/joinrpg-net/pull/5885))
- Пакет: README.md и CHANGELOG.md, лицензия MIT. ([#5948](https://github.com/joinrpg/joinrpg-net/pull/5948), [#5950](https://github.com/joinrpg/joinrpg-net/pull/5950))

## 2026.9.438

### Добавлено
- `UserIdentification`, `AvatarIdentification`, `KogdaIgraIdentification`, `TelegramChatId`: атрибут `[TypeConverter]` — читаются биндером конфигурации из строки (собрано генератором 2026.9.363). ([#5790](https://github.com/joinrpg/joinrpg-net/pull/5790))

### Удалено
- Зависимость `Microsoft.Build.Tasks.Git`. ([#5856](https://github.com/joinrpg/joinrpg-net/pull/5856))

## 2026.9.363

### Добавлено
- `TypedEntityIdTypeConverter<T>`: разбор типизированного Id из строки. ([#5787](https://github.com/joinrpg/joinrpg-net/pull/5787))

Более ранние версии — [история коммитов](https://github.com/joinrpg/joinrpg-net/commits/master/src/Common/JoinRpg.Common.PrimitiveTypes).
