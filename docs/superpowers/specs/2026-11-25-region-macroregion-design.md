# Region и MacroRegion: enum'ы регионов КогдаИгры и их отображение

Дата: 2026-11-25

## Контекст и зачем

JoinRpg импортирует игры с сайта «КогдаИгра» (kogda-igra.ru). У каждой игры там есть субрегион
(`sub_region_id`, например `1` = «Санкт-Петербург») и через него — макрорегион (раздел сайта:
«Северо-Запад», «Москва и Центральный регион» и т.д.). Сейчас в JoinRpg регионы — свободные строки
(`KogdaIgraGame.RegionName`), а `sub_region_id` даже не парсится из API. Задел под фильтрацию
по регионам уже есть (`AdvertisementScheduleInfo.RegionsFilter`, ADR010), но типизированных
регионов нет.

Задача — завести типизированные `Region` и `MacroRegion` в `JoinRpg.Common.PrimitiveTypes`,
методы взаимного преобразования, а в `JoinRpg.Common.WebComponents` — получение имён и
компонент «бейдж региона».

## Цели

- `enum Region` — все ~170 субрегионов из справочника КогдаИгры, значения = `sub_region_id`.
- `enum MacroRegion` — 11 макрорегионов, значения = `region_id` КогдаИгры.
- Конвертеры: `Region → MacroRegion` и `MacroRegion → IReadOnlyList<Region>`.
- В WebComponents: получение полного/короткого имени региона, имени макрорегиона.
- Компонент `RegionBadge`: короткое имя + tooltip с полным именем. Демо в WebComponentBook.

## Не-цели (в эту задачу НЕ входит)

- Парсинг `sub_region_id` из API КогдаИгры (`KogdaIgraClient`), изменение `KogdaIgraGameInfo`.
- Сохранение региона в БД, миграции, маппинг существующих строковых `RegionName`.
- Использование бейджа на страницах портала и реализация `RegionsFilter` (ADR010).

## Анти-цели

- Перенумерация значений enum «для красоты»: числовые значения — внешний контракт с КогдаИгрой.
- Русские строки в `JoinRpg.Common.PrimitiveTypes` (правило слоёв AGENTS.md; прецедент пакета —
  `CountHelper`/`DateRangeFormatter` принимают слова параметрами).

## Источник данных

