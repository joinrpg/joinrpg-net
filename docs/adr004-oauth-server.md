Проблема:
==
Разные сайты экосистемы JoinRpg/КогдаИгра/Бастилия, а в будущем и другие ролевые сайты, в том числе  игровые системы хотят использовать аккаунты JoinRpg для авторизации игроков.
Нужен стандартный OAuth 2.0 / OpenID Connect сервер, которому можно доверять.

Решение:
==
Отдельное ASP.NET Core приложение `JoinRpg.IdPortal` является OAuth/OIDC провайдером на базе **OpenIddict**.

Поддерживается один flow: **Authorization Code Flow** (scope: openid, email, phone, profile).

Подробности
==

Проекты
---
- `JoinRpg.IdPortal` — ASP.NET Core хост; UI управления аккаунтами
- `JoinRpg.IdPortal.OAuthServer` — регистрация эндпоинтов и хендлеров
- `JoinRpg.IdPortal.Client` — Blazor WASM клиент IdPortal

Ключевые эндпоинты
---
| Путь | Назначение |
|------|-----------|
| `connect/authorize` | Авторизация (GET/POST passthrough) |
| `connect/token` | Выдача токена (OpenIddict) |
| `connect/user_info` | Профиль пользователя (GET, требует bearer token) |

UserInfo (`/connect/user_info`)
---

Скоупы и возвращаемые клеймы:
- всегда: `sub`, `preferred_username`, `profile` (URL)
- `email`: `email`, `email_verified`
- `phone`: `phone_number`, `phone_number_verified` (всегда false — не реализовано)
- `profile`: `name`, `given_name`, `family_name`, `middle_name`, `picture` (если есть аватар)

Значения `given_name`, `family_name`, `middle_name` — строки (`string?`), не доменные типы.

БД
---
- IdPortal: PostgreSQL, схема OpenIddict 
- DefaultConnection (SQL Server, EF6): используется для чтения профиля пользователя через `IUserRepository`

Регистрация клиентов
---
`OAuthRegistrator` (hosted service) создаёт OAuth-клиентов при старте из конфигурации `OAuthServer:Clients[]`.

Тесты
---
- Юнит-тесты: `JoinRpg.IdPortal.OAuthServer.Test` 
- Интеграционные тесты: `JoinRpg.IdPortal.Test` — полный flow через Testcontainers (PostgreSQL + SQL Server)
