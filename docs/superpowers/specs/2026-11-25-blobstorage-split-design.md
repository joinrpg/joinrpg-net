# Разделение JoinRpg.BlobStorage: общая S3/файловая инфраструктура в Common.WebInfrastructure

Дата: 2026-11-25
Статус: согласовано, к реализации

## Замысел

`JoinRpg.BlobStorage` сейчас смешивает два пласта: общую инфраструктуру работы с файлами
и S3 (создание клиента, загрузка с ретраями, скачивание по URL, health check) и аватарную
политику (лимит 1 МБ, whitelist image-типов, именование файлов, резолв публичного URI).
Общая часть нужна не только аватаркам — её место в `JoinRpg.Common.WebInfrastructure`
и `JoinRpg.Common.WebInfrastructure.Interfaces`.

## Цели

- Регистрация `IAmazonS3`, `S3UploadHealth`, `HealthCheckS3Storage` живут в
  `JoinRpg.Common.WebInfrastructure` и подключаются из `AddJoinWebPlatform`.
- `AvatarDownloader` обобщается до `FileDownloader` (лимит размера — параметр вызова).
- Из `S3AvatarStorageService` выделяется `FileS3Uploader` (загрузка Stream в S3 с ретраями),
  интерфейсы — в `Common.WebInfrastructure.Interfaces`.
- `JoinRpg.BlobStorage` остаётся только аватарным.

## Не-цели

- Не меняем аватарную политику (лимиты, типы, именование, `AvatarUriResolver`).
- Не выносим `IAvatarStorageService`/`IAvatarUriResolver` из их текущих проектов.
- Не меняем секции конфигурации (`S3BlobStorage`) и формат настроек.

## Ограничения

- `JoinRpg.Common.WebInfrastructure` и `.Interfaces` — пакуемые NuGet-пакеты: новые публичные
  типы требуют обновления `PublicAPI.Unshipped.txt` (`scripts/Update-PublicApi.ps1`),
  записей в CHANGELOG и README (см. `docs/nuget-packages.md`).
- `WebInfrastructure` не должен зависеть от `JoinRpg.Services.*` — поэтому `S3StorageOptions`
  переезжает в `.Interfaces` до остальных шагов.
- `JoinRpg.Services.Interfaces` не пакуемый — переезд `S3StorageOptions` не ломает внешних
  потребителей.

## Рассмотренные варианты

**Размещение S3-кода:**

1. Прямо в `JoinRpg.Common.WebInfrastructure` — выбрано. Проще, без нового проекта;
   цена — транзитивная зависимость `AWSSDK.S3` у всех потребителей пакета (принято).
2. Отдельный пакет-спутник `JoinRpg.Common.WebInfrastructure.S3` — отклонено: новый проект
   в матрице `nuget-publish.yml` ради изоляции зависимости, которая нужна обоим сайтам.

**SHA256-хеш при загрузке:** в `S3AvatarStorageService.StoreAvatar` хеш вычисляется и
выбрасывается (`S3AvatarStorageService.cs:30`, в `PutObjectRequest` не передаётся).
Решено: удалить совсем — SDK сам считает чексуммы. Вариант «возвращать в результате
`UploadFileAsync`» отклонён за ненадобностью.

## Целевая структура

### JoinRpg.Common.WebInfrastructure

- `S3/AmazonS3Registration.cs` — public extension `AddJoinS3Storage()`:
  `IAmazonS3` (синглтон; фабрика `CreateS3Client` с яндекс-совместимым конфигом
  `WHEN_REQUIRED` переезжает как есть), `S3UploadHealth` (синглтон),
  `HealthCheckS3Storage`, `FileS3Uploader`, именованный HttpClient для скачивания,
  `FileDownloader`. Вызывается из `AddJoinWebPlatformCore`
  (`WebInfrastructureRegistration.cs`) — оба сайта (Portal, IdPortal) получают
  регистрацию через платформу; `AddJoinS3Storage` остаётся public для нестандартных хостов и тестов.
- `S3/S3UploadHealth.cs` — переезд без изменений.
- `S3/HealthCheckS3Storage.cs` — переезд; текст «Last avatar upload failed» →
  «Last file upload failed».
- `S3/FileS3Uploader.cs` (internal) — `UploadFileAsync(key, Stream, contentType, ct)`:
  PutObject с ретраями (до 3 попыток при `AmazonS3Exception`), репорт в `S3UploadHealth`.
