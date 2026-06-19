# Документация проекта Joinrpg.ru

## Основные документы

### Архитектура
- [Структура проекта](structure.md) — обзор архитектуры и слоёв. Читать, если есть вопрос куда добавлять новые классы или возникнет необходимость модифицировать csproj
- [Правила работы с LINQ-запросами](linq-queries.md) — использование Expression и LinqKit

### Разработка
- [Добавление сущностей метаданных проекта](project-entities.md) — пошаговое руководство
- [Миграции Entity Framework](ef-migrations.md) — работа с миграциями EF6
- [Общие шаги для UI-страниц](ui-pages-common.md) — стартовые решения, слои, паттерн Builder
- [Страницы редактирования](editing-pages-guide.md) — Blazor-острова с мутациями
- [Страницы просмотра](view-pages-guide.md) — read-only Blazor-острова

### Конфигурация
- [Добавление нового окружения](how-to-add-new-env.md) — настройка сред разработки

### Архитектурные решения (ADR)
- [ADR001: Blazor WebAssembly islands](adr001-blazor.md)
- [ADR002: Daily background jobs](adr002-dailyjobs.md)
- [ADR003: Notifications system](adr003-notifications.md)
- [ADR004: OAuth server](adr004-oauth-server.md)
- [ADR005: User blogs](adr005-user-blogs.md)
- [ADR006: Common split](adr006-common-split.md)
- [ADR007: TypedStringValue](adr007-string-value.md)
- [ADR008: Страница просмотра сетки ролей](adr008-roles-list-view-page.md)
- [ADR009: ProjectPropsService — единая точка изменения метаданных проекта](adr009-project-props-service.md)

## Быстрые ссылки

### Распространённые задачи
- **Добавить новую сущность проекта**: Следуйте [project-entities.md](project-entities.md)
- **Создать миграцию**: Смотрите [ef-migrations.md](ef-migrations.md)

### Команды
```bash
# Применить миграции
dotnet run --project src/Joinrpg.Dal.Migrate

# Проверить стиль кода
dotnet format --verify-no-changes --severity error

# Запустить тесты
dotnet test
```

## Обновление документации

При добавлении нового функционала или обнаружении проблем:

1. Определите, нужно ли создать новый документ или дополнить существующий
2. Следуйте стилю существующих документов (кратко, по-русски, с примерами кода)
3. Добавьте ссылку в этот README
4. Убедитесь, что документация актуальна для текущей версии кода

## Стиль документации

- Пишите на русском языке
- Используйте практические примеры из кода проекта
- Включайте команды для выполнения задач
- Упоминайте типичные проблемы и их решения
- Ссылайтесь на конкретные файлы и строки кода