Авторитетный справочник — [02-reference-data.sql](https://github.com/joinrpg/kogda-igra/blob/master/db/init/02-reference-data.sql)
репозитория `joinrpg/kogda-igra`: таблицы `ki_regions` (макрорегионы) и `ki_sub_regions`
(субрегионы: `sub_region_id`, `sub_region_name`, `sub_region_disp_name`, `region_id`).
Сверено со скрапингом живого сайта (62 активных субрегиона за 2024–2028) — расхождений по id нет.

Особенности данных КогдаИгры, которые переносим осознанно:

- Псевдо-регион `1 «Все игры»` — пункт меню, не регион; в `MacroRegion` не включаем.
- `region_id = 12` (области Казахстана, субрегионы 114–130) отсутствует в `ki_regions` —
  добавляем `MacroRegion.Kazakhstan = 12` сами (решение пользователя).
- «Франция» встречается дважды (156 и 172); 172 помечаем `[Obsolete]` (решение пользователя).
- У субрегионов 1/2 («Санкт-Петербург»/«Ленинградская область») и 3/4 («Москва»/«Московская
  область») одинаковые короткие имена («СПб», «Мск») — так в справочнике, оставляем.

## Подходы

Рассматривались варианты реализации конвертеров:

1. **Extension-методы со switch** — как существующие `ExpenseStateViewExtensions.ToView`,
   `KogdaIgraStatusLabel`. Явно, тестируемо, соответствует проекту. **Выбрано.**
2. Словарь, строимый рефлексией из атрибутов — меньше кода, но магия и рефлексия; отклонено.
3. Source generator — избыточно для статичной таблицы; отклонено.

Рассматривались места для имён (полное/короткое):

1. `[Display(Name=..., ShortName=...)]` на членах enum в PrimitiveTypes — атрибуты имеют оба
   свойства, всё в одном месте, но русские строки в Common-пакете. Отклонено пользователем.
2. **Имена в WebComponents** — UI-слой, правило локализации соблюдено; enum'ы в PrimitiveTypes
   остаются чистыми числами. **Выбрано пользователем.**

## Решения

### PrimitiveTypes (`src/Common/PrimitiveTypes/JoinRpg.Common.PrimitiveTypes/`)

Новые файлы (например, в папке `Regions/`):

- `Region.cs` — `public enum Region`, 170 членов, значения = `sub_region_id` (таблица ниже).
  Члены по возрастанию id. Имена членов — транслит **полного** имени КогдаИгры
  (`SanktPeterburg = 1`, `SverdlovskayaOblast = 24`, `Tatarstan = 17`); единственное исключение —
  `FrantsiyaObsolete` (172, с `[Obsolete("Дубликат Frantsiya (156) в справочнике КогдаИгры")]`).
  Над enum'ами — комментарий со ссылкой на `02-reference-data.sql` и датой снятия снапшота
  (2026-11-25); расхождение с живой КогдаИгрой чинится отдельными задачами, не молчаливой правкой.
- `MacroRegion.cs` — `public enum MacroRegion`:

  | Значение | Член | Имя (в WebComponents) |
  |---:|---|---|
  | 2 | `NorthWest` | Северо-Запад |
  | 3 | `MoscowAndCenter` | Москва и Центральный регион |
  | 5 | `Ural` | Урал |
  | 6 | `Siberia` | Сибирь |
  | 7 | `South` | Юг России |
  | 8 | `FarEast` | Дальний Восток |
  | 9 | `Volga` | Поволжье |
  | 10 | `Ukraine` | Украина |
  | 11 | `Belarus` | Беларусь |
  | 12 | `Kazakhstan` | Казахстан |
  | 13 | `Foreign` | Зарубежье |

- `RegionMapping.cs` — `public static class RegionMapping`:
  - `public static MacroRegion ToMacroRegion(this Region region)` — switch по всем 170 значениям,
    на неизвестном — `ArgumentOutOfRangeException` (прецедент `ExpenseStateViewExtensions`).
    Внимание: ссылка на `FrantsiyaObsolete` в switch даст warning CS0618 — использовать
    `(Region)172` или локальный `#pragma warning disable CS0618`.
  - `public static IReadOnlyList<Region> GetRegions(this MacroRegion macroRegion)` — кэшированные
    приватные массивы наружу отдаются как `IReadOnlyList<Region>` (публичный изменяемый массив
    в контракте NuGet-пакета не отдаём); на неизвестном — `ArgumentOutOfRangeException`.

JSON-сериализация — по умолчанию (числом), специальной инфраструктуры не добавляем.

Члена `None = 0` в enum'ах **нет** (решение пользователя): «регион не задан» выражается через
`Region?`/`MacroRegion?`. `default(Region)` — не валидное значение, конвертеры на нём бросают
`ArgumentOutOfRangeException`.

### WebComponents (`src/Common/WebComponents/JoinRpg.Common.WebComponents/`)

- `RegionDisplay.cs` — `public static class RegionDisplay`:
  - `public static string GetName(this Region region)` — полное имя («Санкт-Петербург»);
  - `public static string GetShortName(this Region region)` — короткое имя («СПб»);
  - `public static string GetName(this MacroRegion macroRegion)` — имя макрорегиона
    («Северо-Запад»). Короткого имени у макрорегиона нет.
  Реализация — switch/статический словарь по таблице ниже; на неизвестном значении —
  `ArgumentOutOfRangeException`. Строки в UI-слое, `TODO[Localize]` не нужен.
- `RegionBadge.razor` — обёртка над существующим `JoinBadge` (tooltip уже встроен в него):
  параметры `Region` (обязательный), `Variation` (`VariationStyleEnum`, по умолчанию `Info`),
  `Size` (`SizeStyleEnum`). Текст бейджа — `GetShortName()`, `Title` тултипа — `GetName()`
  (только полное имя региона, без макрорегиона).
- Демо-страница `RegionBadgeDemo.razor` в `JoinRpg.Common.WebComponentBook.Client/Shared` +
  регистрация в `AllDemos.razor` (образец — `JoinBadgeDemo.razor`).

## Тестирование

- `JoinRpg.Common.PrimitiveTypes.Test`:
  - для каждого значения `Region` из `Enum.GetValues` (включая `(Region)172`) `ToMacroRegion()`
    не бросает, а для каждого `MacroRegion` — `GetRegions()`;
  - round-trip: для каждого `Region r` → `r ∈ r.ToMacroRegion().GetRegions()`;
  - снапшот числовых значений обоих enum'ов по образцу `EnumVerifyTest` (образец живёт в
    `JoinRpg.DomainTypes.Test`, сюда переносим подход) — значения являются внешним контрактом
    с КогдаИгрой, случайная перенумерация должна ломать тест.
