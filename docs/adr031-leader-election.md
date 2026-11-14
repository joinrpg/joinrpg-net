# ADR031. Leader election для фоновых сервисов

## Статус

Реализовано (#6220): namespace `JoinRpg.Common.WebInfrastructure.LeaderElection` в пакете
`JoinRpg.Common.WebInfrastructure`, тесты на Testcontainers в `JoinRpg.Common.WebInfrastructure.Test`.

## Контекст

Нужен простой способ запускать фоновый сервис ровно на одной реплике из N. Конкретный кейс:
bastilia-rating получает обновления Telegram через long polling (`getUpdates`) — Bot API допускает
только одного активного poller'а на бота (второй получает 409 Conflict), а webhook не работает
из-за сетевой блокировки Telegram↔Selectel. Поэтому подобные сервисы сегодня стоят на
`replicas: 1`, что блокирует отказоустойчивость.

## Решение

Leader election на базе PostgreSQL advisory lock (`pg_try_advisory_lock` / `pg_advisory_unlock`):
PostgreSQL уже есть у всех хостов через EF Core, отдельный координационный сервис не нужен.

- **База — существующая БД Data Protection** (`ConnectionStrings:DataProtection`, имя
  переопределяется параметром `connectionStringName`). Advisory locks не требуют таблиц и
  миграций, отдельная база не появляется.
- **Лок живёт на выделенном непулинговом Npgsql-соединении** (`Pooling=false` выставляет
  `PostgresLeaderLock` сам): advisory lock привязан к сессии, а пулинговое соединение EF Core
  вернулось бы в пул раньше unlock'а. При смерти владельца сервер освобождает лок при обрыве
  сессии — вторая реплика подхватывает без ручного вмешательства.
- **Публичный API намеренно минимален** (packable-библиотека): `ILeaderElectedService` +
  `LeaderElectedRegistration.AddLeaderElectedHostedService<TService>(configuration, lockKey = null)`:
  ключ лока — строка, кодируется стабильным SHA-256-хешем в `bigint` (advisory lock принимает
  только bigint; `string.GetHashCode()` нельзя — рандомизирован per-process); по умолчанию ключ —
  имя типа `TService` (appName не используется: несколько лидер-выбираемых сервисов приложения
  поделили бы один лок).
  `ILeaderLock`, `PostgresLeaderLock`, hosted-сервис — internal; тесты ходят через
  `InternalsVisibleTo`.
- `LeaderElectedHostedService<TService>` крутит scoped `TService` с linked-CTS; рядом крутится
  probe-цикл (`SELECT 1` каждые 5 с по соединению, держащему лок): если соединение умерло,
  лок уже потерян — сервису шлётся отмена и лок перезахватывается. Сбои не роняют хост
  (фоновый сервис не должен останавливать приложение — см. #6217).

## Последствия

- Лидерство — best-effort: при сетевом разрыве без смерти процесса две реплики могут на короткое
  время считать себя лидерами; probe сокращает окно до ~5 секунд, но не гарантирует строгую
  взаимную блокировку одновременных исполнений. Для задач «один активный экземпляр» (poller'ы)
  этого достаточно; строгие инварианты по-прежнему решаются на уровне БД.
- Это **не заменяет Data Protection** для интерактивных сценариев (cookie-шифрование и т.п.) —
  разные задачи, разные механизмы.
- Джобы at-most-once в сутки (ADR002) остаются на своей семантике — `IJobRepository` и
  журнал `DailyJobRuns`; leader election про непрерывную работу одного экземпляра, а не про
  расписание.
- Снимает блокер `replicas: 1` для bastilia-rating; само поднятие реплик — в репозитории
  потребителя.
