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

$("html").addClass($.fn.details.support ? "details" : "no-details");
$("details").details();
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

$("#deleteElementModal").on("show.bs.modal", function(event) {
    var button = $(event.relatedTarget);
    var plotElementId = button.data("element");
    var modal = $(this);
    modal.find("#deletePlotElementId").val(plotElementId);
});

$("#publishElementModal").on("show.bs.modal", function (event) {
    var button = $(event.relatedTarget);
    var plotElementId = button.data("element");
    var version = button.data("version");
    var modal = $(this);
    modal.find("#publishPlotElementId").val(plotElementId);
    modal.find("#publishVersionId").val(version);
});

$("#unpublishElementModal").on("show.bs.modal", function (event) {
    var button = $(event.relatedTarget);
    var plotElementId = button.data("element");
    var modal = $(this);
    modal.find("#publishPlotElementId").val(plotElementId);
});

$(".modaldialogforid").on("show.bs.modal", function (event) {
  var button = $(event.relatedTarget);
  var entityId = button.data("element");
  var modal = $(this);
  modal.find("#entityId").val(entityId);
});

var hash = window.location.hash.substr(1);
if (hash) {
    $("#" + hash).collapse('show');
}

$(function() {
  $('[data-toggle="tooltip"]').tooltip();
});
