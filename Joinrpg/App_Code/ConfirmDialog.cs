using System.Web.Mvc;

namespace JoinRpg.Web
{
    public static class ConfirmDialogMvcHelper
    {

        public static MvcHtmlString ConfirmDialog(
            this HtmlHelper self,
            string confirmId,
            string confirmInfo,
            string confirmHeader,
            string confirmYes,
            string confirmNo)
        {
            return MvcHtmlString.Create(string.Format(@"<div class=""modal fade"" tabindex=""-1"" role=""dialog"" id=""{4}"">
  <div class=""modal-dialog"" role=""document"">
    <div class=""modal-content"">
      <div class=""modal-header"">
        <button type=""button"" class=""close"" data-dismiss=""modal"" aria-label=""Close""><span aria-hidden=""true"">&times;</span></button>
        <h4 class=""modal-title"">{0}</h4>
      </div>
      <div class=""modal-body"">
        <p>{1}</p>
      </div>
      <div class=""modal-footer"">
        <button type=""button"" class=""btn btn-default"" data-dismiss=""modal"">{2}</button>
        <button type=""submit"" class=""btn btn-primary"">{3}</button>
      </div>
    </div><!-- /.modal-content -->
  </div><!-- /.modal-dialog -->
</div><!-- /.modal -->",
                confirmHeader,
                confirmInfo,
                confirmNo,
                confirmYes,
                confirmId));
        }
    }
}
