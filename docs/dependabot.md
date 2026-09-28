# Dependabot: что он обновляет, а что нет

Конфигурация — [`.github/dependabot.yml`](../.github/dependabot.yml). Две экосистемы:
`nuget` (корень решения, версии централизованы в `Directory.Packages.props`) и `github-actions`.
Обновления группируются (`net`, `efcore`, `xunit`, `serilog`, `opentelemetry` и т.д.), чтобы
не получать по PR на каждый пакет и чтобы dependabot не упирался в таймаут.

## Prerelease-пакеты dependabot практически не обновляет

Если текущая версия пакета — prerelease, NuGet-апдейтер dependabot принимает в кандидаты
только такие prerelease, у которых **числовая часть версии совпадает с текущей**
(`VersionFinder.CreateVersionFilter` в `dependabot-core`):

```
1.11.0-beta.1 -> 1.11.0-beta.2   — предложит
1.11.0-beta.1 -> 1.11.2-beta.1   — не предложит
1.11.0-beta.1 -> 1.19.1-beta.1   — не предложит
```

Кроме этого проходит переход на стабильную версию старше текущей. Для пакета, у которого
стабильных версий не бывает вовсе, это означает, что dependabot **молча не предложит ничего**:
не пропущенный PR, а ноль кандидатов на уровне апдейтера.

Опции «разрешить prerelease» в конфиге нет: в
[Dependabot options reference](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference)
такого ключа не существует, `versioning-strategy` для NuGet не поддерживается, а `allow`,
`ignore` и `groups` на этот фильтр не влияют. Поведение обсуждалось в апстриме и закрыто
как *not planned* — [dependabot-core#1926](https://github.com/dependabot/dependabot-core/issues/1926),
[dependabot-core#3109](https://github.com/dependabot/dependabot-core/issues/3109).

### Как с этим жить

Prerelease-only пакеты — **ручные**. Рядом с такой строкой в `Directory.Packages.props`
должен стоять комментарий, чтобы следующий человек не расследовал заново, почему версия
отстала. Сейчас такой пакет один:

- `OpenTelemetry.Exporter.Prometheus.AspNetCore` — экспортёр Prometheus у OpenTelemetry .NET
  выпускается только в бетах. Версия должна идти вровень с остальными `OpenTelemetry.*`,
  иначе экспортёр молча собирает ноль метрик и `/metrics` отдаёт пустое тело (#5047).
  Расхождение ловится интеграционным тестом `MetricsEndpointTests`.

Проверять такие пакеты имеет смысл при разборе группового PR `opentelemetry`: если группа
приехала на новый minor, а экспортёр — нет, его надо поднять руками тем же PR.
