// Бейджи PrintableBadge.razor: подбор размера шрифта строки под ширину бейджа.
//
// От максимального размера (его уже выставила разметка) вниз двоичным поиском, но не ниже минимального.
// Если и на минимальном строка не влезает — включаем перенос (CSS ограничивает его двумя строками).
//
// Модуль нужен только страницам с бейджами, поэтому он не входит в JS-инициализатор компонентов.
// Статическая страница печати подключает его тегом — тогда он сам подгоняет все бейджи после загрузки
// шрифтов и перед печатью. Интерактивный компонент (превью в редакторе шаблона) импортирует модуль
// и вызывает fitBadges после каждой отрисовки.

const BADGE_LINE_SELECTOR = '.join-printable-badge-line';
const BADGE_WRAP_CLASS = 'join-printable-badge-wrap';
const FONT_STEP_PT = 0.25;

function fitBadgeLine(line) {
    const min = Number(line.dataset.minFontPt);
    const max = Number(line.dataset.maxFontPt);
    if (!(min > 0) || !(max >= min)) {
        return;
    }

    line.classList.remove(BADGE_WRAP_CLASS);
    line.style.fontSize = `${max}pt`;

    // Скрытый бейдж мерить не по чему: ширина нулевая и «влезает» всё. Оставляем максимальный шрифт.
    if (line.clientWidth === 0) {
        return;
    }

    const fits = () => line.scrollWidth <= line.clientWidth;
    if (fits()) {
        return;
    }

    // lo — наибольший проверенный размер, который влезает (или min, если не влезает ничего).
    let lo = min;
    let hi = max;
    while (hi - lo > FONT_STEP_PT) {
        const mid = (lo + hi) / 2;
        line.style.fontSize = `${mid}pt`;
        if (fits()) {
            lo = mid;
        } else {
            hi = mid;
        }
    }

    line.style.fontSize = `${lo}pt`;
    if (!fits()) {
        line.classList.add(BADGE_WRAP_CLASS);
    }
}

function fitBadgesNow(root) {
    root.querySelectorAll(BADGE_LINE_SELECTOR).forEach(fitBadgeLine);
}

// Мерить надо настоящим шрифтом, а не запасным, поэтому сначала ждём загрузки шрифтов.
export async function fitBadges(root) {
    await document.fonts?.ready;
    fitBadgesNow(root);
}

fitBadges(document);
// Синхронно: продолжение после await браузер может выполнить уже после того, как свёрстал печать.
window.addEventListener('beforeprint', () => fitBadgesNow(document));
