/*
  Разморозка уведомлений, замороженных freeze-notification-queue.sql (Postgres,
  база notifications).

  Зачем. Когда решили, что уведомления из очереди можно отправить (или часть из них —
  например, только show_in_ui), возвращаем им прежний SendAfter из журнала
  drp_frozen_channels. Прежний SendAfter в прошлом, поэтому отправитель подберёт их
  сразу, как только Portal будет запущен.

  Что делает (только с apply):
    - для строк журнала, чей канал всё ещё queued и SendAfter = 'infinity',
      восстанавливает прежний SendAfter;
    - удаляет из журнала размороженные строки, а также строки, чей канал уже не
      заморожен (статус сменился или SendAfter поменяли руками).
  Необязательный фильтр -v channel=email|telegram|show_in_ui — разморозить только
  этот тип канала.

  Запуск:
    psql "host=... dbname=notifications user=..." -f unfreeze-notification-queue.sql
    psql "host=... dbname=notifications user=..." -v channel=show_in_ui -f unfreeze-notification-queue.sql
    psql "host=... dbname=notifications user=..." -v apply=1 [-v channel=...] -f unfreeze-notification-queue.sql

  По умолчанию скрипт ничего не меняет и только показывает план (apply = 0).
*/
\set ON_ERROR_STOP on
\pset footer off

\if :{?apply}
\else
\set apply 0
\endif

-- Пустая строка — все типы каналов.
\if :{?channel}
\else
\set channel ''
\endif

SELECT to_regclass('drp_frozen_channels') IS NOT NULL AS journal_exists \gset
\if :journal_exists
\else
\echo 'Журнала drp_frozen_channels нет — размораживать нечего.'
\quit
\endif

\echo '=== План разморозки (фильтр по каналу:' :'channel' ') ==='
SELECT ch."Channel" AS channel,
       count(*) FILTER (WHERE ch."NotificationMessageStatus" = 'queued' AND ch."SendAfter" = 'infinity') AS will_unfreeze,
       count(*) FILTER (WHERE NOT (ch."NotificationMessageStatus" = 'queued' AND ch."SendAfter" = 'infinity')) AS no_longer_frozen,
       min(j.prev_send_after) AS min_prev_send_after,
       max(j.prev_send_after) AS max_prev_send_after
FROM drp_frozen_channels j
JOIN "NotificationMessageChannels" ch ON ch."NotificationMessageChannelId" = j.notification_message_channel_id
WHERE (:'channel' = '' OR ch."Channel"::text = :'channel')
GROUP BY 1
ORDER BY 1;

SELECT count(*) AS journal_rows_without_channel
FROM drp_frozen_channels j
WHERE NOT EXISTS (
    SELECT 1 FROM "NotificationMessageChannels" ch
    WHERE ch."NotificationMessageChannelId" = j.notification_message_channel_id);

\if :apply
\echo '=== Применяем разморозку ==='
BEGIN;

LOCK TABLE drp_frozen_channels IN EXCLUSIVE MODE;

WITH restored AS (
    UPDATE "NotificationMessageChannels" ch
    SET "SendAfter" = j.prev_send_after
    FROM drp_frozen_channels j
    WHERE j.notification_message_channel_id = ch."NotificationMessageChannelId"
      AND ch."NotificationMessageStatus" = 'queued'
      AND ch."SendAfter" = 'infinity'
      AND (:'channel' = '' OR ch."Channel"::text = :'channel')
    RETURNING ch."NotificationMessageChannelId"
)
SELECT count(*) AS unfrozen FROM restored;

-- Чистим журнал: всё, что по фильтру больше не заморожено (включая только что размороженное).
DELETE FROM drp_frozen_channels j
WHERE NOT EXISTS (
        SELECT 1 FROM "NotificationMessageChannels" ch
        WHERE ch."NotificationMessageChannelId" = j.notification_message_channel_id
          AND ch."NotificationMessageStatus" = 'queued'
          AND ch."SendAfter" = 'infinity')
  AND (:'channel' = ''
       OR EXISTS (
           SELECT 1 FROM "NotificationMessageChannels" ch
           WHERE ch."NotificationMessageChannelId" = j.notification_message_channel_id
             AND ch."Channel"::text = :'channel'));

COMMIT;

SELECT count(*) AS journal_rows_left FROM drp_frozen_channels;
\else
\echo 'Только план. Для применения: -v apply=1'
\endif
