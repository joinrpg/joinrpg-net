using JoinRpg.Web.Models;
using Shouldly;

namespace JoinRpg.Web.Test
{
    internal static class ShouldlyFieldValueViewModelExtensions
    {
        public static void ShouldNotHaveValue(this FieldValueViewModel field) =>
            field.Value.ShouldBeNull();

        public static void ShouldBeEditable(this FieldValueViewModel field) =>
            field.CanEdit.ShouldBeTrue();

        public static void ShouldBeHidden(this FieldValueViewModel field) =>
            field.CanView.ShouldBeFalse();

        public static void ShouldBeReadonly(this FieldValueViewModel field) =>
            field.CanEdit.ShouldBeFalse();

        public static void ShouldBeVisible(this FieldValueViewModel field) =>
            field.CanView.ShouldBeTrue();
    }
}
