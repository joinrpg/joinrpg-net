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

function addAntiforgeryTokenBeforeSend(xhr) {
  xhr.setRequestHeader("X-CSRF-TOKEN",
    $('input:hidden[name="__RequestVerificationToken"]').val());
}
