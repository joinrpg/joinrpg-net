using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using JoinRpg.Domain;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.App_Code
{
    public static class MvcHtmlHelpers
    {
        public static string GetDescription<TModel, TValue>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, TValue>> expression)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);
            return metadata.Description;
        }

        public static MvcHtmlString DescriptionFor<TModel, TValue>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, TValue>> expression)
        {
            var description = self.GetDescription(expression);

            if (string.IsNullOrWhiteSpace(description))
            {
                return MvcHtmlString.Empty;
            }

            // ReSharper disable once UseStringInterpolation we are inside Razor
            return MvcHtmlString.Create(string.Format(@"<div class=""help-block"">{0}</div>", description));
        }

        /// <summary>
        /// Renders dictionary to attributes
        /// </summary>
        public static IHtmlString RenderAttrs(this HtmlHelper self, Dictionary<string, string> attrs)
        {
            string s = string.Empty;
            foreach (var kv in attrs)
                s += s + @" " + kv.Key + @"=""" + self.AttributeEncode(kv.Value) + @"""";
            return self.Raw(s);
        }

        /// <summary>
        /// Converts payment status to CSS display property value
        /// </summary>
        public static string PaymentStatusToDisplayStyle(this ClaimFeeViewModel self, ClaimPaymentStatus status)
        {
            return self.PaymentStatus == status ? "inline" : "none";
        }

        /// <summary>
        /// Renders specified number as a price to html tag
        /// </summary>
        public static MvcHtmlString RenderPriceElement(this HtmlHelper self, int price, string id = null)
        {
            return self.RenderPriceElement(price.ToString(), id);
        }

        /// <summary>
        /// Renders specified value as a price to html tag
        /// </summary>
        public static MvcHtmlString RenderPriceElement(this HtmlHelper self, string price, string id = null)
        {
            //TODO[Localize]
            if (!string.IsNullOrWhiteSpace(id))
                id = id.Trim();
            return MvcHtmlString.Create("<span "
                + (string.IsNullOrWhiteSpace(id) ? "" : @"id=" + id)
                + @" class=""price-value price-RUR"">" + price + "</span>");
        }

        public readonly static string defaultPriceTemplate = @"{0}" + (char)0x00A0 + (char)0x20BD;

        /// <summary>
        /// Renders price to a string
        /// </summary>
        public static string RenderPrice(this HtmlHelper self, int price, string template = null)
        {
            //TODO[Localize]
            return string.Format(template ?? defaultPriceTemplate, price);
        }        

        public static MvcHtmlString HelpLink(string link, string message)
        {
            return new MvcHtmlString("<span class=\"glyphicon glyphicon-question-sign\"></span><a href=\"http://docs.joinrpg.ru/ru/latest/" + link +
                                     "\">" + message + "</a>");
        }

        public static TValue GetValue<TModel, TValue>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, TValue>> expression)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);
            return (TValue) metadata.Model;
        }

        public static TModel GetModel<TModel>(this HtmlHelper<TModel> self)
        {
            return (TModel) ModelMetadata.FromLambdaExpression(m => m, self.ViewData).Model;
        }

        public static MvcHtmlString MagicSelectParent<TModel>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = (IProjectIdAware) self.GetModel();

            var value = self.GetValue(expression).ToList();
            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false,
                ShowImplicitGroups.Parents, MagicControlStrategy.NonChanger, metadata.PropertyName, value, false);
        }

        public static MvcHtmlString MagicSelectBindGroups<TModel>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = (IProjectIdAware) self.GetModel();

            var value = self.GetValue(expression).ToList();
            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false,
                ShowImplicitGroups.Children, MagicControlStrategy.NonChanger, metadata.PropertyName, value, true);
        }

        public static MvcHtmlString MagicSelectGroupParent<TModel>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : EditCharacterGroupViewModel
        {
            var container = (EditCharacterGroupViewModel) self.GetModel();

            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false, ShowImplicitGroups.Parents,
                MagicControlStrategy.Changer, metadata.PropertyName, container.CharacterGroupId.PrefixAsGroups(),
                false);
        }

        public static MvcHtmlString MagicSelectBind<TModel>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = self.GetModel();

            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);

            var value = self.GetValue(expression);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, true, ShowImplicitGroups.Children,
                MagicControlStrategy.NonChanger, metadata.PropertyName, value, true);
        }
    }
}
