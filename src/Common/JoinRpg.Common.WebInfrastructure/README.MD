# JoinRpg.Common.WebInfrastructure

Общая веб-инфраструктура для ASP.NET Core приложений JoinRpg: логирование (Serilog), OpenTelemetry, Data Protection, кеширование, health checks, поддержка PostgreSQL.

## Подключение

### 1. Настройка хоста (Program.cs)

Настройте Serilog как провайдер логирования, передав имя приложения:

```csharp
builder.Host.UseJoinSerilog("MyApp.Name");
```

### 2. Регистрация сервисов

```csharp
services.AddJoinWebPlatform(
    configuration: Configuration,
    environment: Environment,
    appName: "MyApp.Name",
    dataProtectionConnectionStringName: "DataProtection",
    telemetryServiceNames: ["MyApp.ActivitySourceName"]
);
```

Опционально — конфигурация имён хостов JoinRpg (нужна, если приложение использует `JoinRpgHostNamesOptions`):

```csharp
services.Configure<JoinRpgHostNamesOptions>(
    Configuration.GetSection("JoinRpgHostNames")
);
```

### 3. Подключение DbContext (если нужно)

Регистрация DbContext с PostgreSQL и health check:

```csharp
services.AddJoinEfCoreDbContext<MyDbContext>(
    Configuration, Environment,
    connectionStringName: "MyDatabase"
);
```

Для запуска миграций через `Joinrpg.Dal.Migrate`:

```csharp
services.RegisterMigrator<MyDbContext>(
    Configuration, Environment,
    connectionStringName: "MyDatabase"
);
```

### 4. Middleware pipeline

```csharp
app.UseForwardedHeaders();           // Должен быть первым — обрабатывает X-Forwarded-* заголовки
app.UseOpenTelemetryPrometheusScrapingEndpoint();  // Эндпойнт /metrics
app.UseJoinRequestLogging();         // Логирование HTTP-запросов через Serilog

// ... остальной pipeline ...

app.MapJoinHealthChecks();           // Эндпойнты /health, /health/ready, /health/live
```

### 5. MVC/Razor Pages фильтры (опционально)

Для обогащения логов данными из MVC/Razor Pages контекста:

```csharp
options.Filters.Add<SerilogMvcFilter>();
options.Filters.Add<SerilogRazorPagesFilter>();
```

---

## Состав функционала

| Компонент | Класс/Метод | Описание |
|-----------|-------------|----------|
| **Логирование** | `UseJoinSerilog()`, `UseJoinRequestLogging()` | Serilog с обогащением (пользователь, трассировка, YC-уровни), JSON/text форматтеры, фильтрация health check запросов |
| **Data Protection** | `AddJoinDataProtection()` (вызывается из `AddJoinWebPlatform`) | Хранение ключей шифрования в PostgreSQL для работы в нескольких репликах |
| **OpenTelemetry** | `AddJoinOpenTelemetry()` (вызывается из `AddJoinWebPlatform`) | Distributed tracing + метрики, Prometheus endpoint `/metrics` |
| **Кеширование** | `PerRequestCache<K,V>`, `SingletonCache<K,V>` | Per-request scoped кеш и singleton-кеш с фабрикой значений |
| **Health Checks** | `MapJoinHealthChecks()` | Эндпойнты `/health` (полный), `/health/ready` (тег `ready`), `/health/live` (всегда OK) |
| **Forwarded Headers** | `UseForwardedHeaders()` / `ConfigureForwardedHeaders()` | Обработка X-Forwarded-Proto и X-Forwarded-For от reverse proxy |
| **DbContext** | `AddJoinEfCoreDbContext<T>()` | PostgreSQL + EntityFramework.Exceptions + health check для каждого контекста |
| **Миграции** | `RegisterMigrator<T>()` | Регистрация DbContext + `IMigratorService` для применения миграций |
| **Конфигурация хостов** | `JoinRpgHostNamesOptions` | Имена хостов всех сервисов JoinRpg (основной сайт, IdPortal, KogdaIgra, рейтинг) |
| **MVC фильтры** | `SerilogMvcFilter`, `SerilogRazorPagesFilter` | Обогащение логов данными из HTTP-контекста для MVC и Razor Pages |

---

## Требования по конфигурации

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DataProtection": "Host=localhost;Port=5432;Database=joinrpg;Username=joinrpg;Password=..."
  },
  "Logging": {
    "Structured": false,
    "SelfLogEnabled": false,
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "JoinRpgHostNames": {
    "MainHost": "joinrpg.ru",
    "IdHost": "id.joinrpg.ru",
    "KogdaIgraHost": "kogda-igra.ru",
    "RatingHost": "rating.bastilia.ru"
  }
}
```

### Описание параметров

#### ConnectionStrings

| Ключ | Обязателен | Описание |
|------|------------|----------|
| `DataProtection` | Да | Строка подключения PostgreSQL для хранения ключей Data Protection |
| Любой другой | По необходимости | Строки подключения для каждого DbContext, зарегистрированного через `AddJoinEfCoreDbContext` |

#### Logging

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| `Structured` | bool | `false` | `true` — JSON-формат логов (для production/Elasticsearch), `false` — текстовый формат (для разработки) |
| `SelfLogEnabled` | bool | `false` | Включить вывод внутренних ошибок Serilog в stderr |
| `LogLevel` | Dictionary | `{}` | Уровни логирования по категориям (аналог стандартного `Logging:LogLevel`) |

#### JoinRpgHostNames

| Параметр | Описание |
|----------|----------|
| `MainHost` | Основной сайт JoinRpg (например, `joinrpg.ru`) |
| `IdHost` | Портал аккаунтов (например, `id.joinrpg.ru`) |
| `KogdaIgraHost` | Сервис KogdaIgra |
| `RatingHost` | Сервис рейтинга Бастилии |
