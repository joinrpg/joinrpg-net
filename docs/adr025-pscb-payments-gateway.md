# ADR025: Админский шлюз к API ПСКБ — список платежей и параметры платежа

Проблема:
==

Поддержке невидно, что про платёж знает банк. В JoinRpg есть `FinanceOperation` со статусом,
который мы сами когда-то записали, но первоисточника нет: нельзя посмотреть, какие платежи
прошли через магазин за период, в каком они состоянии на стороне ПСКБ, какие по ним чеки,
возвраты, данные карты и реестры. Сейчас это делается руками в кабинете мерчанта.

HTTP-клиент к банку в решении уже есть — `src/PscbApi/BankApi.cs`: он считает подпись, ходит на
`merchantApi` и разбирает ответ. Из двух нужных методов реализован один:

- **«Запрос параметров платежа»** (`merchantApi/checkPayment`) — реализован как
  `BankApi.GetPaymentInfoAsync` (`BankApi.cs:189`). Занят только в боевом пути обновления статуса
  (`PaymentsService.cs:136`, `:657`), наружу не отдан.
- **«Запрос списка платежей»** (`merchantApi/getPayments`) — не реализован вовсе: ни метода, ни
  моделей.

Отдельно конфигурация клиента разошлась с документацией банка: в
`src/JoinRpg.Portal/appsettings.json` прописаны `https://oos.pscb.ru` и `https://oosdemo.pscb.ru`,
а документация (<https://docs.pscb.ru/oos/api.html>) для всех методов — и для платёжной формы
`/pay`, и для всего `merchantApi/*` — называет `https://paygates.ru` и
`https://demo.paygates.ru`. Хоста `oos.pscb.ru` в `api.md` нет ни разу.

Решение:
==

Два read-only эндпоинта-шлюза и админская страница `/admin/pscb-payments` с двумя формами. Шлюз
отдаёт то, что сказал банк, своими типами и **ничего не примешивает** из данных JoinRpg.

### Контракт банка

Оба метода — HTTP POST с JSON-телом; подпись `SHA-256(тело‖secretKey)` в шестнадцатеричном виде
в заголовке `Signature`. Ровно это уже делает `BankApi.ApiRequestAsync`, менять его не нужно.

`merchantApi/getPayments`:

| Параметр | Тип | Обязательный |
|---|---|---|
| `marketPlace` | String | да |
| `dateFrom` | DateTime | да, нижняя граница включительно |
| `dateTo` | DateTime | нет, верхняя граница **исключительно** |
| `selectMode` | String | нет: `paid` (по умолчанию) — только завершённые, `created` — все |
| `paymentType` | String | нет: `payment` — без счетов, `invoice` — только счета, пусто — всё |
| `requestFiscalData` | Boolean | нет |
| `requestClearingData` | Boolean | нет |

Ответ — `{ "payments": [ … ] }`. **Максимальная продолжительность периода — 1 месяц**; если период
больше, банк молча возвращает платежи только за последний месяц запрошенного периода.

`merchantApi/checkPayment`: `orderId`\*, `marketPlace`\*, `requestCardData`, `requestFiscalData`,
`requestClearingData`. Ответ — конверт `status` (`STATUS_SUCCESS`/`STATUS_FAILURE`), `requestId`,
`payment`, `description`, `errorCode`, `errorDescription`.

Документация **не объявляет** состав элемента `payments[]` — в примере стоит многоточие. Единственное
описание ключей платежа находится в разделе «Схема взаимодействия → Ответ», и оно же используется для
`payment` в `checkPayment`.

### Две поверхности, одна реализация

`[XAdminAuthorize]` жёстко задаёт `AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme`
(`Infrastructure/XApi/XAdminAuthorizeAttribute.cs:8`), поэтому остров Blazor, который ходит
`fetch`'ем с кукой, авторизоваться на `x-api` не может — это же зафиксировано комментарием в
`Infrastructure/Authentication/AuthenticationConfigurator.cs:64`. Значит, поверхностей две:

- `GET /x-api/pscb/payments` и `GET /x-api/pscb/payments/{orderId}` — `[XAdminAuthorize]`, JWT,
  попадают в Swagger (`Infrastructure/XApi/Swagger.cs` оставляет только `IsExternalApiPath`).
  Прецедент админского `x-api` уже есть: `XUserInfoController` — `[Route("x-api/users/"), XAdminAuthorize()]`.
- `GET /webapi/PscbPayments/GetPayments` и `GetPaymentInfo` — `[AdminAuthorize]`, кука, для острова.

Оба контроллера тонкие и вызывают один сервис. Авторизация — только админ сайта; доступ к данным
проекта не проверяется, потому что шлюз отдаёт платежи магазина целиком, а не по проектам.

Все четыре маршрута — GET, поэтому глобальный `AutoValidateAntiforgeryTokenAttribute` их не
касается и `CsrfTokenProvider` острову не нужен.

### Модель ответа — наш контракт, а не банковский JSON

Эндпоинты отдают C#-модели. Отдавать `PscbApi.Models` из контроллера напрямую нельзя: в решении
**нет** `AddNewtonsoftJson()`, контроллеры сериализуют через System.Text.Json с веб-настройками по
умолчанию (camelCase), и атрибуты `Newtonsoft.Json.JsonProperty`, которыми размечены модели банка,
при этом игнорируются. Имена разъехались бы:

| Модель | `[JsonProperty]` | System.Text.Json отдаст |
|---|---|---|
| `PaymentData.Id` | `paymentId` | `id` |
| `PaymentData.OrderIdDisplayValue` | `showOrderId` | `orderIdDisplayValue` |
| `PaymentData.CurrentStatusDate` | `stateDate` | `currentStatusDate` |
| `PaymentData.Card` | `paymentParams` | `card` |
| `BankCardData.MaskedName` | `card` | `maskedName` |

Поэтому на границе API — отдельные DTO в `JoinRpg.XGameApi.Contract`: это проект для DTO-контрактов
API, у него нет зависимостей, и на него уже ссылаются `JoinRpg.WebPortal.Managers` и
`JoinRpg.Portal`. Стиль — как у существующих контрактов (`CheckInStats`): обычные свойства без
JSON-атрибутов, на проводе camelCase по умолчанию, XML-документация. Банковские имена полей
оставляются внутри `PscbApi` и наружу не протекают.

Набор DTO **один на обе поверхности**: `JoinRpg.Web.AdminTools` получает ссылку на
`JoinRpg.XGameApi.Contract`, и остров показывает те же типы, которые отдаёт `x-api`. Проект
контрактов пуст по зависимостям, а `JoinRpg.Web.AdminTools` объявлен как
`<SupportedPlatform Include="browser" />`, поэтому для WASM это безопасно.

Преобразование `PscbApi.Models.PaymentData` → DTO — отдельный статический Builder в
`JoinRpg.WebPortal.Managers/AdminTools/`, по паттерну из [ui-pages-common.md](ui-pages-common.md):
его можно тестировать без банка и без репозиториев.

Следствие, которое надо держать в голове: **поля, которых нет в нашей модели, до админа не доедут.**
Поэтому:

- `PaymentData` дополняется недостающими по документации ключами — `transactionId`, `fee`, `type`,
  `marketPlace`, `merchantData`, `registryId`, `registryDate`; в `PaymentInfoQueryParams`
  добавляется `requestClearingData`. Правки только добавочные: Newtonsoft игнорирует отсутствующие
  в JSON ключи, поэтому боевой путь не меняется;
- модель элемента списка строится по той же таблице ключей платежа — единственному доступному
  описанию. Если банк вернёт что-то сверх неё, это лечится добавлением поля в модель, а не поиском
  по логам. Фактический состав `payments[]` проверить на demo-окружении до релиза.

Выигрыш от типизации: Swagger описывает ответ типом, а не текстом в `<remarks>`.

### Список платежей — чанки по 20 дней

Банк ограничивает период одним месяцем, а на превышение отвечает не ошибкой, а тихим обрезанием до
последнего месяца. Что именно он считает месяцем — календарный месяц, 30 дней или
`dateFrom.AddMonths(1)`, — из документации не ясно. Поэтому чанк берётся заведомо короче любого из
этих толкований: **20 дней**.

- чанки идут подряд, `dateTo` каждого — это `dateFrom` следующего; так как `dateTo` исключительно,
  ни перекрытий, ни дыр нет;
- 20 дней короче любого календарного месяца, значит обрезание не сработает ни при каком толковании
  ограничения;
- запросы последовательные, не параллельные: банк — внешняя сторона, и всплеск параллельных вызовов
  ради ускорения админской страницы не оправдан;
- цена — год диапазона это 19 вызовов вместо 12. Верхняя граница диапазона остаётся (2 года,
  ~37 вызовов), чтобы запрос не мог превратиться в бесконечную серию обращений к банку;
- после слияния элементы дедуплицируются по `orderId` + `paymentId`. Чанки не пересекаются, но
  статус платежа между запросами может измениться, и защита стоит дешевле, чем расхождение в сверке.

Если хоть один чанк вернулся с `STATUS_FAILURE`, весь запрос падает с ошибкой банка. Частичный
список в инструменте для сверки хуже ошибки: его примут за полный.

### CSV

`format=json` (по умолчанию) или `format=csv` — параметр того же эндпоинта списка.

Колонки фиксированные, по полям DTO: модель объявлена нами, поэтому разбирать пришедший JSON в
наборе ключей не нужно. Генерация идёт через существующий `JoinRpg.Services.Export` —
`IExportDataService.GetGenerator(ExportType.Csv, …)`, `CsvBackend`, отдача через
`GeneratorResultHelper.Result(...)`; фронтенд колонок — по образцу `CustomExporter<TRow>`
(`JoinRpg.WebPortal.Models/Exporters`).

Вложенные коллекции (`refunds`, `receipts`) попадают в ячейку количеством и списком id через
разделитель — другого места в строке CSV у них нет. Документация не обещает их в ответе
`getPayments`, но `requestFiscalData` может вернуть чеки, поэтому колонки обязаны терпеть и пустоту,
и массив.

Две мелочи, которые надо сделать явно:

- строки передавать в генератор **материализованным списком**: `TableGenerator.Generate()` сначала
  вызывает `ParseColumns()`, а потом перечисляет `Data`, поэтому ленивая последовательность будет
  перечислена дважды;
- `CsvBackend` пишет UTF-8 **без BOM** (`Encoding.UTF8.GetBytes` не выдаёт преамбулу). Банк
  возвращает русский текст (`state_desc`, `details`, `comment`), и Excel на русской локали прочитает
  такой файл как CP1251. Преамбулу добавляем в `CsvBackend` — это чинит кириллицу во всех
  CSV-экспортах сразу, включая существующий `FinancesController.SummaryByMaster`, поэтому правка
  идёт отдельным коммитом.

### Страница `/admin/pscb-payments`

По образцу ADR024 (`/admin/user-merge`):

- `src/JoinRpg.Portal/Pages/Admin/PscbPayments.cshtml(.cs)` — `@page "/admin/pscb-payments"`,
  `[AdminAuthorize]`, рендерит остров;
- остров и контракт `IPscbPaymentsClient` — в `src/JoinRpg.Web.AdminTools/PscbPayments/`;
- серверная реализация — `src/JoinRpg.WebPortal.Managers/AdminTools/`, регистрация в `Registration.cs`;
- WASM-реализация — `src/JoinRpg.Blazor.Client/ApiClients/` + `HttpClientRegistration.cs`;
- ссылка добавляется в `src/JoinRpg.Portal/Pages/Admin/Index.cshtml` (список там ведётся вручную).

Две формы — по одной на эндпоинт. Форма списка: `dateFrom`, `dateTo`, `selectMode`, `paymentType`,
`requestFiscalData`; `marketPlace` подставляется из конфигурации и в форме не показывается. Форма
деталей: `orderId`, `requestCardData`, `requestFiscalData`, `requestClearingData`. Все подписи — на
русском.

Параметр `merchant` (запрос по всем магазинам мерчанта) наружу не выводится: у нас один магазин, и
выбирать его некому.

Результат списка — таблица (`JoinTableSimple`/`JoinBorderedTable`) поверх списка DTO: колонки
известны на этапе компиляции, динамический разбор не нужен. Результат деталей — панель с полями
того же DTO. Ссылка «Скачать CSV» строится в острове из адреса того же вызова заменой
`format=csv`; `*UriLocator` не добавляем — он нужен, когда один адрес используют и серверная
разметка, и WASM (рассинхрон ловит `UriLocatorConsistencyTests`), а здесь адрес нужен только
клиенту.

### Ошибки банка

`ApiRequestAsync` проверяет только HTTP-код: `STATUS_FAILURE` приходит как HTTP 200 и молча
десериализуется. Трогать его нельзя — через него идёт боевой путь платежей. Поэтому `status`
проверяет новый код шлюза: при `STATUS_FAILURE` он отдаёт админу `errorCode` и `errorDescription`
банка. Возможные коды — `NOT_AUTHORIZED`, `ILLEGAL_REQUEST`, `ILLEGAL_ARGUMENTS`,
`UNKNOWN_PAYMENT`, `ILLEGAL_ACTION`, `ILLEGAL_PAYMENT_STATE`, `REPEAT_REQUEST`, `PROCESSING`,
`SERVER_ERROR`; все они уже есть в `PscbApi.Models.ApiErrorCode`.

### Конфигурация

- `PaymentProviders:Pscb:BankApiEndpoint` → `https://paygates.ru`,
  `BankApiDebugEndpoint` → `https://demo.paygates.ru`. Хост один и для `/pay`, и для
  `merchantApi/*`, поэтому достаточно двух значений: `BankApi` строит из `ActualApiEndpoint` и
  платёжную форму, и все запросы к API.
- **Правка затрагивает боевой путь.** `BankApi.BuildPaymentRequestAsync` собирает из
  `ActualApiEndpoint` адрес платёжной формы `{endpoint}/pay`, на которую редиректится плательщик.
  Документация для рабочего окружения называет именно `https://paygates.ru/pay`, так что замена
  корректна, но проверять её нужно на demo-ключе и катить отдельным коммитом, не вместе с фичей.
- `BankMerchantIdFastPayments` удаляется — ключ устарел, `ConfigurationAdapter` его не читает.
- **Виджет банка на странице заявки удалён.** `src/JoinRpg.Portal/Views/Claim/Edit.cshtml` грузил
  `https://oos.pscb.ru/s/widget/pscb_oos_pay.min.js` — единственную оставшуюся в коде ссылку на
  `oos.pscb.ru`, зашитую жёстко. Точка входа виджета — глобальная функция `doPscbPay(...)`, сам
  скрипт ничего не инициализирует. Вызовов в репозитории нет, и `git log --all -S "doPscbPay"` не
  находит ни одного коммита — то есть вызова не было никогда. Скрипт добавлен в 2019 году
  (`0e448b364`), платёж с тех пор уходит полной навигацией через `_RedirectFormPartial` →
  `_RedirectContentPartial`, а `<iframe>` в представлениях Portal нет вовсе.
  Выводить адрес из конфигурации смысла нет: настраивать нечего, а на demo-хосте файла и вовсе нет —
  `demo.paygates.ru/s/widget/pscb_oos_pay.min.js` и `oosdemo.pscb.ru/…` отдают HTTP 404, тогда как
  `paygates.ru/…` и `oos.pscb.ru/…` — HTTP 200. Адрес из конфигурации означал бы битый тег
  `<script>` в каждом окружении с `Debug = true`, поэтому тег убран целиком.
- **Локальная разработка.** `src/JoinRpg.Portal/appsettings.Development.json` (файл отслеживается
  git'ом, там лежат дефолты docker-compose) получает `"Debug": true`, чтобы локальные запуски ходили
  на `demo.paygates.ru`; адреса там не дублируются — они приходят из `appsettings.json`. Секреты
  (`BankMerchantId`, `BankApiDebugKey`) туда не кладутся — файл в репозитории; разработчик задаёт их
  сам переменной окружения или локальным незакоммиченным оверрайдом. `AddUserSecrets` у Portal не
  подключён — он есть только в `JoinRpg.Tools.Drp.Common`.
- **Продакшен.** `manifests/base/deployment.yaml` подключает настройки через
  `envFrom: secretRef: joinrpg-main`, а `manifests/base/joinrpg-main.secret.env` в репозитории пуст.
  Если в секрете заданы `PaymentProviders__Pscb__BankApiEndpoint` / `BankApiDebugEndpoint`, правка
  `appsettings.json` там не подействует. Значения секрета не читаем — при выкатке достаточно
  убедиться, что адрес в самом деле сменился.

### Тесты

Новый тестовый проект `PscbApi.Test` (в `Joinrpg.slnx` добавляется вручную; `InternalsVisibleTo`
для `$(AssemblyName).Test` уже даёт `Directory.Build.props`). **Тесты не ходят на сервер банка — ни
на боевой, ни на тестовый.** Подпись, URL и разбор ответа проверяются на самодельных
`HttpMessageHandler` + `IHttpClientFactory` — этот приём уже используется в
`src/JoinRpg.BlobStorage.Test/S3AvatarStorageServiceTests.cs:67`. Moq/NSubstitute в решении нет.

Без HTTP проверяются: нарезка периода на чанки (чистая работа с датами), Builder
`PaymentData` → DTO и набор колонок CSV.

Новые GET-маршруты обязательно вносятся в
`src/JoinRpg.IntegrationTest/TestInfrastructure/SmokeExpectations.cs` — иначе смоук-тест падает с
именем маршрута. Снапшот ленивых загрузок (`docs/lazy-loads-baseline.md`) пересчитывается в CI.

Что не делаем:
==

- **Обогащения данными JoinRpg.** `orderId` банка — это наш `CommentId`, дополненный нулями до 10
  знаков (`PaymentsService.cs:45`), поэтому связать платёж с заявкой, проектом и игроком дёшево.
  Сейчас не делаем: шлюз должен показывать первоисточник без примесей. Вернёмся, если понадобится
  сверка.
- **Изменений `ApiRequestAsync`.** Проверка `status` на уровне клиента была бы правильнее, но через
  него идёт боевой путь платежей — трогать его в этой задаче нельзя.
- **Банковских имён полей в нашем контракте.** DTO называются по-нашему, `JsonProperty`-имена
  банка остаются внутри `PscbApi`.
- **Прозрачной передачи неизвестных полей.** Чего нет в модели — того нет в ответе; это осознанная
  плата за типизированный контракт.
- **Кэширования, повторов и `REPEAT_REQUEST`-логики.** Инструмент диагностический, вызовов мало;
  `REPEAT_REQUEST` и `PROCESSING` отдаются админу как есть.
- **xlsx.** Только JSON и CSV.
- **Параллельных запросов чанков к банку.**
- **Обращений к банку из тестов** — даже к demo-окружению.
