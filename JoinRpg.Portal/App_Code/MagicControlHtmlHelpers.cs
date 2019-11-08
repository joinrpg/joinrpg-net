using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CharacterGroups;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JoinRpg.Portal
{
    public static class MagicControlHtmlHelpers
    {

        public static IHtmlContent MagicSelectParent<TModel>(this IHtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = (IProjectIdAware)self.GetModel();

            var value = self.GetValue(expression).ToList();
            var metadata = self.GetMetadataFor(expression);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false,
                ShowImplicitGroups.Parents, MagicControlStrategy.NonChanger, metadata.PropertyName, value, false);
        }

        public static IHtmlContent MagicSelectBindGroups<TModel>(this IHtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = (IProjectIdAware)self.GetModel();

            var value = self.GetValue(expression).ToList();
            var metadata = self.GetMetadataFor(expression);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false,
                ShowImplicitGroups.Children, MagicControlStrategy.NonChanger, metadata.PropertyName, value, true);
        }

        public static IHtmlContent MagicSelectGroupParent<TModel>(this IHtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : EditCharacterGroupViewModel
        {
            var container = (EditCharacterGroupViewModel)self.GetModel();

            var metadata = self.GetMetadataFor(expression);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, false, ShowImplicitGroups.Parents,
                MagicControlStrategy.Changer, metadata.PropertyName, container.CharacterGroupId.PrefixAsGroups(),
                false);
        }

        public static IHtmlContent MagicSelectBind<TModel>(this IHtmlHelper<TModel> self,
            Expression<Func<TModel, IEnumerable<string>>> expression)
            where TModel : IProjectIdAware
        {
            var container = self.GetModel();

            var metadata = self.GetMetadataFor(expression);

            var value = self.GetValue(expression);

            return MagicControlHelper.GetMagicSelect(container.ProjectId, true, ShowImplicitGroups.Children,
                MagicControlStrategy.NonChanger, metadata.PropertyName, value, true);
        }
    }
}
