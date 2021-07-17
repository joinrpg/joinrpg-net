window.joinmethods = {
  GetDocumentCookie: function () {
    return { content: document.cookie };
  },
  InitBootstrapSelect: function (ref) {
    $(ref).selectpicker();
  },
  RefreshBootstrapSelect: function (ref) {
    $(ref).selectpicker('refresh');
  }
}