- `JoinRpg.Common.WebComponents.Test` (bUnit, образец `JoinBadgeTooltipContentTest`):
  - `RegionBadge` рендерит короткое имя и tooltip с полным именем;
  - теория: `GetName`/`GetShortName` не пусты для всех членов обоих enum'ов.

## Сопутствующие действия (docs/nuget-packages.md)

- Записи в раздел «Не выпущено» CHANGELOG обоих пакетов (`JoinRpg.Common.PrimitiveTypes`,
  `JoinRpg.Common.WebComponents`).
- `pwsh scripts/Update-PublicApi.ps1` — новые публичные типы (PublicAPI.Unshipped.txt).
- README пакета PrimitiveTypes — добавить новые основные типы; README WebComponents — новый
  компонент (если там ведётся список компонентов).
- XML-доки: CS1591 глобально подавлен в `Directory.Build.props`, обязательных доков на 170 членов
  нет; краткий XML-doc на сами enum'ы и публичные методы всё равно написать.
- `dotnet format` перед коммитом.

## Что оставлено реализатору

- Точные имена членов enum: таблица ниже — источник истины по числам и русским именам;
  C#-имена можно слегка улучшать (читаемость транслита), не меняя чисел.
- Формат XML-доков на членах enum.
- Значение `Variation` по умолчанию у бейджа.

## Таблица субрегионов (источник истины)

Все 170 записей `ki_sub_regions` из 02-reference-data.sql. Значение = `sub_region_id`.

| Значение | Член enum | Полное имя | Короткое имя | MacroRegion |

|---:|---|---|---|---|

| 1 | `SanktPeterburg` | Санкт-Петербург | СПб | `NorthWest` |

| 2 | `LeningradskayaOblast` | Ленинградская область | СПб | `NorthWest` |

| 3 | `Moskva` | Москва | Мск | `MoscowAndCenter` |

| 4 | `MoskovskayaOblast` | Московская область | Мск | `MoscowAndCenter` |

| 5 | `TverskayaOblast` | Тверская область | Тверь | `MoscowAndCenter` |

| 6 | `VladimirskayaOblast` | Владимирская область | Влад | `MoscowAndCenter` |

| 7 | `ArkhangelskayaOblast` | Архангельская область | Арх | `NorthWest` |

| 8 | `NovgorodskayaOblast` | Новгородская область | Новг | `NorthWest` |

| 9 | `Kareliya` | Карелия | Карел | `NorthWest` |

| 10 | `Komi` | Коми | Коми | `NorthWest` |

| 11 | `VologodskayaOblast` | Вологодская область | Волог | `NorthWest` |

| 12 | `KaliningradskayaOblast` | Калининградская область | Клнград | `NorthWest` |

| 13 | `MurmanskayaOblast` | Мурманская область | Мурм | `NorthWest` |

| 14 | `PskovskayaOblast` | Псковская область | Псков | `NorthWest` |

| 15 | `NenetskiyAvtonomnyyOkrug` | Ненецкий автономный округ | Ненец | `NorthWest` |

| 17 | `Tatarstan` | Татарстан | Казань | `Volga` |

| 18 | `TomskayaOblast` | Томская область | Томск | `Siberia` |

| 19 | `Bashkortostan` | Башкортостан | Бшк | `Ural` |

| 20 | `NizhegorodskayaOblast` | Нижегородская область | Нижн | `Volga` |

| 21 | `SamarskayaOblast` | Самарская область | Смр | `Volga` |

| 22 | `TulskayaOblast` | Тульская область | Тула | `MoscowAndCenter` |

| 23 | `RyazanskayaOblast` | Рязанская область | Ряз | `MoscowAndCenter` |

| 24 | `SverdlovskayaOblast` | Свердловская область | Сврд | `Ural` |

| 25 | `ChelyabinskayaOblast` | Челябинская область | Члб | `Ural` |

| 26 | `OrlovskayaOblast` | Орловская область | Орёл | `MoscowAndCenter` |

| 27 | `KaluzhskayaOblast` | Калужская область | Калуга | `MoscowAndCenter` |

| 28 | `PermskiyKray` | Пермский край | Пермь | `Ural` |

| 29 | `OrenburgskayaOblast` | Оренбургская область | Орен | `Volga` |

| 30 | `TyumenskayaOblast` | Тюменская область | Тюм | `Ural` |

