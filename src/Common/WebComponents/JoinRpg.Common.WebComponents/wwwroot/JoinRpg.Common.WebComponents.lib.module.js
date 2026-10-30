// Подсказки компонента Tooltip.razor, раскрытие JoinCollapsePanel.razor по якорю, закрытие меню
// JoinDropdownButton.razor и списков TypedSelector.razor по клику мимо и по Esc, поиск в статических TypedSelector.
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

// Панели в WebAssembly-островах без prerender появляются в DOM позже, чем выполняется модуль, —
// тогда ждём появления элемента с этим id.
let pendingHashObserver = null;

function openPanelFromHash() {
    pendingHashObserver?.disconnect();
    pendingHashObserver = null;

    const id = decodeHash(location.hash.slice(1));
    if (id === '' || openPanel(id)) {
        return;
    }

    pendingHashObserver = new MutationObserver(() => {
        if (openPanel(id)) {
            pendingHashObserver.disconnect();
            pendingHashObserver = null;
        }
    });
    pendingHashObserver.observe(document.documentElement, { childList: true, subtree: true });
}

// Возвращает true, когда элемент с id нашёлся (панель он или нет) — дальше ждать нечего.
function openPanel(id) {
    const target = document.getElementById(id);
    if (target === null) {
        return false;
    }
    if (target instanceof HTMLDetailsElement) {
        target.open = true;
        keepInView(target);
    }
    return true;
}

// Браузер прокручивает к якорю, пока панель ещё свёрнута или её нет, а острова догружают содержимое
// позже — страница растёт, и панель остаётся внизу окна. Поэтому держим её у верха окна, пока растёт
// страница, но не дольше FOLLOW_MS и не после того, как пользователь сам взялся листать.
const FOLLOW_MS = 5000;
const USER_SCROLL_EVENTS = ['wheel', 'touchstart', 'keydown', 'pointerdown'];
let stopKeepingInView = null;

