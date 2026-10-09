// Подсказки компонента Tooltip.razor и раскрытие JoinCollapsePanel.razor по якорю.
//
// Это JS-инициализатор Blazor: имя файла обязано совпадать с PackageId библиотеки, а сам файл
// лежать в её wwwroot. Blazor импортирует такой модуль сам — и в Blazor Web App (Portal, IdPortal),
// и в standalone WebAssembly (каталог компонентов), — но только когда стартует интерактивный рантайм.
// На страницах Portal из одних статических компонентов он не стартует, поэтому layout'ы Portal
// подключают модуль ещё и тегом (Views/Shared/Layout/_WebComponentsScriptPartial.cshtml).
//
// Почему вообще нужен скрипт. Подсказка объявлена как popover, то есть браузер рисует её в top layer:
// там её не обрезают предки с overflow: hidden (шапка и левая колонка сетки расписания) и не перебивает
// чужой z-index. Но показать popover можно только из JS (showPopover), а координаты в top layer
// считаются от окна — отсюда getBoundingClientRect и выставление left/top.
//
// Слушатели навешиваются на document один раз, поэтому Blazor-острова, отрисованные позже,
// работают без дополнительной регистрации.

const CONTAINER_SELECTOR = '.join-tooltip-container';
const TOOLTIP_SELECTOR = ':scope > .join-tooltip';

const GAP = 5; // Отступ подсказки от цели
const EDGE = 4; // Минимальный отступ подсказки от края окна
const SHOW_DELAY = 100; // Чтобы подсказки не мигали, когда курсор просто проезжает мимо

const OPPOSITE_PLACEMENT = { bottom: 'top', top: 'bottom', left: 'right', right: 'left' };

// Контейнер, для которого подсказка показана или вот-вот покажется (отложена по таймеру).
let activeContainer = null;
let shownTooltip = null;
let showTimer = null;

function clamp(value, min, max) {
    return max < min ? min : Math.max(min, Math.min(value, max));
}

function coordsFor(placement, target, size) {
    switch (placement) {
        case 'top':
            return { left: target.left + target.width / 2 - size.width / 2, top: target.top - GAP - size.height };
        case 'left':
            return { left: target.left - GAP - size.width, top: target.top + target.height / 2 - size.height / 2 };
        case 'right':
            return { left: target.right + GAP, top: target.top + target.height / 2 - size.height / 2 };
        default:
            return { left: target.left + target.width / 2 - size.width / 2, top: target.bottom + GAP };
    }
}

// Проверяем только ту ось, по которой подсказка отодвинута от цели: по второй оси подсказку
// всё равно можно подвинуть вдоль цели, не меняя сторону.
function fitsOnPlacementAxis(placement, coords, size, viewWidth, viewHeight) {
    return placement === 'top' || placement === 'bottom'
        ? coords.top >= EDGE && coords.top + size.height <= viewHeight - EDGE
        : coords.left >= EDGE && coords.left + size.width <= viewWidth - EDGE;
}

function place(tooltip, container) {
    // Сначала снимаем прошлые координаты, иначе подсказка у края окна измерится уже перенесённой.
    tooltip.style.left = '0px';
    tooltip.style.top = '0px';

    const target = container.getBoundingClientRect();
    const size = tooltip.getBoundingClientRect();
    const viewWidth = document.documentElement.clientWidth;
    const viewHeight = document.documentElement.clientHeight;

    let placement = container.dataset.joinTooltipPlacement || 'bottom';
    let coords = coordsFor(placement, target, size);

    if (!fitsOnPlacementAxis(placement, coords, size, viewWidth, viewHeight)) {
        const opposite = OPPOSITE_PLACEMENT[placement] || 'top';
        const oppositeCoords = coordsFor(opposite, target, size);
        if (fitsOnPlacementAxis(opposite, oppositeCoords, size, viewWidth, viewHeight)) {
            placement = opposite;
            coords = oppositeCoords;
        }
    }

    tooltip.style.left = clamp(coords.left, EDGE, viewWidth - EDGE - size.width) + 'px';
    tooltip.style.top = clamp(coords.top, EDGE, viewHeight - EDGE - size.height) + 'px';
}

function hide() {
    clearTimeout(showTimer);
    showTimer = null;
    activeContainer = null;

    if (shownTooltip === null) {
        return;
    }

    const tooltip = shownTooltip;
    shownTooltip = null;
    window.removeEventListener('scroll', hide, true);
    window.removeEventListener('resize', hide);

    try {
        tooltip.hidePopover();
    } catch {
        // Подсказку уже убрали из DOM вместе с островом — скрывать нечего.
    }
}

function show(container) {
    const tooltip = container.querySelector(TOOLTIP_SELECTOR);
    if (tooltip === null) {
        return;
    }

    try {
        tooltip.showPopover();
    } catch {
        return; // Браузер без Popover API: подсказки не будет, остальная страница работает.
    }

    shownTooltip = tooltip;
    place(tooltip, container);

    // Подсказка стоит в координатах окна, при прокрутке она бы отъехала от цели.
    window.addEventListener('scroll', hide, true);
    window.addEventListener('resize', hide);
}

function requestShow(target) {
    const container = target instanceof Element ? target.closest(CONTAINER_SELECTOR) : null;

    if (container === activeContainer) {
        return; // Курсор просто ходит внутри цели, подсказка уже показана или уже запланирована.
    }

    hide();

    if (container !== null) {
        activeContainer = container;
        showTimer = setTimeout(() => show(container), SHOW_DELAY);
    }
}

document.addEventListener('pointerover', event => requestShow(event.target));
document.addEventListener('focusin', event => requestShow(event.target));
document.addEventListener('focusout', hide);
document.addEventListener('pointerleave', hide);
document.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
        hide();
    }
});

// Сворачиваемые панели JoinCollapsePanel.razor: ссылка с якорем на панель её раскрывает.
// Браузер сам прокручивает к <details>, но открывать его по якорю на сам элемент не обязан.

function openPanelFromHash() {
    const id = decodeHash(location.hash.slice(1));
    const target = id === '' ? null : document.getElementById(id);
    if (target instanceof HTMLDetailsElement) {
        target.open = true;
    }
}

// Битый якорь (#50%) не должен ронять модуль: он ещё и JS-инициализатор Blazor, и исключение
// при его выполнении не даёт стартовать WebAssembly-островам на странице.
function decodeHash(hash) {
    try {
        return decodeURIComponent(hash);
    } catch {
        return hash;
    }
}

openPanelFromHash();
window.addEventListener('hashchange', openPanelFromHash);
