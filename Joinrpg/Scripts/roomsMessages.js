function ConfirmKickAllFromRoom(name)
{
    return confirm("Выселить всех из комнаты '" + name + "'?");
}

function ConfirmKickAllFromRoomType()
{
    return confirm("Вы точно уверены, что хотите выселить ВСЕХ участников из ВСЕХ комнат этого типа?");
}

function ConfirmKickAll()
{
    return confirm("Вы точно уверены, что хотите выселить ВСЕХ участников из ВСЕХ комнат ВСЕХ типов?");
}

function ConfirmPlaceAll()
{
    return confirm("Приступить к заселению ВСЕХ нерасселенных участников по ВСЕМ типам комнат?");
}

function ErrorOccupation()
{
    alert("Произошла ошибка при заселении в комнату");
}

function ErrorUnOccupation()
{
    alert("Произошла ошибка при выселении из комнаты");
}

function ErrorEdit()
{
    alert("Произошла ошибка при добавлении или изменении комнат");
}

function ErrorDelete()
{
    alert("Произошла ошибка при попытке удаления комнаты");
}
