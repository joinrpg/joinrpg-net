# ADR011. Размер payload сетки ролей

Каждое решение из этого ADR выполняется отдельным PR.

## Проблема

Ручка `webapi/project-role-grid/getclassic` отдаёт слишком много данных. Замеры (август 2026):

| | размер | строк |
|---|---|---|
| Аноним, прод, `projectId=1272&characterGroupId=30323` | 462 КБ | 542 |
| Мастер, проект 1329 | **18,13 МБ** (15 703 235 симв.) | 19 609 |

Дополнительно: ответ отдаётся **полностью без сжатия** — прод не ставит `Content-Encoding`
даже на запрос с `Accept-Encoding: gzip, deflate, br`. TTFB анонимного запроса — 14,5 с.

### Откуда 18 МБ

Полезной информации в документе около **100 КБ**, остальное — структурный оверхед:

| Драйвер | символов | доля |
|---|---|---|
| Объекты типизированных Id (74 158 шт.) | 5 196 184 | **33,1 %** |
| `fieldValues` на строках `firstCopy:false` (клиент их не читает) | 2 453 840 | **15,6 %** |
| Свойства со значением по умолчанию (`viewMode:0`, `isHot:false`, `isSlot:false`, `canEdit`, `isActive`, `firstCopy`, `activeClaimsCount:0`) | ~2 170 000 | ~13,8 % |
| Свойства со значением `null` (`contacts`, `groups`, `slotCount`, `approvedClaimId`, `link`, `description*`) | ~940 000 | ~6 % |
| Реальный контент (уникальные описания персонажей + HTML групп) | ~64 000 | **0,4 %** |

Корневых причин две.

**1. Каждый Id — вложенный объект с дублирующимся полем.**

```json
{"projectId":{"value":1329,"id":1329},"characterId":111260,"id":111260}
```

68 символов там, где хватило бы 22. Поле `id` — сгенерированное вычисляемое свойство
`public int Id => <последний int-параметр>;` (`TypedEntityIdGenerator.cs:416`), которое
System.Text.Json тоже сериализует. Плюс `player.applyStatus.characterId` дублирует
`character.character.characterId` той же строки — 18 985 лишних Id, 10,2 % файла.

**2. 19 609 строк, но всего 401 уникальный персонаж.**

18 584 из 18 985 строк персонажей (97,9 %) имеют `firstCopy:false` — клиент рисует для них
«(см. выше)» и **не читает `fieldValues`** (`ProjectRoleGrid.razor:106-112`), но сервер шлёт
полное описание персонажа в каждой. `BuildTreeRows` вычисляет `firstCopy`
(`ProjectRoleGridViewModelBuilder.cs:146`), а `BuildCharacterRow` его игнорирует. Для групп
такая экономия уже сделана (`ProjectRoleGridViewModelBuilder.cs:128`:
`firstCopy ? ... : null`), для персонажей — нет.

---

## Решение 1 (PR 1). Включить сжатие ответов — в приложении, не в nginx

Замер: 18,13 МБ → **2,11 МБ** при `gzip -6`; brotli даст ~1,3 МБ.

### Почему в приложении

Поверх приложения стоит ingress-nginx (`manifests/base/ingress.yaml`, TLS-терминация,
service 80 → pod 8080), поэтому вариант «пусть жмёт nginx» рассмотрен отдельно:

* ingress-nginx **по умолчанию не жмёт** (`use-gzip` = `false`) — это и объясняет замер;
* gzip настраивается **только в глобальном ConfigMap контроллера** (`use-gzip`, `gzip-types`,
  `gzip-level`, `gzip-min-length`), per-Ingress аннотации для этого нет. Этого ConfigMap в
  репозитории нет — в `manifests/` только Ingress, Service, Deployment, PDB и Job. Правка была
  бы вне репозитория и с blast radius на все приложения за контроллером;
* `nginx.ingress.kubernetes.io/configuration-snippet` как обходной путь в свежих версиях
  ingress-nginx отключён по умолчанию (`allow-snippet-annotations: false`);
* brotli-модуль из ingress-nginx выпилен — со стороны nginx доступен только gzip;
* локальная разработка и `JoinRpg.IntegrationTests` поднимают приложение без nginx: сжатие,
  настроенное в кластере, там не воспроизводится и не тестируется.

Против приложения играет только CPU: лимит пода 500m (`manifests/base/deployment.yaml:62-68`),
2 реплики. Но middleware в ASP.NET по умолчанию использует `CompressionLevel.Fastest`, а после
решений 2–4 тело ужмётся до ~1 МБ — единицы миллисекунд. Не аргумент.

