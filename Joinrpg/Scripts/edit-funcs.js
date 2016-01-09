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
    format: {
        /*
        Say our UI should display a week ahead,
        but textbox should store the actual date.
        This is useful if we need UI to select local dates,
        but store in UTC
        */
        toDisplay: function (date, format, language) {
            var d = new Date(date);
            d.setDate(d.getDate());
            return d.toISOString();
        },
        toValue: function (date, format, language) {
            var d = new Date(date);
            d.setDate(d.getDate());
            return new Date(d);
        }
    },
    autoclose: true
});