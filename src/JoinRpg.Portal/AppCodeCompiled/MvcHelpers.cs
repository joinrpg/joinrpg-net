using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Net;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace JoinRpg.Portal;

public static class MvcHtmlHelpers
{
    private static string? TryGetDescription<TModel, TValue>(this IHtmlHelper<TModel> self,
        Expression<Func<TModel, TValue>> expression)
    {
        ModelMetadata modelMetadata = self.GetMetadataFor(expression);

        var description = modelMetadata.Description;

        if (!string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        //Try to get enum description

        if (modelMetadata.ModelType == typeof(Enum))
        {
            var e = (Enum)self.GetUntypedModelFor(expression);
            var dispAttr = e.GetAttribute<DisplayAttribute>();

            return dispAttr?.Description;
        }

        return null;
    }

    public static IHtmlContent DescriptionFor<TModel, TValue>(
        this IHtmlHelper<TModel> self,
        Expression<Func<TModel, TValue>> expression)
    {
        var description = self.TryGetDescription(expression);
        if (string.IsNullOrWhiteSpace(description))
        {
            return HtmlString.Empty;
        }

        return new HtmlString(@$"<div class=""help-block"">{description}</div>");
    }

    /// <summary>
    /// Renders dictionary to attributes
    /// </summary>
    public static IHtmlContent RenderAttrs(this IHtmlHelper self, Dictionary<string, string> attrs)
    {
        var s = string.Empty;
        foreach (var kv in attrs)
        {
            s += s + @" " + kv.Key + @"=""" + WebUtility.HtmlEncode(kv.Value) + @"""";
        }

        return self.Raw(s);
    }

    /// <summary>
    /// Converts payment status to CSS display property value
    /// </summary>
    public static string PaymentStatusToDisplayStyle(this ClaimFeeViewModel self, ClaimPaymentStatus status) => self.PaymentStatus == status ? "inline" : "none";

    /// <summary>
    /// Renders specified number as a price to html tag
    /// </summary>
    public static IHtmlContent RenderPriceElement(this IHtmlHelper self, int price, string? id = null) => self.RenderPriceElement(price.ToString(), id);

    /// <summary>
    /// Renders specified value as a price to html tag
    /// </summary>
    public static IHtmlContent RenderPriceElement(this IHtmlHelper self, string price, string? id = null)
    {
        //TODO[Localize]
        if (!string.IsNullOrWhiteSpace(id))
        {
            id = id.Trim();
        }

        return new HtmlString("<span "
                              + (string.IsNullOrWhiteSpace(id) ? "" : @"id=" + id)
                              + @" class=""price-value price-RUR"">" + price + "</span>");
    }

    public readonly static string defaultPriceTemplate = @"{0}" + (char)0x00A0 + (char)0x20BD;

    /// <summary>
    /// Renders price to a string
    /// </summary>
    public static string RenderPrice(this IHtmlHelper self, int price, string? template = null)
    {
        //TODO[Localize]
        return string.Format(template ?? defaultPriceTemplate, price);
    }

    public static Task<IHtmlContent> HelpLink(this IHtmlHelper self, string link = "", string message = "")
    {
        return self.RenderComponentAsync<WebComponents.HelpLink>(RenderMode.Static, new { link, message });
    }

    public static TValue GetValue<TModel, TValue>(this IHtmlHelper<TModel> self,
        Expression<Func<TModel, TValue>> expression) => (TValue)self.GetUntypedModelFor(expression);

    public static TModel GetModel<TModel>(this IHtmlHelper<TModel> self) => self.GetValue(m => m);

    private static IModelExpressionProvider GetModelExpressionProvider<TModel>(this IHtmlHelper<TModel> self)
        => (IModelExpressionProvider)self.ViewContext.HttpContext.RequestServices.GetService(typeof(IModelExpressionProvider))!;

    private static ModelExpression GetModelExplorer<TModel, TValue>(
        this IHtmlHelper<TModel> self, Expression<Func<TModel, TValue>> expression) =>
        self.GetModelExpressionProvider().CreateModelExpression(self.ViewData, expression);

    public static ModelMetadata GetMetadataFor<TModel, TValue>(
        this IHtmlHelper<TModel> self, Expression<Func<TModel, TValue>> expression)
        => self.GetModelExplorer(expression).Metadata;

    private static object GetUntypedModelFor<TModel, TValue>(
        this IHtmlHelper<TModel> self, Expression<Func<TModel, TValue>> expression)
        => self.GetModelExplorer(expression).Model;

    public static HtmlString GetFullHostName(this IHtmlHelper self, HttpRequest request) => new(request.Scheme + "://" + request.Host);
}
