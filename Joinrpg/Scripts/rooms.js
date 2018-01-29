var projectId = null;
var roomTypeId = null;
var roomsCount = null;
var roomCapacity = 0;
var roomsCapacity = 0;
var requestsAssignedCount = 0;
var requestsNotAssigned = [];

var unassignedBank = null;
var roomsRows = null;
var rowPlaceholder = null;
var dlgEditRoomName = null;
var dlgChoosePeople = null;
var bnPlaceAll = null;
var bnKickAll = null;

// Shows menu for adding accommodation requests to a specific room
function AddPeople(roomId)
{
    if (requestsNotAssigned.length > 0)
    {
        var row = roomsRows.GetRowById(roomId);
        dlgChoosePeople.PrepareForRoom(row);
        $(dlgChoosePeople).modal("show");
    }
}

function AddMocks()
{
    for (var i = 2; i < 6; i++)
    {
        dlgChoosePeople.AddItem({
            Id: 100,
            Persons: i,
            PersonsList: "Persons: " + i,
            Instance: null
        });
    }
}

// Moves unassigned request from bank to room
function MoveReqToRoom(roomId, req)
{
    var room = roomsRows.GetRowById(roomId);
    for (var i = requestsNotAssigned.length - 1; i >= 0; i--)
        if (requestsNotAssigned[i].Id === req.Id)
        {
            requestsNotAssigned.splice(i, 1);
            break;
        }
    var reqParent = req.Instance.parentElement;
    req.Instance.remove();
    reqParent.remove();

    var cells = room.getElementsByTagName("td");

    // Saving request in rooms list
    room.requests.push(req);

    // Updating occupancy counter
    room.occupancy += req.Persons;
    cells[1].innerHTML = room.occupancy + " / " + roomCapacity;

    // Adding person to corresponding cell
    var item = document.createElement("div");
    item.innerHTML = '<button type="button" class="btn btn-xs btn-default glyphicon glyphicon-remove" title="Выселить из комнаты" onclick="Kick('
        + room.roomId + ', '
        + req.Id
        + ')"></button>';
    item.appendChild(req.Instance);
    cells[2].appendChild(item);

    requestsAssignedCount++;

    CallToServer(roomId, req.Id, true);
}

// Places all requests to all rooms
function PlaceAll()
{
    UpdateGlobalButtons();
}

function DoKick(roomId, id)
{
    var i, req, room = roomsRows.GetRowById(roomId);
    for (i = room.requests.length - 1; i >= 0; i--)
    {
        req = room.requests[i];
        if (req.Id === id)
        {
            room.requests.splice(i, 1);
            break;
        }
    }
    if (i < 0)
        return;

    var reqParent = req.Instance.parentElement;
    req.Instance.remove();
    reqParent.remove();

    var cells = room.getElementsByTagName("td");

    // Updating occupancy counter
    room.occupancy -= req.Persons;
    cells[1].innerHTML = room.occupancy + " / " + roomCapacity;

    // Placing item to unassigned requests bank
    var item = document.createElement("span");
    $(item).addClass("list-group-item");
    item.appendChild(req.Instance);
    unassignedBank.appendChild(item);
    requestsNotAssigned.push(req);

    requestsAssignedCount--;

    CallToServer(roomId, req.Id, false);
}

function CallToServer(roomId, reqId, occupy)
{
    var url = '/' + projectId + '/rooms/'
        + (occupy ? 'occupyroom' : 'unoccupyroom')
        + '?roomTypeId=' + roomTypeId
        + '&room=' + roomId
        + '&reqId=' + reqId;

    $.ajax(url, { method: "HEAD" })
        .done(function (data, status, xr)
        {
            if (xr.status !== 200)
                // Unknown success code -- have to reload page
                location.reload();
        })
        .fail(function (xr, status, error)
        {
            if (occupy)
                ErrorOccupation();
            else
                ErrorUnOccupation();
        });
}

// Kicks specific accommodation request from room
function Kick(roomId, id)
{
    DoKick(roomId, id);
    UpdateGlobalButtons();
    UpdateRoomButtons(roomId);
}

// Kicks all people from specific room
function KickPeople(roomId)
{
    var room = roomsRows.GetRowById(roomId);
    var name = roomsRows.GetName(room);
    if (ConfirmKickAllFromRoom(name))
    {
        for (var i = room.requests.length - 1; i >= 0; i--)
        {
            req = room.requests[i];
            DoKick(roomId, req.Id);
        }

        UpdateGlobalButtons();
        UpdateRoomButtons(roomId);
    }
}

// Kicks everybody from all rooms
function KickAll(href)
{
    if (ConfirmKickAllFromRoomType())
    {
        location.href = href;
    }
}

function UpdateRoomButtons(roomId)
{
    var room = roomsRows.GetRowById(roomId);
    $("#delete" + roomId).prop("disabled", room.occupancy !== 0);
    $("#kick" + roomId).prop("disabled", room.requests.length === 0);
    $("#add" + roomId).prop("disabled", room.occupancy === roomCapacity);
}

