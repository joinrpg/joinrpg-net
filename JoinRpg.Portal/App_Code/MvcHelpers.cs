using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Net;
using JetBrains.Annotations;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace JoinRpg.Portal
{


    public static class DisplayCount
    {
        [MustUseReturnValue]
        public static IHtmlContent OfX(int count, string single, string multi1, string multi2)
        {
            var selected = count == 0 ? multi2 : (count == 1 ? single : (count < 5 ? multi1 : multi2));
            return new HtmlString(count + " " + selected);
        }
    }
    public static class MvcHtmlHelpers
    {
        //https://stackoverflow.com/a/17455541/408666
        public static IHtmlContent HiddenFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, TProperty value)
        {
            string expressionText = ExpressionHelper.GetExpressionText(expression);
            string propertyName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expressionText);

            return htmlHelper.Hidden(propertyName, value, new { });
        }

        public static string GetDescription<TModel, TValue>(this IHtmlHelper<TModel> self,
            Expression<Func<TModel, TValue>> expression)
        {
            return self.GetMetadataFor(expression).Description;
        }

        private static string TryGetDescription<TModel, TValue>(this IHtmlHelper<TModel> self,
            Expression<Func<TModel, TValue>> expression)
        {
            var description = self.GetDescription(expression);

            if (!string.IsNullOrWhiteSpace(description))
            {
                return description;
            }

            //Try to get enum description

            var metadata = self.GetMetadataFor(expression);
            if (metadata.ModelType == typeof(Enum))
            {
                var e = (Enum)self.GetUntypedModelFor(expression);
                var dispAttr = e.GetAttribute<DisplayAttribute>();

                return dispAttr == null ? null : dispAttr.Description;
            }

            return null;
        }

        public static IHtmlContent DescriptionFor<TModel, TValue>(
            this IHtmlHelper<TModel> self,
            [InstantHandle]
            Expression<Func<TModel, TValue>> expression)
        {
            var description = self.TryGetDescription(expression);
            if (string.IsNullOrWhiteSpace(description))
            {
                return HtmlString.Empty;
            }
            // ReSharper disable once UseStringInterpolation we are inside Razor
            return new HtmlString(string.Format(@"<div class=""help-block"">{0}</div>", description));
        }

        /// <summary>
        /// Renders dictionary to attributes
        /// </summary>
        public static IHtmlContent RenderAttrs(this IHtmlHelper self, Dictionary<string, string> attrs)
        {
            string s = string.Empty;
            foreach (var kv in attrs)
                s += s + @" " + kv.Key + @"=""" + WebUtility.HtmlEncode(kv.Value) + @"""";
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
        public static IHtmlContent RenderPriceElement(this IHtmlHelper self, int price, string id = null)
        {
            return self.RenderPriceElement(price.ToString(), id);
        }

        /// <summary>
        /// Renders specified value as a price to html tag
        /// </summary>
        public static IHtmlContent RenderPriceElement(this IHtmlHelper self, string price, string id = null)
        {
            //TODO[Localize]
            if (!string.IsNullOrWhiteSpace(id))
                id = id.Trim();
            return new HtmlString("<span "
                + (string.IsNullOrWhiteSpace(id) ? "" : @"id=" + id)
                + @" class=""price-value price-RUR"">" + price + "</span>");
        }

        public readonly static string defaultPriceTemplate = @"{0}" + (char)0x00A0 + (char)0x20BD;

        /// <summary>
        /// Renders price to a string
        /// </summary>
        public static string RenderPrice(this IHtmlHelper self, int price, string template = null)
        {
            //TODO[Localize]
            return string.Format(template ?? defaultPriceTemplate, price);
        }

        public static IHtmlContent HelpLink(this IHtmlHelper self, string link, string message)
        {
            return new HtmlString("<span class=\"glyphicon glyphicon-question-sign\"></span><a href=\"http://docs.joinrpg.ru/ru/latest/" + link +
                                     "\">" + message + "</a>");
        }

        public static TValue GetValue<TModel, TValue>(this IHtmlHelper<TModel> self,
            Expression<Func<TModel, TValue>> expression)
        {
            return (TValue)self.GetUntypedModelFor(expression);
        }

        public static TModel GetModel<TModel>(this IHtmlHelper<TModel> self)
        {
            return self.GetValue(m => m);
        }

        public static Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata GetMetadataFor<TModel, TValue>(this IHtmlHelper<TModel> self, Expression<Func<TModel, TValue>> expression)
        {
            return ExpressionMetadataProvider.FromLambdaExpression(expression, self.ViewData, self.MetadataProvider).Metadata;
        }

        private static object GetUntypedModelFor<TModel, TValue>(this IHtmlHelper<TModel> self, Expression<Func<TModel, TValue>> expression)
        {
            return ExpressionMetadataProvider.FromLambdaExpression(expression, self.ViewData, self.MetadataProvider).Model;
        }
    }
}
