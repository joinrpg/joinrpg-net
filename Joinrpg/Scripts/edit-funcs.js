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
$(".datepicker").datepicker({
    autoclose: true
});


$('#deleteElementModal').on('show.bs.modal', function(event) {
    var button = $(event.relatedTarget);
    var plotElementId = button.data('element');
    var modal = $(this);
    modal.find('#deletePlotElementId').val(plotElementId);
});