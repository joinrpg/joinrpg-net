function Delete(id, href)
{
    var row = document.getElementById("value" + id);
    $("#valueToDeleteTitle").text(row.getAttribute("title"));    
    $("#bnStartDelete").click(function ()
    {
        $("#bnStartDelete").off("click");
        DoDelete(id, href);
    });
    $("#dlgDeleteValue").modal("show");
    return false;
}

function DoDelete(id, href)
{
    $("#dlgDeleteValue").modal("hide");

    var valueId = "value" + id;
    var row = document.getElementById(valueId);
    row.className += " deleting";

    var xr = $.ajax(href, { method: "DELETE" })
        .done(function (data, status, xr)
        {
            if (xr.status == 200)
            {
                // 200 means that this value have to be removed
                row.remove();
            }
            else if (xr.status == 250)
            {
                // 250 means that this value have to be hidden
                row.className = row.className.replace("deleting", "deleted");
                var btn = document.getElementById(valueId + "DeleteButton");
                btn.remove();
                btn = document.getElementById(valueId + "EditButton");
                btn.setAttribute("title", "Восстановить");
                btn.firstElementChild.className = btn.firstElementChild.className.replace("-pencil", "-heart");
            }
            else
            {
                // Unknown success code -- have to reload page
                location.reload();
            }
        })
        .fail(function (xr, status, error)
        {
            // Operation failed
            var dlg = document.getElementById("dlgDeleteProgressText");
            dlg.className = "modal-body alert alert-danger";
            dlg.innerHTML = "Не удалось удалить";            
            $("#dlgDeleteProgress").modal({ keyboard: true });
            $("#dlgDeleteProgress").modal("show");
            setTimeout(function ()
            {
                $("#dlgDeleteProgress").modal("hide");
                location.reload();
            }, 3000);
        });
}