| 31 | `KurganskayaOblast` | Курганская область | Кург | `Ural` |

| 32 | `KurskayaOblast` | Курская область | Курск | `MoscowAndCenter` |

| 33 | `BelgorodskayaOblast` | Белгородская область | Белг | `MoscowAndCenter` |

| 34 | `VoronezhskayaOblast` | Воронежская область | Врнж | `MoscowAndCenter` |

| 35 | `KhantyMansiyskiyAvtonomnyyOkrug` | Ханты-Мансийский автономный округ | ХМАО | `Ural` |

| 36 | `YamaloNenetskiyAvtonomnyyOkrug` | Ямало-Ненецкий автономный округ | ЯНАО | `Ural` |

| 37 | `YaroslavskayaOblast` | Ярославская область | Ярслвль | `MoscowAndCenter` |

| 38 | `KrasnoyarskiyKray` | Красноярский край | Крс | `Siberia` |

| 39 | `NovosibirskayaOblast` | Новосибирская область | Нвсиб | `Siberia` |

| 40 | `Adygeya` | Адыгея | Адг | `South` |

| 41 | `AstrakhanskayaOblast` | Астраханская область | Астрх | `South` |

| 42 | `VolgogradskayaOblast` | Волгоградская область | Влгр | `South` |

| 43 | `Kalmykiya` | Калмыкия | Клмк | `South` |

| 44 | `RostovskayaOblast` | Ростовская область | Рств | `South` |

| 45 | `KrasnodarskiyKray` | Краснодарский край | Крсдар | `South` |

| 46 | `StavropolskiyKray` | Ставропольский Край | Стврп | `South` |

| 47 | `TambovskayaOblast` | Тамбовская область | Тмбв | `MoscowAndCenter` |

| 48 | `Buryatiya` | Бурятия | Бурят | `Siberia` |

| 49 | `AltayRespublika` | Алтай (Республика) | Алт | `Siberia` |

| 50 | `ZabaykalskiyKray` | Забайкальский край | Збкл | `Siberia` |

| 51 | `Khakasiya` | Хакасия | Хкс | `Siberia` |

| 52 | `Tyva` | Тыва | Тыв | `Siberia` |

| 53 | `AltayskiyKray` | Алтайский край | Алткр | `Siberia` |

| 55 | `IrkutskayaOblast` | Иркутская область | Иркт | `Siberia` |

| 56 | `KemerovskayaOblast` | Кемеровская область | Кмрв | `Siberia` |

| 57 | `OmskayaOblast` | Омская область | Омск | `Siberia` |

| 59 | `SakhaYakutiya` | Саха (Якутия) | Якт | `FarEast` |

| 60 | `KhabarovskiyKray` | Хабаровский край | Хабр | `FarEast` |

| 61 | `AmurskayaOblast` | Амурская область | Амур | `FarEast` |

| 62 | `KamchatskiyKray` | Камчатский край | Камч | `FarEast` |

| 63 | `MagadanskayaOblast` | Магаданская область | Магад | `FarEast` |

| 64 | `SakhalinskayaOblast` | Сахалинская область | Схлн | `FarEast` |

| 65 | `EvreyskayaAvtonomnayaOblast` | Еврейская автономная область | ЕАО | `FarEast` |

| 66 | `ChukotskiyAvtonomnyyOkrug` | Чукотский автономный округ | ЧАО | `FarEast` |

| 67 | `PrimorskiyKray` | Приморский край | Приморье | `FarEast` |

| 68 | `KostromskayaOblast` | Костромская область | Костром | `MoscowAndCenter` |

| 69 | `IzhevskayaOblast` | Ижевская область | Ижв | `Volga` |

| 70 | `KirovskayaOblast` | Кировская область | Крв | `Volga` |

| 71 | `RespublikaMariyEl` | Республика Марий Эл | МрЭл | `Volga` |

| 72 | `Mordoviya` | Мордовия | Мрдв | `Volga` |

| 73 | `PenzenskayaOblast` | Пензенская область | Пенза | `Volga` |

| 74 | `SaratovskayaOblast` | Саратовская область | Сртв | `Volga` |

| 75 | `UdmurtskayaRespublika` | Удмуртская республика | Удмрт | `Volga` |

| 76 | `UlyanovskayaOblast` | Ульяновская область | Улвн | `Volga` |

| 77 | `ChuvashskayaOblast` | Чувашская область | Чвш | `Volga` |

| 78 | `RespublikaKrym` | Республика Крым | Крым | `South` |

