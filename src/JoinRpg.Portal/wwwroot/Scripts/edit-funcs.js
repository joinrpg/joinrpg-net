var tryNumber = 0;
jQuery("input[type=submit]").click(function () {
    var self = $(this);

    if (self.closest("form").valid()) {
        if (tryNumber > 0) {
            tryNumber++;
            alert("Мы уже отправили форму на сервер, ждем результата...");
            return false;
        }
        else {
            tryNumber++;
        }
    };
    return true;
});

$("[data-toggle='confirmation']").popConfirm({
    yesBtn: "OK",
    noBtn: "Отмена",
    title: "Подтверждение операции",
    container: "body"
});
$(".datepicker").datepicker({
    autoclose: true
});

//TODO: merge this

$(".modaldialogforid").on("show.bs.modal", function (event) {
    var modal = $(this);
    var button = $(event.relatedTarget);

    var entityId = button.data("element");
    modal.find("#entityId").val(entityId);

    var href = button.data("action-url");
    if (href) {
        modal.find("form").attr({
            "action": href
        });
    }
});

var hash = window.location.hash.substr(1);
if (hash) {
    $("#" + hash).collapse('show');
}

$(function () {
    $('[data-toggle="tooltip"]').tooltip();
});

$('.require-element-id')
    .on('show.bs.modal',
        function (event) {
            var button = $(event.relatedTarget);
            var id = button.data('element');
            var modal = $(this);
            modal.find('#elementId').val(id);
        });

function addAntiforgeryTokenBeforeSend(xhr) {
  xhr.setRequestHeader("X-CSRF-TOKEN",
    $('input:hidden[name="__RequestVerificationToken"]').val());
}
