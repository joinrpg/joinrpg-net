# Шрифты

Сайт использует Roboto. Файлы шрифта лежат в репозитории и раздаются с нашего сервера, а не
с серверов Google: внешняя зависимость в критическом пути рендера не нужна, а в России
`fonts.googleapis.com` доступен нестабильно.

## Где лежит

Пакет `JoinRpg.Common.WebComponents` (там же, где иконки и прочие статические ресурсы
компонентов):

- `wwwroot/css/roboto.css` — объявления `@font-face`;
- `wwwroot/fonts/roboto-*.woff2` — сами файлы.

Страницы подключают шрифт одной строкой:

```html
<link rel="stylesheet" href="~/_content/JoinRpg.Common.WebComponents/css/roboto.css" />
```

Начертание вариативное: один файл на подмножество символов покрывает веса 100–900
(`font-weight: 100 900`), отдельных файлов под 400 и 700 нет. Подмножеств четыре —
`latin`, `latin-ext`, `cyrillic`, `cyrillic-ext`, по два на каждое (прямое и курсив),
итого 8 файлов, ~270 КБ. Благодаря `unicode-range` браузер скачивает только те, в которых
реально есть символы со страницы — для русского текста это обычно один-два файла.

## Как обновить версию шрифта

1. Запросить CSS у Google Fonts API (версия шрифта в ответе видна в путях `/s/roboto/vNN/`):

   ```bash
   curl -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/140.0.0.0" \
     "https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,100..900;1,100..900&display=swap"
   ```

   User-Agent обязателен: без него API отдаёт устаревший формат (ttf вместо woff2).

2. Из ответа взять блоки нужных подмножеств (`latin`, `latin-ext`, `cyrillic`,
   `cyrillic-ext`), скачать `woff2` по ссылкам, положить в `wwwroot/fonts/` под именами
   `roboto-<подмножество>-<normal|italic>.woff2`.

3. Перенести в `roboto.css` новые `unicode-range` (Google их иногда меняет), заменив
   абсолютные URL на относительные `url('../fonts/<файл>')`. Версию шрифта указать
   в комментарии в начале файла.
