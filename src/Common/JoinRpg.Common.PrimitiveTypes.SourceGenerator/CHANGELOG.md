# Изменения JoinRpg.Common.PrimitiveTypes.SourceGenerator

Формат — по мотивам [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).

## Не выпущено

## 2026.9.533 — 2026-10-29

### Добавлено
- Пакет: README.md и CHANGELOG.md, лицензия MIT. ([#5948](https://github.com/joinrpg/joinrpg-net/pull/5948), [#5950](https://github.com/joinrpg/joinrpg-net/pull/5950))
- `[TypedEntityId]`, `[TypedStringValue]`: у сгенерированных членов есть XML-комментарии — видны в IntelliSense, CS1591 на них нет. ([#5982](https://github.com/joinrpg/joinrpg-net/pull/5982))

## 2026.9.438

### Добавлено
- `[TypedStringValue]`: константы `MinLength` и `MaxLength` из параметров атрибута. ([#5878](https://github.com/joinrpg/joinrpg-net/pull/5878))

## 2026.9.363

### Ломающие изменения
- `[TypedEntityId]`: генерирует `[TypeConverter]` на `TypedEntityIdTypeConverter<T>` из PrimitiveTypes 2026.9.363+ — тип читается биндером конфигурации. Переход: обновить `JoinRpg.Common.PrimitiveTypes` вместе с генератором и убрать свой `[TypeConverter]` с таких типов. ([#5787](https://github.com/joinrpg/joinrpg-net/pull/5787))

Более ранние версии — [история коммитов](https://github.com/joinrpg/joinrpg-net/commits/master/src/Common/JoinRpg.Common.PrimitiveTypes.SourceGenerator).
