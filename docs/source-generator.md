# Правка генератора типизированных Id

Что генератор делает для потребителя — в
[его README](../src/Common/JoinRpg.Common.PrimitiveTypes.SourceGenerator/README.md), замысел
`[TypedStringValue]` — в [ADR007](adr007-string-value.md) (проверку в конструкторе так и не
сделали). Здесь — как генератор менять.

## Правка генератора — это два PR и публикация

Генератор подключён в `JoinRpg.Common.PrimitiveTypes` и `JoinRpg.DomainTypes` пакетом из NuGet
(`PackageReference`, версия в `Directory.Packages.props`), а не ссылкой на проект — иначе мешает
language server. Поэтому правка генератора сама по себе ни на что не влияет.

1. **PR с правкой.** Генератор плюс то, на что ссылается сгенерированный код (конвертеры и т. п. в
   `JoinRpg.Common.PrimitiveTypes`). Тесты генератора (`JoinRpg.Common.PrimitiveTypes.SourceGenerator.Test`)
   компилируют сгенерированный код и вызывают его через рефлексию — так поведение проверяется без публикации.
   Изменился набор генерируемых членов — поправить README генератора.
2. **Публикация** — см. [NuGet-пакеты](nuget-packages.md#версия-и-публикация). Генератор и
   `JoinRpg.Common.PrimitiveTypes` выходят одним прогоном с одной версией.
3. **PR с бампом** версии в `Directory.Packages.props` и с кодом, который пользуется новой возможностью.

Проверить правку на настоящих типах до публикации: временно (не коммитить) заменить обе
`PackageReference` на
`<ProjectReference Include="...SourceGenerator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`
и прогнать юнит-тесты.

## Версия Roslyn

`Microsoft.CodeAnalysis.CSharp` в генераторе зафиксирован на 4.3.1 и не следует за
`Directory.Packages.props`: анализатор, собранный под более новый Roslyn, компилятор потребителя
молча не загружает. Причина подробно — в комментарии в `.csproj` генератора.

## Подводный камень в тестах генератора

`[TypeConverter]` и похожие атрибуты хранят тип строкой. Сборку, загруженную тестом из байтов
(`GeneratorTestHelper.EmitAndLoad`), `Type.GetType` по имени не находит, и атрибут молча не срабатывает.
Пример обхода — `AssemblyResolve` в `TypedEntityIdConfigurationBindingTests`.
