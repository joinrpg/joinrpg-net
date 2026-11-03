# CLAUDE.md

Все инструкции для агентов живут в [AGENTS.md](AGENTS.md). Прочитай его полностью и следуй ему.

## Скиллы и агенты — симлинки

`.claude/skills/` и `.claude/agents/` — симлинки на общие для нескольких инструментов
каталоги [`.agents/skills/`](.agents/skills/) и [`.agents/agents/`](.agents/agents/).
Если ты **не видишь** скиллов и агентов (каталоги пусты, отсутствуют или вместо них
текстовые файлы-заглушки с путём) — **сообщи пользователю**: клонирование, вероятно,
прошло без `core.symlinks` (на Windows — без режима разработчика). Ссылки нужно
восстановить: удалить `.claude/skills` и `.claude/agents` и пересоздать как симлинки
на `.agents/skills` и `.agents/agents`, либо переклонировать репозиторий
(`git config --global core.symlinks true` + режим разработчика на Windows).
Подробности — [docs/kimi-setup.md](docs/kimi-setup.md).
