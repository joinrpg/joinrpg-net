
function emailKeyPress(ev) {
    if (ev.charCode === 32) {
        ev.stopPropagation();
        return false;
    }
    return true;
}

function emailPaste(o) {
    o.checkInput = true;
}

function emailInput(o) {
    if (o.checkInput === true) {
        o.value = cutSpaces(o.value.toString());
        o.checkInput = false;
    }
}

function cutSpaces(s) {
    return s.replace(/\s+/g, "");
}