Конфликта с nginx не будет: nginx не пережимает ответ, у которого уже стоит `Content-Encoding`,
и по умолчанию не вырезает `Accept-Encoding` из проксируемого запроса.

### Реализация

`src/JoinRpg.Portal/Startup.cs`, `ConfigureServices` (рядом с `AddMvc`, строки 62–82):

```csharp
services.AddResponseCompression(options =>
{
    // Только JSON. HTML-страницы несут antiforgery-токен в теле формы —
    // сжимать их поверх HTTPS означает открыть BREACH.
    options.MimeTypes = ["application/json"];
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

`Configure` — `app.UseResponseCompression();` сразу после `app.MapStaticAssets().ShortCircuit()`
(`Startup.cs:157`). Статику трогать не надо: `MapStaticAssets` сам отдаёт предсжатые `.br`/`.gz`
ассеты Blazor WASM и ставит `Content-Encoding`; ограничение `MimeTypes` этот путь и так
исключает.

### Что проверить после деплоя

В staging это не воспроизводится: надо убедиться, что `Accept-Encoding` реально доходит до
Kestrel через ingress. Если ответ придёт без `Content-Encoding` — контроллер его срезает, и
тогда всё-таки придётся идти в ConfigMap (`use-gzip: "true"`).

---

## Решение 2 (PR 2, сделано). Не слать `fieldValues` для повторных строк

−15,6 %, diff в три строки, клиент менять не нужно.

`src/JoinRpg.WebPortal.Managers/CharacterGroups/ProjectRoleGridViewModelBuilder.cs`,
`BuildCharacterRow` (строки 189–195): при `firstCopy == false` отдавать пустой список вместо
`fields.Select(...)`.

Тест — в `src/JoinRpg.WebPortal.Managers.Test/CharacterGroups/ProjectRoleGridViewModelBuilderTests.cs`
рядом с `Tree_CharacterInTwoGroups_SecondRowIsNotFirstCopy` (строка 602): у второй строки
`FieldValues` пуст, у первой заполнен.

Тем же PR, бесплатно:

* `ProjectRoleGridGroupHeaderRowViewModel.Description`
  (`src/JoinRpg.Web.CharacterGroups/ProjectRoleGrid/ViewModels.cs:77`) — `MarkupString`,
  вычисляемый из `DescriptionHtml`, из-за чего HTML каждой группы едет по проводу дважды
  (вдобавок `MarkupString` сериализуется как `{"value":"..."}`). Клиент читает только
  `Description` (`ProjectRoleGrid.razor:67`). Сделать вычисляемым свойством с `[JsonIgnore]`
  и проверить тестом, что после десериализации оно корректно считается из `DescriptionHtml`.
* `ProjectRoleGridViewModel.GroupName` (`ViewModels.cs:32`) — клиентом не читается вообще, на
  сервере используется только для заполнения `Name`. Убрать из DTO.

---

## Решение 3 (PR 3 + PR 4). Типизированные Id — одной строкой, через генератор

−24 % чистыми: 74 158 Id × ~70 символов → × ~22 символа, `"Character(1329-111260)"`.

Формат берём из уже генерируемых членов — `ToString()` (`TypedEntityIdGenerator.cs:435`) и
`IParsable<T>.TryParse` через `IdentificationParseHelper` (строки 557–597). Тот же формат уже
персистентен: лежит в БД (`NotificationsRepository.cs:56`, колонка `EntityReference`) и ездит
в `webapi/move` (`MoveClientImpl.cs`). Префикс (`Character(...)`) сохраняем — он совпадает с
персистентным форматом и оставляет провод читаемым.

### Рантайм-конвертер

Новый файл `src/JoinRpg.Common.PrimitiveTypes/Json/TypedEntityIdJsonConverter.cs` — закрытый
generic `JsonConverter<T> where T : class, ISpanParsable<T>`:

* `Write` → `writer.WriteStringValue(value.ToString())`;
* `Read` → `T.TryParse(span, provider: null, out _)`, иначе `JsonException` с внятным текстом;
* обязательно `ReadAsPropertyName`/`WriteAsPropertyName` — без них типизированный Id нельзя
  использовать ключом `Dictionary<,>` в JSON (понадобится в решении 4);
* ограничение `where T : class` даёт `HandleNull == false`, поэтому nullable-поля
  (`ApprovedClaimId`, `RolesListId`) продолжают ездить как `null` мимо конвертера.

**Не `JsonConverterFactory`**: `JoinRpg.Blazor.Client` — BlazorWebAssembly SDK с триммингом на
Release, `MakeGenericType` там статически не укоренён. Закрытый generic внутри `typeof(...)` в
атрибуте — укоренён.

### Правка генератора

В `src/JoinRpg.Common.PrimitiveTypes.SourceGenerator/TypedEntityIdGenerator.cs` перед строкой
412 (`public partial record {info.TypeName}`) дописать атрибут:

```csharp
sb.AppendLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof(global::JoinRpg.Common.PrimitiveTypes.TypedEntityIdJsonConverter<{info.TypeName}>))]");
```

Ручные атрибуты на 19 объявляющих record'ов не пишем — иначе при последующем переносе в
генератор получим дубль (`JsonConverterAttribute` — `AllowMultiple = false`, атрибуты на
partial-объявлениях сливаются, дубль = ошибка компиляции).

**Регистрации в DI не нужно и не надо.** `AddJsonOptions` в проекте нет вообще
(`Startup.cs:63-82`), а атрибут на типе работает во всех точках входа сразу: MVC
output/input formatter, `GetFromJsonAsync` в Blazor WASM, `PersistentComponentState`, параметры
компонентов при prerender. Регистрация через `options.Converters` покрыла бы только сервер и
молча пропустила WASM-клиент.

`[JsonIgnore]` на генерируемое `public int Id =>` не добавляем: при наличии конвертера на типе
STJ вообще не перечисляет свойства.

### Релизный цикл — почему это два PR

Генератор подключён как `PackageReference`, а не `ProjectReference`
(`Directory.Packages.props:90`, версия `2026.3.87`; потребители —
`JoinRpg.Common.PrimitiveTypes.csproj:19` и `JoinRpg.DomainTypes.csproj:12`). Правка
`TypedEntityIdGenerator.cs` ни на что не влияет до публикации пакета.

1. **PR 3** — конвертер в `JoinRpg.Common.PrimitiveTypes` + правка генератора. Формат на
   проводе пока не меняется. Для локальной проверки временно заменить обе `PackageReference`
   на `ProjectReference` (с теми же `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`),
   прогнать тесты, вернуть назад перед коммитом.
2. Вручную запустить `.github/workflows/nuget-publish.yml` (`workflow_dispatch`). Матрица
   публикует `JoinRpg.Common.PrimitiveTypes` и `...SourceGenerator` одним прогоном с одной
   версией — рантайм-библиотека и генератор останутся согласованы.
3. **PR 4** — бампнуть версию в `Directory.Packages.props:90`. С этого момента формат меняется;
   здесь же прогоняются интеграционные тесты.

### Два капкана в парсере

* `IdentificationParseHelper.TryParse1` (строка 42) отбрасывает значения `<= 0`. Одноногие Id
  (`ProjectIdentification`, `UserIdentification`, `AvatarIdentification`, `NotificationId`,
  `KogdaIgraIdentification`) будут сериализоваться, но **не десериализоваться** при значении 0
  или отрицательном. В тестах такие значения уже есть: `ProjectFieldTypeTests.cs:24`
  (`new ProjectIdentification(-1)`), `ProjectRolesListViewModelBuilderTests.cs:160`
  (`new UserIdentification(0)`). Решение: `TryParse1` не трогать — он же управляет model
  binding, и `?projectId=0` начнёт биндиться вместо 400. Поведение зафиксировать тестом.
* `ProjectRolesList.cs:59` создаёт `new ProjectRolesListIdentification(projectId, -1)` как
  sentinel «БД присвоит». `"ProjectRolesListId(1--1)"` round-trip'ится корректно (`SplitAny` с
  двумя диапазонами кладёт остаток в последний), но тестами не покрыто:
  `IdentificationCommonTest.TryEasyConstruct` использует только 1, 2, 3, 4. Добавить явный тест
  на отрицательную последнюю компоненту для 2- и 4-компонентного типа.

### Аудит потребителей — блокеров нет

* Публичный внешний API с `Access-Control-Allow-Origin: *`
  (`GameGroupsController.ReturnJson`, строка 133) типизированных Id **не содержит**:
  `CharacterViewModel.CharacterId/ProjectId`, `UserLinkViewModel.UserId`,
  `CharacterGroupListItemViewModel.CharacterGroupId` — везде обычный `int`. Внешние потребители
  `wwwroot/external/joinrpg-api.js` и `roles-1.js` читают только эти поля. Контракт не ломается.
* В `.js`/`.ts`/`.cshtml` нет кода, читающего типизированные Id из JSON: все потребители
  `/webapi/*` — типизированные C#-клиенты в `src/JoinRpg.Blazor.Client/ApiClients/`,
  десериализующие в те же record'ы (симметрично по построению).
* Swagger не затронут: `JoinRpg.XGameApi.Contract` типизированных Id не использует.
* Поле `id` не читает никто: get-only вычисляемое свойство, STJ его не заполняет,
  `[JsonPropertyName("id")]` нигде нет.

Сеть безопасности уже существует: `IdentificationCommonTest.ShouldRoundTripThroughJson`
(строка 104) — theory по **всем** типизированным Id, и
`src/JoinRpg.IntegrationTests/Scenarios/ProjectRoleGridScenario.cs:128,135` — сквозной
`GetFromJsonAsync<ProjectRoleGridViewResult>` против настоящего сервера.

---

## Решение 4 (PR 5). Повторные строки как ссылки

После решений 1–3 останется ~9,8 МБ (~1,1 МБ по проводу), и доминировать будут те же 18 584
повторные строки: каждая всё ещё везёт имя, Id, `canEdit`, `applyStatus`, `groupId`.

Добавить в `ProjectRoleGridViewModel` словарь
`IReadOnlyDictionary<CharacterIdentification, ProjectRoleGridCharacterRowViewModel>` с 401
уникальным персонажем, а повторные вхождения отдавать третьим `$type` — `ref(CharacterId,
GroupId)`. Клиент (`ProjectRoleGrid.razor`, `BuildRenderBlocks`) резолвит ссылку по словарю.
`WriteAsPropertyName` из решения 3 делает такой словарь сериализуемым.

Ожидаемо: ~1,4 МБ без сжатия, **~120 КБ по проводу** — примерно ×150 от исходных 18 МБ.

Отдельным PR, потому что это единственная часть, меняющая контракт DTO и требующая правок
Blazor-компонента.

---

## За рамками этого ADR

* **TTFB 14,5 с.** `ProjectRepository.GetCharacterByGroups`
  (`src/JoinRpg.Dal.Impl/Repositories/ProjectRepository.cs:160`) прогревает контекст **всем
  проектом** целиком — `LoadProjectFields`, `LoadProjectCharactersAndGroups`, `LoadMasters`,
  `LoadProjectClaims` (два уже помечены `//TODO Remove`), независимо от запрошенной группы.
  Плюс `CharacterGroupRepository.GetCharacterGroupsFullInfo` считает `CharacterCount`
  коррелированным подзапросом с `SqlFunctions.CharIndex` по `ParentGroupsImpl.ListIds` —
  не-sargable строковый матчинг на каждую группу.
* **`TypedStringValueGenerator`** (`Email`, `VkId`, `MarkupString` → `{"value":"..."}`). В этом
  payload — 628 символов, не приоритет. Но генератор устроен так же, и аналогичный
  `TypedStringValueJsonConverter<T>` напрашивается следующим шагом (см.
  [ADR007](adr007-string-value.md)).
* **Пагинация / виртуализация сетки.** Меняет UX, отдельное решение.

---

## Проверка

1. `dotnet build`, `dotnet format --verify-no-changes --severity error`.
2. `dotnet test src/JoinRpg.DomainTypes.Test` — round-trip всех Id + новые тесты на
   отрицательные и нулевые компоненты (решение 3).
3. `dotnet test src/JoinRpg.WebPortal.Managers.Test` — билдер сетки ролей (решение 2).
4. `dotnet test src/JoinRpg.IntegrationTests` — `ProjectRoleGridScenario` как сквозная проверка
   MVC ↔ HttpClient после смены формата Id.
5. Локально: `docker compose up -d`, `dotnet run --project src/JoinRpg.Portal`, открыть
   `/{projectId}/roles/{characterGroupId}` — сетка рисуется как раньше: дерево, «см. выше»,
   описания на первых вхождениях, меню редактирования у мастера.
6. Замер после каждого этапа; **после решения 1 обязательно на проде**:

   ```
   curl -s -o /dev/null -D - -H "Accept-Encoding: gzip, br" \
     -w "\nwire=%{size_download}\n" "<url>" | grep -i "content-encoding\|wire"
   ```

   Ожидаем `content-encoding: br`. Если заголовка нет — ingress срезает `Accept-Encoding`,
   тогда идти в ConfigMap контроллера.

Эталон «до»: 18 130 655 байт, 15 703 235 символов, 19 609 строк, 401 уникальный персонаж.
