/*
  Заморозка очереди уведомлений (Postgres, база notifications) перед запуском Portal
  на восстановленной из старого бэкапа основной БД.

  Зачем. В очереди лежат уведомления, созданные до аварии: со ссылками на заявки,
  персонажей и комментарии, которых в восстановленной MSSQL нет (или под теми же id
  окажутся другие). Отправлять их нельзя, пока не разберёмся. Удалять тоже нельзя —
  это единственная копия текста. Поэтому откладываем отправку навсегда:
  SendAfter = 'infinity'. Отправитель выбирает только строки с
  CURRENT_TIMESTAMP > "SendAfter" (NotificationsRepository.BuildLockRequestSql),
  а now() > 'infinity' всегда ложно.

  Что делает (только с @apply):
    - замораживает все каналы в статусе queued с id <= максимального id на момент
      запуска; уведомления, созданные после запуска Portal, уходят как обычно;
    - перед этим пишет id и прежний SendAfter в журнал drp_frozen_channels —
      по нему работает unfreeze-notification-queue.sql;
    - каналы в статусе sending не трогает, только показывает (после рестарта их
      никто не подберёт: отправитель берёт только queued).
  Повторный запуск безопасен: уже замороженные строки не трогаются, прежний
  SendAfter в журнале не перезаписывается.

  Побочные эффекты заморозки: замороженные каналы остаются queued, поэтому длина
  очереди в метриках (NotificationQueueMetricsPublisher) и на админской панели
  уведомлений будет ненулевой, пока их не разморозить. Уведомления «в интерфейсе»
  (show_in_ui) видны в истории пользователя независимо от статуса.

  Порядок запуска (Portal и IdPortal в это время НЕ запущены):
    1. Прогнать без флага — посмотреть сводку.
    2. Прогнать с -v apply=1.
    3. Запускать приложения.

  Запуск:
    psql "host=... dbname=notifications user=..." -f freeze-notification-queue.sql
    psql "host=... dbname=notifications user=..." -v apply=1 -f freeze-notification-queue.sql

  По умолчанию скрипт ничего не меняет и только показывает сводку (apply = 0).
*/
\set ON_ERROR_STOP on
\pset footer off

\if :{?apply}
\else
\set apply 0
\endif

\echo '=== Каналы по статусу и типу ==='
SELECT ch."NotificationMessageStatus" AS status,
       ch."Channel" AS channel,
       count(*) AS channels,
       count(*) FILTER (WHERE ch."SendAfter" = 'infinity') AS frozen
FROM "NotificationMessageChannels" ch
GROUP BY 1, 2
ORDER BY 1, 2;

\echo '=== queued и sending: даты создания уведомлений и SendAfter ==='
SELECT ch."NotificationMessageStatus" AS status,
       ch."Channel" AS channel,
       count(*) AS channels,
       min(n."CreatedAt") AS min_created_at,
       max(n."CreatedAt") AS max_created_at,
       min(ch."SendAfter") AS min_send_after,
       max(ch."SendAfter") AS max_send_after,
       min(ch."NotificationMessageChannelId") AS min_channel_id,
       max(ch."NotificationMessageChannelId") AS max_channel_id
FROM "NotificationMessageChannels" ch
JOIN "Notifications" n ON n."NotificationMessageId" = ch."NotificationMessageId"
WHERE ch."NotificationMessageStatus" IN ('queued', 'sending')
GROUP BY 1, 2
ORDER BY 1, 2;

\echo '=== Будет заморожено (queued, ещё не infinity) ==='
SELECT ch."Channel" AS channel, count(*) AS channels
FROM "NotificationMessageChannels" ch
WHERE ch."NotificationMessageStatus" = 'queued'
  AND ch."SendAfter" <> 'infinity'
GROUP BY 1
ORDER BY 1;

SELECT to_regclass('drp_frozen_channels') IS NOT NULL AS journal_exists \gset
\if :journal_exists
\echo '=== Журнал drp_frozen_channels ==='
SELECT count(*) AS journal_rows,
       min(frozen_at) AS first_frozen_at,
       max(frozen_at) AS last_frozen_at
FROM drp_frozen_channels;
\endif

\if :apply
\echo '=== Применяем заморозку ==='
BEGIN;

-- Не пускаем параллельные вставки и смену статусов, пока фиксируем максимум id.
-- Читатели (и FOR UPDATE SKIP LOCKED отправителя) этой блокировкой не задерживаются.
LOCK TABLE "NotificationMessageChannels" IN SHARE ROW EXCLUSIVE MODE;

CREATE TABLE IF NOT EXISTS drp_frozen_channels (
    notification_message_channel_id integer PRIMARY KEY,
    prev_send_after timestamp with time zone NOT NULL,
    max_channel_id_at_freeze integer NOT NULL,
    frozen_at timestamp with time zone NOT NULL DEFAULT now()
);

SELECT coalesce(max("NotificationMessageChannelId"), 0) AS max_channel_id
FROM "NotificationMessageChannels" \gset
\echo 'Максимальный id канала на момент заморозки:' :max_channel_id

INSERT INTO drp_frozen_channels (notification_message_channel_id, prev_send_after, max_channel_id_at_freeze)
SELECT ch."NotificationMessageChannelId", ch."SendAfter", :max_channel_id
FROM "NotificationMessageChannels" ch
WHERE ch."NotificationMessageStatus" = 'queued'
  AND ch."NotificationMessageChannelId" <= :max_channel_id
  AND ch."SendAfter" <> 'infinity'
ON CONFLICT (notification_message_channel_id) DO NOTHING;

UPDATE "NotificationMessageChannels" ch
SET "SendAfter" = 'infinity'
FROM drp_frozen_channels j
WHERE j.notification_message_channel_id = ch."NotificationMessageChannelId"
  AND ch."NotificationMessageStatus" = 'queued'
  AND ch."NotificationMessageChannelId" <= :max_channel_id
  AND ch."SendAfter" <> 'infinity';

COMMIT;

\echo '=== После заморозки: queued, которые отправитель может взять сейчас (должно быть 0) ==='
SELECT ch."Channel" AS channel, count(*) AS sendable_now
FROM "NotificationMessageChannels" ch
WHERE ch."NotificationMessageStatus" = 'queued'
  AND CURRENT_TIMESTAMP > ch."SendAfter"
GROUP BY 1
ORDER BY 1;

SELECT count(*) AS journal_rows FROM drp_frozen_channels;
\else
\echo 'Только план. Для применения: -v apply=1'
\endif
