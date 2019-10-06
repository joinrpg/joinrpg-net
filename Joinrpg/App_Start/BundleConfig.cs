using System.Web.Optimization;

namespace JoinRpg.Web
{
    internal static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(
                new ScriptBundle("~/bundles/jquery")
                .Include("~/Scripts/lib/jquery/jquery.js",
                "~/Scripts/jquery.popconfirm.js",
                "~/Scripts/lib/jquery-details/jquery.details.js")
                );

            bundles.Add(
                new ScriptBundle("~/bundles/jqueryval")
                .Include("~/Scripts/lib/jquery-validate/jquery.validate.js")
                .Include("~/Scripts/lib/jquery-validate/localization/messages_ru.js")
                .Include("~/Scripts/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js")
                );

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include("~/Scripts/lib/modernizr/modernizr.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/lib/twitter-bootstrap/js/bootstrap.js",
                      "~/Scripts/lib/respond.js/respond.js",
                      "~/Scripts/lib/bootstrap-datepicker/js/bootstrap-datepicker.js",
                      "~/Scripts/lib/bootstrap-datepicker/locales/bootstrap-datepicker.ru.min.js",
                      "~/Scripts/lib/bootstrap-select/js/bootstrap-select.js",
                      "~/Scripts/lib/bootstrap-select/js/i18n/defaults-ru_RU.js",
                      "~/Scripts/bootstrap-select.js"
                      ));

            bundles.Add(new StyleBundle("~/Content/css").Include(
              "~/Scripts/lib/twitter-bootstrap/css/bootstrap.css",
              //"~/Scripts/lib/twitter-bootstrap/css/bootstrap-theme.css",
              "~/Content/site.css",
              "~/Scripts/lib/bootstrap-datepicker/css/bootstrap-datepicker.css",
              "~/Scripts/lib/bootstrap-select/css/bootstrap-select.css"));
        }
    }
}
