# Настройка kimi-code для работы с этим репозиторием

kimi-code читает этот репозиторий почти без настройки: инструкции — из корневого `AGENTS.md`,
скиллы — из `.agents/skills/`, субагент ревью — из `.agents/agents/`, MCP-сервер —
из корневого `.mcp.json`. Ниже — то, что нужно сделать руками один раз.

## Permission-правила

Автоматических разрешений из репозитория у kimi-code нет (в отличие от `.claude/settings.json`
для Claude Code). Добавь в `~/.kimi-code/config.toml` блок `[[permission.rules]]` — аналог
текущего `.claude/settings.json`:

```toml
[[permission.rules]]
decision = "allow"
pattern = "Bash(dotnet build*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(dotnet run*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(dotnet test*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(dotnet format*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(dotnet restore*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(dotnet publish*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(dotnet ef*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(gh pr:*)"

[[permission.rules]]
decision = "allow"
pattern = "Bash(gh run view:*)"

[[permission.rules]]
decision = "allow"
pattern = "mcp__cwm-roslyn-navigator__*"

[[permission.rules]]
decision = "allow"
pattern = "WebSearch"

[[permission.rules]]
decision = "allow"
pattern = "FetchURL"
```

Отличия от claude-версии: `WebFetch` в kimi-code называется `FetchURL`, правила пишутся в TOML,
а не в JSON. Дополнительные личные правила (доступ к прод-логам и скриптам Selectel) описаны
в скиллах `.agents/skills/prod-logs/` и `.agents/skills/selectel-readonly/`.

## Симлинки `.claude/` → `.agents/`

`.claude/skills/` и `.claude/agents/` в репозитории — симлинки на `.agents/skills/` и
`.agents/agents/`. На Windows они материализуются только если:

1. включён **режим разработчика** (Параметры → Система → Для разработчиков → Режим разработчика),
2. git клонировал с `core.symlinks=true` (проверь `git config core.symlinks` в репозитории;
   при клоне — `git config --global core.symlinks true`).

Если после клонирования `.claude/skills` — текстовый файл-заглушка вместо каталога, восстанови ссылки:

```bash
rm .claude/skills .claude/agents
ln -s ../.agents/skills .claude/skills
ln -s ../.agents/agents .claude/agents
```

## Первый запуск

```bash
dotnet tool restore   # MCP-сервер cwm-roslyn-navigator — локальный dotnet-инструмент
```

Новый MCP-сервер и новые скиллы подхватываются при старте сессии — после изменений запускай
новую сессию (`/new`) или перезапускай CLI.
