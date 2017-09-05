function Delete(id, href)
{
    var row = document.getElementById("value" + id);
    $("#valueToDeleteTitle").text(row.getAttribute("title"));
    $("#bnStartDelete").click(function () { DoDelete(id, href); });
    $("#dlgDeleteValue").modal("show");
    return false;
}

function DoDelete(id, href)
{
    $("#dlgDeleteValue").modal("hide");
    var dlg = document.getElementById("dlgDeleteProgressText");
    dlg.setAttribute("class", "modal-body alert alert-info");
    dlg.innerHTML = "Удаление...";
    $("#dlgDeleteProgress").modal("show");

    var valueId = "value" + id;
    var row = document.getElementById(valueId);

    var xr = new XMLHttpRequest();

    // called when action finished
    function load()
    {
        if (xr.status >= 300)
        {
            error();
            return;
        }

        dlg.setAttribute("class", dlg.getAttribute("class").replace("-info", "-success"));
        $("#dlgDeleteProgress").modal({ keyboard: true });
        if (xr.status == 250)
        {
            dlg.innerHTML = "Значение помечено как неактивное";
            var btn = document.getElementById(valueId + "DeleteButton");
            btn.remove();
            btn = document.getElementById(valueId + "EditButton");
            btn.setAttribute("title", "Восстановить");
            btn.firstElementChild.setAttribute("class", btn.firstElementChild.getAttribute("class").replace("-pencil", "-heart"));
        }
        else if (xr.status == 200)
        {
            dlg.innerHTML = "Успешно удалено";
        }
        else
        {
            dlg.setAttribute("class", dlg.getAttribute("class").replace("-info", "-warning"));
            dlg.innerHTML = "Сервер ответил неизвестным кодом";
        }
        setTimeout(function ()
        {
            $("#dlgDeleteProgress").modal("hide");
            if (xr.status != 250)
                location.reload();
        }, 1000);
    }

    // called if any error occured
    function error()
    {
        dlg.setAttribute("class", dlg.getAttribute("class").replace("-info", "-danger"));
        dlg.innerHTML = "Не удалось удалить";
        $("#dlgDeleteProgress").modal({ keyboard: true });
        setTimeout(function ()
        {
            $("#dlgDeleteProgress").modal("hide");
            location.reload();
        }, 5000);
    }

    xr.addEventListener("load", load);
    xr.addEventListener("error", error);
    xr.addEventListener("abort", error);

    xr.open("DELETE", href, true);
    xr.send();
}