| 79 | `VinnitskayaOblast` | Винницкая область | Винница | `Ukraine` |

| 80 | `VolynskayaOblast` | Волынская область | Волынь | `Ukraine` |

| 81 | `DnepropetrovskayaOblast` | Днепропетровская область | Днепр | `Ukraine` |

| 82 | `DonetskayaNarodnayaRespublika` | Донецкая народная республика | ДНР | `South` |

| 83 | `ZhitomirskayaOblast` | Житомирская область | Житомир | `Ukraine` |

| 84 | `ZakarpatskayaOblast` | Закарпатская область | Закарпаты | `Ukraine` |

| 85 | `ZaporozhskayaOblast` | Запорожская область | Запорожье | `South` |

| 86 | `IvanoFrankovskayaOblast` | Ивано-Франковская область | И-Франк | `Ukraine` |

| 87 | `KievskayaOblast` | Киевская область | Киев | `Ukraine` |

| 88 | `KirovogradskayaOblast` | Кировоградская область | Кирвгрд | `Ukraine` |

| 89 | `LuganskayaNarodnayaRespublika` | Луганская народная республика | ЛНР | `South` |

| 90 | `LvovskayaOblast` | Львовская область | Львов | `Ukraine` |

| 91 | `NikolaevskayaOblast` | Николаевская область | Николаевск | `Ukraine` |

| 92 | `OdesskayaOblast` | Одесская область | Одесса | `Ukraine` |

| 93 | `PoltavskayaOblast` | Полтавская область | Полтава | `Ukraine` |

| 94 | `RovnenskayaOblast` | Ровненская область | Ровно | `Ukraine` |

| 95 | `SumskayaOblast` | Сумская область | Сумы | `Ukraine` |

| 96 | `TernopolskayaOblast` | Тернопольская область | Тернополь | `Ukraine` |

| 97 | `KharkovskayaOblast` | Харьковская область | Харьков | `Ukraine` |

| 98 | `KhersonskayaOblast` | Херсонская область | Херсон | `South` |

| 99 | `KhmelnitskayaOblast` | Хмельницкая область | Хмель | `Ukraine` |

| 100 | `CherkasskayaOblast` | Черкасская область | Черкас | `Ukraine` |

| 101 | `ChernigovskayaOblast` | Черниговская область | Чернигов | `Ukraine` |

| 102 | `ChernovitskayaOblast` | Черновицкая область | Черновицк | `Ukraine` |

| 103 | `Sevastopol` | Севастополь | Севпль | `South` |

| 104 | `SmolenskayaOblast` | Смоленская область | Смоленск | `MoscowAndCenter` |

| 106 | `BrestskayaOblast` | Брестская область | Брест | `Belarus` |

| 107 | `VitebskayaOblast` | Витебская область | Витебск | `Belarus` |

| 108 | `GomelskayaOblast` | Гомельская область | Гомель | `Belarus` |

| 109 | `GrodnenskayaOblast` | Гродненская область | Гродно | `Belarus` |

| 110 | `MinskayaOblast` | Минская область | Минск | `Belarus` |

| 111 | `MogilyovskayaOblast` | Могилёвская область | Могилёв | `Belarus` |

| 112 | `IvanovskayaOblast` | Ивановская область | Иванов | `MoscowAndCenter` |

| 113 | `LipetskayaOblast` | Липецкая область | Лпцк | `MoscowAndCenter` |

| 114 | `Astana` | Астана | Астана | `Kazakhstan` |

| 115 | `Almaty` | Алматы | Алматы | `Kazakhstan` |

| 116 | `Baykonur` | Байконур | Байконур | `Kazakhstan` |

| 117 | `AkmolinskayaOblast` | Акмолинская область | Акмол | `Kazakhstan` |

| 118 | `AktyubinskayaOblast` | Актюбинская область | Актюбы | `Kazakhstan` |

| 119 | `AlmatinskayaOblast` | Алматинская область | Алмат. обл | `Kazakhstan` |

| 120 | `AtyrauskayaOblast` | Атырауская область | Атырау | `Kazakhstan` |

| 121 | `VostochnoKazakhstanskayaOblast` | Восточно-Казахстанская область | Восток Кзхстн | `Kazakhstan` |

| 122 | `ZhambylskayaOblast` | Жамбылская область | Жамбыл | `Kazakhstan` |

| 123 | `ZapadnoKazakhstanskayaOblast` | Западно-Казахстанская область | Запад Кзхстн | `Kazakhstan` |

