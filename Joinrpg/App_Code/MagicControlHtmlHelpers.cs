using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.App_Code
{
    public static class MagicControlHtmlHelpers
    {

        public static MvcHtmlString MagicSelectParent<TModel>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = (IProjectIdAware)self.GetModel();

            var value = self.GetValue(expression).ToList();
            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false,
                ShowImplicitGroups.Parents, MagicControlStrategy.NonChanger, metadata.PropertyName, value, false);
        }

        public static MvcHtmlString MagicSelectBindGroups<TModel>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = (IProjectIdAware)self.GetModel();

            var value = self.GetValue(expression).ToList();
            var metadata = ModelMetadata.FromLambdaExpression(expression, self.ViewData);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false,
                ShowImplicitGroups.Children, MagicControlStrategy.NonChanger, metadata.PropertyName, value, true);
        }

        public static MvcHtmlString MagicSelectGroupParent<TModel>(this HtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : EditCharacterGroupViewModel
        {
            var container = (EditCharacterGroupViewModel)self.GetModel();

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