function keepInView(target) {
    stopKeepingInView?.();
    target.scrollIntoView();

    const resizeObserver = new ResizeObserver(() => target.scrollIntoView());
    resizeObserver.observe(document.documentElement);
    const timer = setTimeout(stop, FOLLOW_MS);
    USER_SCROLL_EVENTS.forEach(type => window.addEventListener(type, stop, { capture: true, passive: true }));

    function stop() {
        resizeObserver.disconnect();
        clearTimeout(timer);
        USER_SCROLL_EVENTS.forEach(type => window.removeEventListener(type, stop, { capture: true }));
        stopKeepingInView = null;
    }
    stopKeepingInView = stop;
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

// Кнопка «Отмена» внутри статической формы в теле панели (data-join-collapse-close) сворачивает
// панель: у статического рендеринга нет обработчиков Blazor, а @onclick на JoinButton там не сработает.
document.addEventListener('click', event => {
    const button = event.target instanceof Element ? event.target.closest('[data-join-collapse-close]') : null;
    const toggle = button?.closest('.join-collapse-panel')?.querySelector(':scope > .panel-heading > .join-collapse-panel__toggle');
    if (toggle) {
        toggle.open = false;
        // Кнопка ушла в свёрнутое тело — возвращаем фокус на заголовок панели, а не на body.
        toggle.querySelector(':scope > summary')?.focus();
    }
});

// Меню JoinDropdownButton.razor и выпадающие списки TypedSelector.razor: открывает и закрывает их браузер
// (<details>), а здесь — только закрытие по клику мимо и по Esc, как привыкли у выпадающих меню.
// Открытое меню одно: клик по другому закрывает прежнее. Атрибут open Blazor не рендерит,
// поэтому трогать его можно и в интерактивных островах.

const POPUP_SELECTOR = 'details.join-dropdown, details.join-select';
const OPEN_POPUP_SELECTOR = 'details.join-dropdown[open], details.join-select[open]';

function closePopups(except) {
    document.querySelectorAll(OPEN_POPUP_SELECTOR).forEach(popup => {
        if (popup !== except) {
            popup.open = false;
        }
    });
}

document.addEventListener('click', event => {
    const target = event.target instanceof Element ? event.target : null;
    // Клик по пункту меню тоже закрывает меню: пункт может и не уводить со страницы (скачивание, новая вкладка).
    // У списка клик внутри панели не закрывает: в нём ищут и отмечают несколько пунктов.
    const keepOpen = target?.closest('.join-dropdown__menu a, .join-dropdown__menu button') ? null : target?.closest(POPUP_SELECTOR);
    closePopups(keepOpen ?? null);
});
document.addEventListener('keydown', event => {
    if (event.key !== 'Escape' || document.querySelector(OPEN_POPUP_SELECTOR) === null) {
        return;
    }
    // Внутри модального <dialog> Esc иначе закроет заодно и диалог (JoinMoveControl в JoinDialog).
    event.preventDefault();
    // Фокус внутри закрываемого меню иначе уходит на body, и Tab начинает обход с начала страницы.
    const focused = document.activeElement?.closest(OPEN_POPUP_SELECTOR);
    closePopups(null);
    focused?.querySelector(':scope > summary')?.focus();
});
// Ушли Tab'ом из меню — закрываем, иначе оно висит поверх страницы.
// Фокус, ушедший на предка меню, — не уход: в модальном <dialog> нажатие на текст пункта (не на саму галочку)
// отдаёт фокус самому диалогу. Закрой мы меню на этом mousedown, клик попал бы уже мимо пункта, и выбрать
// можно было бы только точным попаданием в галочку (JoinMoveControl в JoinDialog).
document.addEventListener('focusout', event => {
    const popup = event.target instanceof Element ? event.target.closest(OPEN_POPUP_SELECTOR) : null;
    const next = event.relatedTarget;
    if (popup && next instanceof Node && !popup.contains(next) && !next.contains(popup)) {
        popup.open = false;
    }
});

// TypedSelector.razor. В интерактивном режиме поиск и текст на кнопке ведёт C#. В статическом
// (data-join-select-static) Blazor в браузере не исполняется — это делают слушатели ниже, а строку
// поиска каждого пункта (data-search) уже нормализовал сервер.

const SELECT_SELECTOR = 'details.join-select';

// Внутри отключённого <fieldset> (панель настроек без права) пункты отключает сам браузер, а <summary> — нет,
// и список раскрывался бы. Enter и пробел на <summary> тоже приходят сюда кликом.
document.addEventListener('click', event => {
    const summary = event.target instanceof Element ? event.target.closest(`${SELECT_SELECTOR} > summary`) : null;
    if (summary?.closest('fieldset:disabled')) {
        event.preventDefault();
    }
}, true);

const STATIC_SELECT_SELECTOR = 'details.join-select[data-join-select-static]';

// Повторяет TypedSelectSearch.Normalize на C#: менять вместе.
function normalizeSearch(text) {
    let result = '';
    let pendingSpace = false;
    for (const ch of text.toLowerCase().normalize('NFD')) {
        const code = ch.charCodeAt(0);
        if (code >= 0x300 && code <= 0x36F) {
            continue; // Диакритика: бреве у «й», диерезис у «ё».
        }
        if (ch.trim() === '') {
            pendingSpace = result.length > 0;
            continue;
        }
        if (pendingSpace) {
            result += ' ';
            pendingSpace = false;
        }
        result += ch;
    }
    return result.normalize('NFC');
}

// Подпись FormRow ссылается на <summary> списка (label for), но <summary> не labelable — браузер по клику
// на подпись ничего не сделает. Открываем список сами и ставим фокус в поиск, а без поиска — на кнопку.
// Слушатель закрытия меню выше зарегистрирован раньше: он закроет другие открытые списки, а этот откроем здесь.
document.addEventListener('click', event => {
    const label = event.target instanceof Element ? event.target.closest('label[for]') : null;
    const summary = label ? document.getElementById(label.htmlFor) : null;
    const select = summary?.matches(`${SELECT_SELECTOR} > summary`) ? summary.parentElement : null;
    if (!select) {
        return;
    }
    event.preventDefault();
    select.open = true;
    (select.querySelector('.join-select__search') ?? summary).focus();
});

function filterStaticSelect(select) {
    const search = select.querySelector('.join-select__search');
    const words = normalizeSearch(search?.value ?? '').split(' ').filter(word => word !== '');
    select.querySelectorAll('.join-select__list > li').forEach(item => {
        const itemSearch = item.dataset.search ?? '';
        item.hidden = !words.every(word => itemSearch.includes(word));
    });
}

function refreshStaticSummary(select) {
    const texts = [...select.querySelectorAll('.join-select__list input:checked')]
        .map(checked => checked.closest('label')?.querySelector('.join-select__text')?.textContent ?? '');
    const text = texts.length === 0 ? select.dataset.emptyText ?? '' : texts.join(', ');
    const summary = select.querySelector(':scope > summary');
    const value = summary?.querySelector('.join-select__value');
    if (value) {
        value.textContent = text;
        if (!summary.hasAttribute('aria-labelledby')) {
            summary.title = text; // У подписанного списка значение уже в имени, title читался бы дважды.
        }
    }
}

// Браузер при F5 и «назад» восстанавливает отмеченный пункт и текст поиска без события change/input —
// кнопка и фильтр должны соответствовать тому, что уйдёт в форму.
function refreshStaticSelects() {
    document.querySelectorAll(STATIC_SELECT_SELECTOR).forEach(select => {
        filterStaticSelect(select);
        refreshStaticSummary(select);
    });
}

refreshStaticSelects();
window.addEventListener('pageshow', refreshStaticSelects);

document.addEventListener('input', event => {
    const search = event.target instanceof HTMLInputElement && event.target.classList.contains('join-select__search') ? event.target : null;
    const select = search?.closest(STATIC_SELECT_SELECTOR);
    if (select) {
        filterStaticSelect(select);
    }
});

// Стрелки в группе radio сами отмечают соседний пункт и шлют change — это переход по списку, а не выбор,
// закрывать по нему панель нельзя. Флаг ставим на keydown, change приходит следом.
let arrowNavigation = false;

document.addEventListener('keydown', event => {
    const input = event.target instanceof HTMLInputElement ? event.target : null;
    if (!input?.closest(SELECT_SELECTOR)) {
        return;
    }
    if (input.classList.contains('join-select__search')) {
        if (event.key !== 'Enter') {
            return;
        }
        // Enter в поле поиска иначе отправит всю форму. Как у bootstrap-select — выбираем первый найденный пункт.
        event.preventDefault();
        const first = [...input.closest(SELECT_SELECTOR).querySelectorAll('.join-select__list > li:not([hidden]) input:not(:disabled)')][0];
        first?.click();
        return;
    }
    arrowNavigation = event.key.startsWith('Arrow');
});

document.addEventListener('change', event => {
    const input = event.target instanceof HTMLInputElement ? event.target : null;
    const select = input?.closest(SELECT_SELECTOR);
    if (!select || input.classList.contains('join-select__search')) {
        return;
    }
    if (select.matches(STATIC_SELECT_SELECTOR)) {
        refreshStaticSummary(select);
    }
    const byArrow = arrowNavigation;
    arrowNavigation = false;
    // Одиночный выбор сделан — закрываем, как закрывался обычный <select>. Несколько пунктов отмечают с открытой панелью.
    if (input.type === 'radio' && !byArrow) {
        select.open = false;
        select.querySelector(':scope > summary')?.focus();
    }
});
