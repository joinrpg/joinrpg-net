var projectId = null;
var roomTypeId = null;
var roomsCount = null;
var roomCapacity = 0;

var roomsRows = null;
var rowPlaceholder = null;
var dlgEditRoomName = null;

function AddPeople(roomId)
{

}

function KickPeople(roomId)
{

}

function AddRoom()
{
    dlgEditRoomName.roomId = null;
    dlgEditRoomName.roomName = "";
    dlgEditRoomName.pnRoomNameTitle.innerHTML = "Добавление комнат";
    dlgEditRoomName.edRoomName.value = "";
    $(dlgEditRoomName).modal("show");
    dlgEditRoomName.edRoomName.focus();
}

function DeleteRoom(id)
{
    var row = roomsRows.GetRowById(id);
    var name = row.getElementsByTagName("td").item(0).innerHTML;
    if (confirm("Удалить комнату " + name + "?"))
    {
        $(row.bnDelete).prop("disabled", true);
        DoDeleteRoom(id);
    }
}

function RenameRoom(id)
{
    var row = roomsRows.GetRowById(id);
    if (row != null)
    {
        var name = row.getElementsByTagName("td").item(0).innerHTML;

        dlgEditRoomName.roomId = id;
        dlgEditRoomName.roomName = name;
        dlgEditRoomName.pnRoomNameTitle.innerHTML = "Изменение комнаты";
        dlgEditRoomName.edRoomName.value = name;
        $(dlgEditRoomName).modal("show");
        dlgEditRoomName.edRoomName.focus();
    }
}

function DoEditRoom(id, name)
{
    var row = null;
    if (id == null)
        id = "new";
    else
    {        
        row = roomsRows.GetRowById(id);
        row.getElementsByTagName("td").item(0).innerHTML = name;
        $(row.bnRename).prop("disabled", true);
    }

    var url = "/" + projectId + "/rooms/editroom?roomTypeId=" + roomTypeId + "&room=" + id + "&name=" + name;

    var xr = $.ajax(url, { method: "GET" })
        .done(function (data, status, xr)
        {
            if (id != "new")
                $(row.bnRename).prop("disabled", false);

            if (xr.status == 200)
            {
                $(roomsRows).append(xr.responseText);
            }
            else if (xr.status == 500)
            {
                ErrorEdit();
            }
            else
            {
                // Unknown success code -- have to reload page
                location.reload();
            }
        })
        .fail(function (xr, status, error)
        {
            if (id != "new")
                $(row.bnRename).prop("disabled", false);

            ErrorEdit();
        });
}

function DoDeleteRoom(id)
{
    var url = "/" + projectId + "/rooms/deleteroom?roomTypeId=" + roomTypeId + "&roomId=" + id;
    var xr = $.ajax(url, { method: "DELETE" })
        .done(function (data, status, xr)
        {
            if (xr.status == 200)
            {
                var row = roomsRows.GetRowById(id);
                row.remove();
            }
            else if (xr.status == 500)
            {
                ErrorDelete();
            }
            else
            {
                // Unknown success code -- have to reload page
                location.reload();
            }
        })
        .fail(function (xr, status, error)
        {
            ErrorDelete();
        });
}

function ErrorEdit()
{
    alert("Произошла ошибка при добавлении или изменении комнат");
}

function ErrorDelete()
{
    alert("Произошла ошибка при попытке удаления комнаты")
}

function parseIntDef(value, def)
{
    value = parseInt(value);
    if (isNaN(value))
        value = def;
    return value;
}

$(function ()
{
    roomsRows = document.getElementById("roomsRows");
    rowPlaceholder = document.getElementById("rowPlaceholder");
    $(rowPlaceholder).toggle(roomsCount == 0);

    roomsRows.GetRowById = function (roomId)
    {
        return document.getElementById("room" + roomId);
    };


    // Rows initialization
    if (roomsCount > 0)
    {
        for (var i = 0; i < roomsRows.children.length - 1; i++)
        {
            room = roomsRows.children[i];
            if (room.id.startsWith("room"))
            {
                room.roomId = parseIntDef(room.getAttribute("roomId"), 0);
                room.occupancy = parseIntDef(room.getAttribute("occupancy"), 0);
                room.bnAddPeople = document.getElementById("add" + room.roomId);
                room.bnKickPeople = document.getElementById("kick" + room.roomId);
                room.bnRename = document.getElementById("rename" + room.roomId);
                room.bnDelete = document.getElementById("delete" + room.roomId);
                $(room.bnDelete).prop("disabled", room.occupancy > 0);
            }
        }
    }

    // Dialog initialization
    dlgEditRoomName = document.getElementById("dlgEditRoomName");
    dlgEditRoomName.pnRoomNameTitle = document.getElementById("pnRoomNameTitle");
    dlgEditRoomName.edRoomName = document.getElementById("edRoomName");
    dlgEditRoomName.bnEditRoomOk = document.getElementById("bnEditRoomOk");
    $(dlgEditRoomName.edRoomName).keypress(function ()
    {
        var newValue = String(dlgEditRoomName.edRoomName.value).trim();        
        dlgEditRoomName.bnEditRoomOk.disabled =
            newValue.length == 0 || newValue == dlgEditRoomName.roomName;
    });
    $(dlgEditRoomName.bnEditRoomOk).click(function ()
    {
        var newValue = String(dlgEditRoomName.edRoomName.value).trim();
        if (newValue.length == 0 || newValue == dlgEditRoomName.roomName)
            return;
        $(dlgEditRoomName).modal("hide");
        DoEditRoom(dlgEditRoomName.roomId, newValue);
    });

})
