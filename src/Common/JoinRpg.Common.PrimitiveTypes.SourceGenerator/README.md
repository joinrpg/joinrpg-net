# JoinRpg.Common.PrimitiveTypes.SourceGenerator

Генераторы для `[TypedEntityId]` и `[TypedStringValue]` (см. [ADR007](../../../docs/adr007-string-value.md)).
Для типизированного Id генерирует разбор (`ISpanParsable`), `ToString`, сравнение, JSON-конвертер
(`TypedEntityIdJsonConverter<T>`) и `TypeConverter` (`TypedEntityIdTypeConverter<T>`) — последний нужен,
например, чтобы хранить Id прямо в опциях конфигурации.

## Правка генератора — это два PR и публикация

Генератор подключён в `JoinRpg.Common.PrimitiveTypes` и `JoinRpg.DomainTypes` пакетом из NuGet
(`PackageReference`, версия в `Directory.Packages.props`), а не ссылкой на проект — иначе мешает
language server. Поэтому правка генератора сама по себе ни на что не влияет.

1. **PR с правкой.** Генератор плюс то, на что ссылается сгенерированный код (конвертеры и т. п. в
   `JoinRpg.Common.PrimitiveTypes`). Тесты генератора (`JoinRpg.Common.PrimitiveTypes.SourceGenerator.Test`)
   компилируют сгенерированный код и вызывают его через рефлексию — так поведение проверяется без публикации.
2. **Публикация.** После влития вручную запустить `.github/workflows/nuget-publish.yml` с master. Он
   публикует генератор и `JoinRpg.Common.PrimitiveTypes` одним прогоном с одной версией. Номер версии —
   в логе job'а генератора (`...SourceGenerator.<версия>.nupkg`); на nuget.org пакет появляется через
   несколько минут.
3. **PR с бампом** версии в `Directory.Packages.props` и с кодом, который пользуется новой возможностью.

Проверить правку на настоящих типах до публикации: временно (не коммитить) заменить обе
`PackageReference` на
`<ProjectReference Include="...SourceGenerator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`
и прогнать юнит-тесты.

Внешние репозитории (bastilia-rating и др.) генератор не подключают — они получают уже скомпилированные
типы из `JoinRpg.Common.PrimitiveTypes`.

## Подводный камень в тестах генератора

`[TypeConverter]` и похожие атрибуты хранят тип строкой. Сборку, загруженную тестом из байтов
(`GeneratorTestHelper.EmitAndLoad`), `Type.GetType` по имени не находит, и атрибут молча не срабатывает.
Пример обхода — `AssemblyResolve` в `TypedEntityIdConfigurationBindingTests`.
