using System.Diagnostics;
using JoinRpg.Web.Models;
using Shouldly;

namespace JoinRpg.Web.Test;

internal static class ShouldlyFieldValueViewModelExtensions
{
    //Attributes in this class will cause debugger to stop on exception in caller of that method
    //and thats probably what we like to see


    [DebuggerStepThrough]
    public static void ShouldNotHaveValue(this FieldValueViewModel field) =>
        field.Value.ShouldBeNull();

    [DebuggerStepThrough]
    public static void ShouldBeEditable(this FieldValueViewModel field) =>
        field.CanEdit.ShouldBeTrue();

    [DebuggerStepThrough]
    public static void ShouldBeHidden(this FieldValueViewModel field) =>
        field.CanView.ShouldBeFalse();

    [DebuggerStepThrough]
    public static void ShouldBeReadonly(this FieldValueViewModel field) =>
        field.CanEdit.ShouldBeFalse();

    [DebuggerStepThrough]
    public static void ShouldBeVisible(this FieldValueViewModel field) =>
        field.CanView.ShouldBeTrue();
}
