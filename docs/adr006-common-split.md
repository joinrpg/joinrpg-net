# Разделение JoinRpg.PrimitiveTypes на общие и доменные типы

Проблема:
==
Проект `JoinRpg.PrimitiveTypes` содержит смесь общих примитивных типов (не зависящих от доменной логики JoinRpg) и доменных типов (специфичных для JoinRpg). Это создает ненужные зависимости: проекты, которым нужны только общие типы (например, `Email`, `UserIdentification`), вынуждены ссылаться на весь доменный контекст.

Решение:
==
Разделить проект на два:
- **`JoinRpg.Common.PrimitiveTypes`** – общие примитивные типы, не зависящие от бизнес-логики JoinRpg
- **`JoinRpg.DomainTypes`** – доменные типы, специфичные для JoinRpg

### Принципы разделения

**Общие типы** – не зависят от `ProjectIdentification`, `IProjectEntityId`, `LinkType` или любой доменной логики JoinRpg.
**Доменные типы** – зависят от доменной логики: проекты, заявки, персонажи, сюжеты, форумы, уведомления, права доступа.

Подробности
==

## Структура `JoinRpg.Common.PrimitiveTypes`

```
src/JoinRpg.Common.PrimitiveTypes/
├── SingleValueType.cs
├── Users/
│   ├── Email.cs
│   ├── VkId.cs
│   ├── TelegramId.cs
│   ├── UserFullName.cs
│   ├── UserDisplayName.cs
│   ├── UserInfoHeader.cs
│   ├── UserIdentification.cs
│   └── AvatarIdentification.cs
├── AvatarInfo.cs
├── KogdaIgraIdentification.cs
└── IdentificationParseHelper.cs
```

## Структура `JoinRpg.DomainTypes`

```
src/JoinRpg.DomainTypes/
├── Users/
│   ├── UserEnums.cs
│   ├── SubscriptionOptions.cs
│   └── UserInfo.cs
├── Characters/
|   ├── Claims/
│   │   ├── ClaimIdentification.cs
│   │   ├── ClaimCommentIdentification.cs
│   │   ├── ClaimEnums.cs
│   │   ├── CommentExtraAction.cs
│   │   ├── CaptainAccessRule.cs
│   │   └── Finances/
│   │       ├── ClaimBalance.cs
│   │       └── FinanceOperationIdentification.cs
│   ├── CharacterIdentification.cs
│   ├── CharacterGroupIdentification.cs
│   ├── CharacterTypeInfo.cs
│   ├── Problems.cs
│   └── FieldWithValue.cs
├── ProjectMetadata/
│   ├── ProjectIdentification.cs
│   ├── ProjectName.cs
│   ├── ProjectInfo.cs
│   ├── ProjectDetails.cs
│   ├── ProjectShortInfo.cs
│   ├── ProjectPersonalizedInfo.cs
│   ├── ProjectMasterInfo.cs
│   ├── ProjectFieldInfo.cs
│   ├── ProjectFieldVariant.cs
│   ├── ProjectFieldSettings.cs
│   ├── ProjectFieldType.cs
│   ├── ProjectFieldTypeHelper.cs
│   ├── ProjectFieldVisibility.cs
│   ├── ProjectLifecycleStatus.cs
│   ├── ProjectCheckInSettings.cs
│   ├── ProjectCloneSettings.cs
│   ├── ProjectScheduleSettings.cs
│   ├── TimeSlotFieldVariant.cs
│   ├── TimeSlotOptions.cs
│   ├── KogdaIgraGameData.cs
│   ├── Exceptions.cs
│   ├── ProjectFieldIdentification.cs
│   ├── ProjectFieldVariantIdentification.cs
│   └── Payments/
│       ├── PaymentTypeIdentification.cs
│       ├── PaymentTypeInfo.cs
│       ├── PaymentTypeKind.cs
│       └── ProjectFinanceSettings.cs
├── Forums/
│   ├── ForumThreadIdentification.cs
│   └── ForumCommentIdentification.cs
├── Plots/
│   ├── PlotFolderId.cs
│   ├── PlotElementIdentification.cs
│   ├── PlotVersionIdentification.cs
│   └── PlotElementType.cs
├── Notifications/
│   ├── NotificationId.cs
│   ├── NotificationEnums.cs
│   ├── NotificationEvent.cs
│   └── NotificationMessage.cs
├── Access/
│   ├── AccessArguments.cs
│   ├── Permission.cs
│   └── PlotAccessArguments.cs
├── Interfaces/
│   ├── IProjectEntityId.cs
│   ├── IProjectEntityWithId.cs
│   ├── ILinkable.cs
│   └── ILinkableWithName.cs
├── LinkType.cs
├── Targets.cs
└── ProjectEntityIdParser.cs
```

## Тестовые проекты

### `JoinRpg.Common.PrimitiveTypes.Test`
- `EmailParseTest.cs`
- `TelegramIdParseTest.cs`
- `UserIdentifiersParseTest.cs`
- `GlobalUsings.cs` (скопировать)
- Добавить тесты для `UserIdentification`, `AvatarIdentification`, `KogdaIgraIdentification`, `SingleValueType`

### `JoinRpg.DomainTypes.Test`
- `ProjectIdParseTest.cs`
- `PlotIdParseTest.cs`
- `IdentificationParseTest.cs`
- `IdentificationCommonTest.cs`
- `JsonRoundTripTests.cs`
- `EnumVerifyTest.cs`
- `GlobalUsings.cs` (скопировать)
- `IdentificationDataSource.cs` (адаптировать для своей сборки)
- `ProjectIdDataSource.cs` (адаптировать для своей сборки)

## Зависимости

- `JoinRpg.DomainTypes` ссылается на `JoinRpg.Common.PrimitiveTypes`
- Проекты, использующие только общие типы, ссылаются на `JoinRpg.Common.PrimitiveTypes`
- Проекты, использующие доменные типы, ссылаются на `JoinRpg.DomainTypes`


## Шаги выполнения

1. Создать проекты `JoinRpg.Common.PrimitiveTypes` и `JoinRpg.DomainTypes`
2. Перенести файлы согласно структуре выше
3. Создать тестовые проекты и разделить тесты
4. Обновить зависимости во всех проектах
5. Удалить старый проект `JoinRpg.PrimitiveTypes`

## Последствия

- **Уменьшение связанности** – проекты, которым нужны только общие типы, не зависят от доменной логики
- **Повторное использование** – `JoinRpg.Common.PrimitiveTypes` может использоваться в других проектах вне JoinRpg
- **Улучшение поддерживаемости** – четкие границы между общими и доменными типами
- **Временные затраты** – требуется обновление множества проектов и тестов

## Статус

Предложен, ожидает утверждения.

---
*Создано: 01.05.2026*  
*Автор: opencode*