| 124 | `KaragandinskayaOblast` | Карагандинская область | Караганда | `Kazakhstan` |

| 125 | `KostanayskayaOblast` | Костанайская область | Костанай | `Kazakhstan` |

| 126 | `KyzylordinskayaOblast` | Кызылординская область | Қызылорда | `Kazakhstan` |

| 127 | `MangistauskayaOblast` | Мангистауская область | Мангист | `Kazakhstan` |

| 128 | `PavlodarskayaOblast` | Павлодарская область | Павлодар | `Kazakhstan` |

| 129 | `SeveroKazakhstanskayaOblast` | Северо-Казахстанская область | Север Кзхстн | `Kazakhstan` |

| 130 | `YuzhnoKazakhstanskayaOblast` | Южно-Казахстанская область | Юг Кзхстн | `Kazakhstan` |

| 131 | `Armeniya` | Армения | Армения | `Foreign` |

| 132 | `Kazakhstan` | Казахстан | Казахстан | `Foreign` |

| 133 | `Kirgiziya` | Киргизия | Киргизия | `Foreign` |

| 134 | `Moldaviya` | Молдавия | Молдавия | `Foreign` |

| 135 | `Tadzhikistan` | Таджикистан | Таджикистан | `Foreign` |

| 136 | `Uzbekistan` | Узбекистан | Узбекистан | `Foreign` |

| 137 | `Velikobritaniya` | Великобритания | UK | `Foreign` |

| 138 | `Daniya` | Дания | Дания | `Foreign` |

| 139 | `Irlandiya` | Ирландия | Ирландия | `Foreign` |

| 140 | `Islandiya` | Исландия | Исландия | `Foreign` |

| 141 | `Latviya` | Латвия | Латвия | `Foreign` |

| 142 | `Litva` | Литва | Литва | `Foreign` |

| 143 | `Norvegiya` | Норвегия | Норвегия | `Foreign` |

| 144 | `Finlyandiya` | Финляндия | Финляндия | `Foreign` |

| 145 | `Shvetsiya` | Швеция | Швеция | `Foreign` |

| 146 | `Estoniya` | Эстония | Эстония | `Foreign` |

| 147 | `Vengriya` | Венгрия | Венгрия | `Foreign` |

| 148 | `Gruziya` | Грузия | Грузия | `Foreign` |

| 149 | `Polsha` | Польша | Польша | `Foreign` |

| 150 | `Slovakiya` | Словакия | Словакия | `Foreign` |

| 151 | `Chekhiya` | Чехия | Чехия | `Foreign` |

| 152 | `Avstriya` | Австрия | Австрия | `Foreign` |

| 153 | `Belgiya` | Бельгия | Бельгия | `Foreign` |

| 154 | `Germaniya` | Германия | ФРГ | `Foreign` |

| 155 | `Niderlandy` | Нидерланды | Нидерланды | `Foreign` |

| 156 | `Frantsiya` | Франция | Франция | `Foreign` |

| 157 | `Shveytsariya` | Швейцария | Швейцария | `Foreign` |

| 158 | `Albaniya` | Албания | Албания | `Foreign` |

| 159 | `Bolgariya` | Болгария | Болгария | `Foreign` |

| 160 | `Gretsiya` | Греция | Греция | `Foreign` |

| 161 | `Rumyniya` | Румыния | Румыния | `Foreign` |

| 162 | `SevernayaMakedoniya` | Северная Македония | Северная Македония | `Foreign` |

| 163 | `Serbiya` | Сербия | Сербия | `Foreign` |

| 164 | `Sloveniya` | Словения | Словения | `Foreign` |

| 165 | `Turtsiya` | Турция | Турция | `Foreign` |

| 166 | `Khorvatiya` | Хорватия | Хорватия | `Foreign` |

| 167 | `Chernogoriya` | Черногория | Черногория | `Foreign` |

| 168 | `Ispaniya` | Испания | Испания | `Foreign` |

| 169 | `Italiya` | Италия | Италия | `Foreign` |

| 170 | `RespublikaKipr` | Республика Кипр | Республика Кипр | `Foreign` |

| 171 | `Portugaliya` | Португалия | Португалия | `Foreign` |

| 172 | `FrantsiyaObsolete` `[Obsolete]` | Франция | Франция | `Foreign` |

| 173 | `Izrail` | Израиль | Израиль | `Foreign` |

| 174 | `BryanskayaOblast` | Брянская область | Брянск | `MoscowAndCenter` |