- `Files/FileDownloader.cs` (internal, бывший `AvatarDownloader`) —
  `DownloadAsync(Uri, Stream target, long maxSize, ct)` → возвращает media type.
  Именованный HttpClient и таймаут переезжают сюда; whitelist image-типов и маппинг
  content-type → расширение отсюда уходят в BlobStorage.

### JoinRpg.Common.WebInfrastructure.Interfaces

- `IFileS3Uploader` — `Task UploadFileAsync(string key, Stream content, string contentType, CancellationToken ct)`;
  сигнатура без AWS-типов.
- `IFileDownloader` — `Task<string> DownloadAsync(Uri source, Stream target, long maxSize, CancellationToken ct)`
  (возвращает media type).
- `FileDownloadException` — рядом с `IFileDownloader`, чтобы BlobStorage мог его ловить.
- `S3StorageOptions` — переезд из `JoinRpg.Services.Interfaces` (смена namespace,
  правка using в `Startup.cs` Portal/IdPortal и в BlobStorage).

Классы реализации — internal, наружу только интерфейсы: меньше публичной поверхности пакета.

### JoinRpg.BlobStorage (остаётся аватарным)

- `S3AvatarStorageService` — оркестратор: качает через `IFileDownloader` (maxSize 1 МБ —
  аватарная политика), маппинг content-type → расширение (переезжает сюда из даунлоадера),
  имя файла (SHA1+random), загрузка через `IFileS3Uploader`, ловит `FileDownloadException` → `null`.
- `AvatarUriResolver`, `StubAvatarStorageService` — без изменений логики (только using опций).
- `BlobStorageRegistration` — только аватарные регистрации; S3-регистрацию больше не делает
  (её даёт `AddJoinWebPlatform`). `Startup.cs` сайтов меняется минимально (using для опций).
- `AvatarDownloadException` — после переезда проверить использования (`find_references`)
  и удалить, если мёртв.

## Изменения поведения (осознанные)

- Ретрай больше не перекачивает файл: сейчас download внутри `while`
  (`S3AvatarStorageService.cs:21-26`), после выноса ретраи остаются только на upload.
- Пропадает бесполезное вычисление SHA256 на каждую загрузку.

## Шаги (каждый — компилируемый коммит)

1. Переезд `S3StorageOptions` в `Common.WebInfrastructure.Interfaces` + правка using
   в `Startup.cs` Portal/IdPortal и в BlobStorage.
2. Переезд S3-инфраструктуры (клиент, `S3UploadHealth`, health check) в WebInfrastructure,
   `AddJoinS3Storage()`, вызов из `AddJoinWebPlatformCore`; `BlobStorageRegistration`
   перестаёт регистрировать S3-часть.
3. `AvatarDownloader` → `FileDownloader` + `IFileDownloader`/`FileDownloadException`:
   обобщение (maxSize параметром), маппинг расширений уезжает в `S3AvatarStorageService`.
4. `FileS3Uploader` + `IFileS3Uploader`, переписывание `S3AvatarStorageService`,
   удаление SHA256.
5. Тесты: `AvatarDownloaderHttpClientTests` и `HealthCheckS3StorageTests` переезжают в
   `Common.WebInfrastructure.Test`; новые тесты ретраев `FileS3Uploader` (успех со 2-й
   попытки; 3 ретрая → throw + `ReportFailure`). `S3AvatarStorageServiceTests` переписать
   на моки `IFileDownloader`/`IFileS3Uploader` вместо наследования от `AmazonS3Client`.
6. Хозяйство пакетов: `pwsh scripts/Update-PublicApi.ps1`, CHANGELOG обоих пакетов,
   README (новые типы и регистрация), `dotnet format`.

## Проверка

- `dotnet build` и юнит-тесты затронутых проектов (`JoinRpg.BlobStorage.Test`,
  `JoinRpg.Common.WebInfrastructure.Test`) локально; интеграционные — на CI.
- `dotnet format --verify-no-changes --severity warn`.
- Ручная проверка сценария: логин через внешнего провайдера с аватаркой → аватарка
  скачана и загружена в S3 (dev-окружение).

## Решения, принятые при проектировании (можно оспорить при ревью)

- Имена методов: `UploadFileAsync` / `DownloadAsync`.
- `FileDownloadException` живёт в `.Interfaces` рядом с контрактом.
- ADR не пишем: решение локальное, фиксируется этой спецификацией и описанием PR.
