# Дизайн: ButtonPreset.Archive + JoinIconType.Archive

Дата: 2026-11-07. Статус: на согласовании.

## Намерение и зачем

В библиотеке `JoinRpg.Common.WebComponents` нет пресета кнопки для архивации объекта.
Добавляем его в библиотеку, чтобы места использования (отдельные задачи) могли
ссылаться на готовый пресет вместо ручной сборки кнопки из подписи, иконки и стиля.

## Цели / нецели

- Цель: пресет `ButtonPreset.Archive` с конфигурацией и новой иконкой `JoinIconType.Archive`.
- Не цель: использовать пресет в каком-либо UI сейчас — отдельная задача.
- Анти-цель: не переиспользовать глиф `circle-x` (Hide/Closed), чтобы иконки архива
  и «скрыть» можно было развести при смене иконочного набора.

## Ограничения

- Пакет `IsPackable=true`: публичный API фиксируется в `PublicAPI.Shipped.txt`
  (RS0016/RS0017 — прогон `pwsh scripts/Update-PublicApi.ps1`), изменение — в
  «Не выпущено» `CHANGELOG.md`.
- Иконочный набор — Tabler (MIT), исходники в `Icons/tabler`, спрайт собирается тестом
  `JoinIconSpriteTest` (пересборка через `JOINRPG_UPDATE_SPRITE=1`).
- Имена иконок Tabler не должны встречаться вне `JoinIconDefinitions` (проверяется тестом).
- Юнит-тест `JoinButtonPresetsTest.PresetShouldBeConfigured` требует конфигурацию
  для каждого значения `ButtonPreset`.
- UI-строки — на русском.

## Подходы

1. **Новая `JoinIconType.Archive` + пресет Archive (выбрано).** Скачать svg `archive`
   из Tabler, добавить значение enum, строчку в `JoinIconDefinitions`, пересобрать
   спрайт. Чистая семантика, иконки разойдутся при смене набора. Минус: чуть больше
   работы, нужно скачать svg.
2. Переиспользовать `JoinIconType.Closed` (`circle-x`). Быстрее, но архивация визуально
   совпадёт с «скрыть» — отклонено.
3. Полный набор «архив/разархивировать» (два пресета). Сейчас нужен только один —
   отклонено как избыточное.

## Решения (согласованы с заказчиком)

- Иконка: новая `JoinIconType.Archive`, глиф Tabler `archive`.
- Стиль кнопки: `VariationStyleEnum.Warning`.
- Подписи: «В архив» / «Архивируем...».
- Объём: только библиотека, без использования в UI.

## Что делаем

1. Скачать `archive.svg` из Tabler Icons (v3, MIT) в `src/Common/WebComponents/JoinRpg.Common.WebComponents/Icons/tabler/`.
2. `JoinIconType.cs`: значение `Archive` («Отправить в архив»).
3. `JoinIconDefinitions.cs`: `{ JoinIconType.Archive, "archive" }`.
4. `ButtonPreset.cs`: значение `Archive`.
5. `ButtonPresets.cs`: `{ ButtonPreset.Archive, new("В архив", "Архивируем...", JoinIconType.Archive, VariationStyleEnum.Warning) }`.
6. Пересобрать спрайт (`JOINRPG_UPDATE_SPRITE=1 dotnet test` на `JoinIconSpriteTest`).
7. `pwsh scripts/Update-PublicApi.ps1` — PublicAPI для обоих enum.
8. CHANGELOG.md пакета, «Не выпущено» → «Добавлено»: `ButtonPreset.Archive` и `JoinIconType.Archive`.
9. `dotnet build` + тесты `JoinRpg.Common.WebComponents.Test` и `JoinRpg.WebPortal.Models.Test`, `dotnet format --verify-no-changes --severity warn`.

На усмотрение исполнителя: порядок значений в enum (в конец, рядом с близкими по смыслу), точные формулировки док-комментариев.