function UpdateGlobalButtons()
{
    $(bnPlaceAll).prop("disabled", requestsNotAssigned.length === 0);
    $(bnKickAll).prop("disabled", requestsAssignedCount === 0);

    if (requestsNotAssigned.length === 0)
        $("#availRequests").hide();
    else
        $("#availRequests").show();
}

function AddRoom()
{
    dlgEditRoomName.roomId = null;
    dlgEditRoomName.roomName = "";
    dlgEditRoomName.pnRoomNameTitle.innerHTML = "Добавление комнат";
    dlgEditRoomName.edRoomName.value = "";
    $(dlgEditRoomName.addComment).show();
    $(dlgEditRoomName).modal("show");
    dlgEditRoomName.edRoomName.focus();
}

function DeleteRoom(id)
{
    var row = roomsRows.GetRowById(id);
    var name = roomsRows.GetName(row);
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
        var name = roomsRows.GetName(row);

        dlgEditRoomName.roomId = id;
        dlgEditRoomName.roomName = name;
        dlgEditRoomName.pnRoomNameTitle.innerHTML = "Изменение комнаты";
        dlgEditRoomName.edRoomName.value = name;
        $(dlgEditRoomName.addComment).hide();
        $(dlgEditRoomName).modal("show");
        dlgEditRoomName.edRoomName.focus();
    }
}

