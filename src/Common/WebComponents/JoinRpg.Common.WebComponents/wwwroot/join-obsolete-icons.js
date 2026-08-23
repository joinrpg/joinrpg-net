// Иконка для старых скриптов, которые строят разметку строками: joinIcon("FeePaid").
//
// Новый код так не пишут: в .razor есть компонент JoinIcon, в .cshtml — тег-хелпер <join-icon />.
// Этот файл подключают только те страницы, где такой скрипт ещё остался, и вместе с ним
// нужен JoinIconMarkup.BuildScript() — он объявляет спрайт и нужные странице иконки.
window.joinIcon = function (icon) {
    var symbol = window.joinIconSymbols[icon];
    if (!symbol) {
        throw new Error('Иконка ' + icon + ' не объявлена в JoinIconMarkup.BuildScript() на этой странице');
    }
    return '<svg class="join-icon" aria-hidden="true" focusable="false">'
        + '<use href="' + window.joinIconSprite + '#' + symbol + '"></use>'
        + '</svg>';
};
