using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace JoinRpg.Portal.Tools
{
    public static class HtmlHelperExtensions
    {
        //https://stackoverflow.com/questions/57821737/replacement-for-expressionhelper-in-asp-net-core-3-0#answer-57821738
        public static string GetExpressionText<TModel, TResult>(
            this IHtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TResult>> expression)
        {
            var expresionProvider = htmlHelper.ViewContext.HttpContext.RequestServices
                .GetService(typeof(ModelExpressionProvider)) as ModelExpressionProvider;

            return expresionProvider.GetExpressionText(expression);
        }
    }
}