function DoEditRoom(id, name)
{
    var row = null;
    var url = url = "/" + projectId + "/rooms/";
    if (id == null)
    {
        url += "addroom?roomTypeId=" + roomTypeId + "&name=" + name;
    }
    else
    {
        row = roomsRows.GetRowById(id);
        roomsRows.SetName(row, name);
        $(row.bnRename).prop("disabled", true);
        url += "editroom?roomTypeId=" + roomTypeId + "&room=" + id + "&name=" + name;
    }

    $.ajax(url, { method: "GET" })
        .done(function (data, status, xr)
        {
            if (id != null)
                $(row.bnRename).prop("disabled", false);

            if (xr.status === 200)
            {
                if (id == null)
                    location.reload();
                //$(roomsRows).append(xr.responseText);
            }
            else if (xr.status === 500)
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
            if (id != null)
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

function parseIntDef(value, def)
{
    value = parseInt(value);
    if (isNaN(value))
        value = def;
    return value;
}

$(function()
{
    // Maps DOM nodes to array with accommodation requests
    function loadInstances(list)
    {
        for (var i = 0; i < list.length; i++)
            list[i].Instance = document.getElementById("req" + list[i].Id);
    }

    loadInstances(requestsNotAssigned);

    unassignedBank = document.getElementById("availRequestsList");
    roomsRows = document.getElementById("roomsRows");
    rowPlaceholder = document.getElementById("rowPlaceholder");
    $(rowPlaceholder).toggle(roomsCount == 0);

    roomsRows.GetRowById = function(roomId)
    {
        return document.getElementById("room" + roomId);
    };
    roomsRows.GetName = function(row)
    {
        return row.getElementsByTagName("td").item(0).innerHTML;
    };
    roomsRows.SetName = function(row, name)
    {
        row.getElementsByTagName("td").item(0).innerHTML = name;
    }

    bnPlaceAll = document.getElementById("bnPlaceAll");
    bnKickAll = document.getElementById("bnKickAll");
    UpdateGlobalButtons();

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
                room.requests = [];
                eval("room.requests = " + room.getAttribute("requests"));
                loadInstances(room.requests);
                room.bnAddPeople = document.getElementById("add" + room.roomId);
                $(room.bnAddPeople).prop("disabled", room.occupancy === roomCapacity);
                room.bnKickPeople = document.getElementById("kick" + room.roomId);
                $(room.bnKickPeople).prop("disabled", room.occupancy === 0);
                room.bnRename = document.getElementById("rename" + room.roomId);
                room.bnDelete = document.getElementById("delete" + room.roomId);
                $(room.bnDelete).prop("disabled", room.occupancy > 0);
            }
        }
    }

    // Add/edit room dialog initialization
    dlgEditRoomName = document.getElementById("dlgEditRoomName");
    dlgEditRoomName.pnRoomNameTitle = document.getElementById("pnRoomNameTitle");
    dlgEditRoomName.edRoomName = document.getElementById("edRoomName");
    dlgEditRoomName.bnEditRoomOk = document.getElementById("bnEditRoomOk");
    dlgEditRoomName.addComment = document.getElementById("addComment");
    $(dlgEditRoomName.edRoomName).keypress(function()
    {
        var newValue = String(dlgEditRoomName.edRoomName.value).trim();
        dlgEditRoomName.bnEditRoomOk.disabled =
            newValue.length == 0 || newValue == dlgEditRoomName.roomName;
    });
    $(dlgEditRoomName.bnEditRoomOk).click(function()
    {
        var newValue = String(dlgEditRoomName.edRoomName.value).trim();
        if (newValue.length == 0 || newValue == dlgEditRoomName.roomName)
            return;
        $(dlgEditRoomName).modal("hide");
        DoEditRoom(dlgEditRoomName.roomId, newValue);
    });

    // Place people dialog initialization
    dlgChoosePeople = document.getElementById("dlgChoosePeople");
    dlgChoosePeople.spRoomName = document.getElementById("spRoomName");
    dlgChoosePeople.spUsedSpace = document.getElementById("spUsedSpace");
    dlgChoosePeople.spFreeSpace = document.getElementById("spFreeSpace");
    dlgChoosePeople.freeSpace = 0;
    dlgChoosePeople.usedSpace = 0;
    dlgChoosePeople.selected = 0;
    dlgChoosePeople.peopleList = document.getElementById("peopleList");
    dlgChoosePeople.bnChoosePeopleOk = document.getElementById("bnChoosePeopleOk");
    dlgChoosePeople.ChangeSpace = function(val)
    {   // if val > 0, free space added
        dlgChoosePeople.freeSpace += val;
        dlgChoosePeople.spFreeSpace.innerHTML = dlgChoosePeople.freeSpace;
        dlgChoosePeople.usedSpace -= val;
        dlgChoosePeople.spUsedSpace.innerHTML = dlgChoosePeople.usedSpace;
    };
    dlgChoosePeople.UpdateItemsVisibility = function(val)
    { // if requests people count > val, such a request will be hidden
        var item, items = dlgChoosePeople.peopleList.getElementsByTagName("div");
        for (var i = 0; i < items.length; i++)
        {
            item = items[i];
            if (!$(item).hasClass("active"))
            {
                if (item.req.Persons > val)
                    $(item).addClass("disabled");
                else
                    $(item).removeClass("disabled");
            }
        }
    };
    dlgChoosePeople.PrepareForRoom = function(row)
    {
        var name = roomsRows.GetName(row);
        dlgChoosePeople.roomId = row.roomId;
        dlgChoosePeople.spRoomName.innerHTML = name;
        dlgChoosePeople.freeSpace = roomCapacity - row.occupancy;
        dlgChoosePeople.spFreeSpace.innerHTML = dlgChoosePeople.freeSpace;
        dlgChoosePeople.usedSpace = row.occupancy;
        dlgChoosePeople.spUsedSpace.innerHTML = dlgChoosePeople.usedSpace;
        dlgChoosePeople.selected = 0;
        dlgChoosePeople.UpdateButton();

        // Creating list of persons for occupation
        dlgChoosePeople.peopleList.innerHTML = "";
        var req;
        for (var i = 0; i < requestsNotAssigned.length; i++)
        {
            req = requestsNotAssigned[i];
            if (req.Persons <= dlgChoosePeople.freeSpace)
                dlgChoosePeople.AddItem(req);
        }
    };
    dlgChoosePeople.UpdateButton = function()
    {
        $(dlgChoosePeople.bnChoosePeopleOk).prop("disabled", !(dlgChoosePeople.selected !== 0));
    };
    dlgChoosePeople.AddItem = function(req)
    {
        var item = document.createElement("div");
        item.setAttribute("class", "list-group-item");
        item.innerHTML = '<span class="join-ellipsis">'
            + req.PersonsList
            + '</span><span class="badge">'
            + req.Persons
            + '</span>';
        item.count = req.Persons;
        item.req = req;
        $(item).click(function (ev)
        {
            var obj = ev.currentTarget;
            if (!$(obj).hasClass("disabled"))
            {
                if ($(obj).hasClass("active"))
                {
                    dlgChoosePeople.ChangeSpace(obj.req.Persons);
                    $(obj).removeClass("active");
                    dlgChoosePeople.selected--;
                } else
                {
                    dlgChoosePeople.ChangeSpace(-obj.req.Persons);
                    $(obj).addClass("active");
                    dlgChoosePeople.selected++;
                }
                dlgChoosePeople.UpdateItemsVisibility(dlgChoosePeople.freeSpace);
                dlgChoosePeople.UpdateButton();
            }
        });

        dlgChoosePeople.peopleList.appendChild(item);
    };
    $(dlgChoosePeople.bnChoosePeopleOk).click(function()
    {
        $(dlgChoosePeople).modal("hide");

        var item, items = dlgChoosePeople.peopleList.getElementsByTagName("div");
        for (var i = 0; i < items.length; i++)
        {
            item = items[i];
            if ($(item).hasClass("active"))
            {
                MoveReqToRoom(dlgChoosePeople.roomId, item.req);
            }
        }

        UpdateRoomButtons(dlgChoosePeople.roomId);
        UpdateGlobalButtons();
    });
});
