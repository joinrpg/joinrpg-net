-- Выполнить на проде под sysadmin-логином.
-- Пароль подставь сам вместо плейсхолдера прямо в момент выполнения —
-- нигде, кроме твоей головы/менеджера паролей, он оседать не должен.
--
-- Без `GO`: команда-разделитель батчей sqlcmd/SSMS, которую не все клиенты
-- понимают (например, веб-консоли облаков). CREATE LOGIN/CREATE USER
-- не обязаны быть единственным оператором в батче, так что скрипт можно
-- выполнить целиком одним запуском. Если конкретный клиент всё равно
-- ругается на несколько `USE` в одном выполнении — выполняй по разделам
-- 1/2/3 отдельно.

-- 1. Логин на уровне сервера
USE master;
CREATE LOGIN [claude_joinrpg_readonly]
    WITH PASSWORD = N'<ПОДСТАВЬ_СИЛЬНЫЙ_ПАРОЛЬ_ЗДЕСЬ>',
    CHECK_POLICY = ON,
    CHECK_EXPIRATION = ON,
    DEFAULT_DATABASE = [joinrpg-prod];

-- 2. Пользователь в целевой БД + встроенная readonly-роль.
-- Если база восстановлена из бэкапа (как при переезде в Selectel, DRP #5459), пользователь
-- в ней уже есть, но «осиротел»: серверный логин в бэкап базы не входит, а SID нового логина
-- другой. Тогда не создаём пользователя, а перепривязываем его к новому логину.
USE [joinrpg-prod];
IF DATABASE_PRINCIPAL_ID(N'claude_joinrpg_readonly') IS NULL
    CREATE USER [claude_joinrpg_readonly] FOR LOGIN [claude_joinrpg_readonly];
ELSE
    ALTER USER [claude_joinrpg_readonly] WITH LOGIN = [claude_joinrpg_readonly];
ALTER ROLE db_datareader ADD MEMBER [claude_joinrpg_readonly];

-- 3. Проверка: у логина не должно быть доступа ни к чему, кроме [joinrpg-prod] —
-- SQL Server по умолчанию не даёт логину доступ к другим базам, явный DENY не нужен,
-- но полезно перепроверить после создания:
SELECT dp.name AS login_name, db.name AS database_name
FROM sys.server_principals dp
CROSS JOIN sys.databases db
WHERE dp.name = 'claude_joinrpg_readonly'
  AND HAS_PERMS_BY_NAME(db.name, 'DATABASE', 'CONNECT') = 1;
-- Ожидаемый результат: только строка с database_name = 'joinrpg-prod'.

-- Отзыв доступа в будущем, если понадобится:
-- USE [joinrpg-prod]; DROP USER [claude_joinrpg_readonly];
-- USE master; DROP LOGIN [claude_joinrpg_readonly];
