using System.Text.Json;

namespace JoinRpg.PrimitiveTypes.Test;

public class IdentificationCommonTest
{
    private static string[] SkipISpanParsable = [];

    [SkippableTheory()]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldImplementISpanParsable(Type type)
    {
        Skip.If(SkipISpanParsable.Contains(type.Name));

        type.IsAssignableTo(typeof(ISpanParsable<>).MakeGenericType(type)).ShouldBeTrue();
    }

    [SkippableTheory()]
    [ClassData(typeof(ProjectIdDataSource))]
    public void ShouldImplementPolymorphicParse(Type type)
    {
        Skip.If(SkipISpanParsable.Contains(type.Name));

        var x = TryEasyConstruct(type);
        ProjectEntityIdParser.TryParseId(x.ToString(), out var id).ShouldBeTrue();
        id.ShouldBe(x);
    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldBeConstructedEasily(Type type)
    {
        TryEasyConstruct(type).ShouldNotBeNull();
    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldImplementIComparable(Type type)
    {
        type.IsAssignableTo(typeof(IComparable<>).MakeGenericType(type)).ShouldBeTrue();
    }

    private static object TryEasyConstruct(Type type)
    {
        var parameters = new List<object?>();
        for (var paramCount = 1; paramCount < 5; paramCount++)
        {
            parameters.Add(paramCount);
            try
            {
                var instance = Activator.CreateInstance(type, parameters.ToArray());
                return instance.ShouldNotBeNull();
            }
            catch
            {

            }
        }
        throw new Exception("Failed to found easy constructor");
    }

    [Theory]
    [ClassData(typeof(ProjectIdDataSource))]
    public void ShouldBeConstructedWithProjectId(Type type)
    {
        if (type == typeof(ProjectIdentification))
        {
            return;
        }
        TryEasyConstruct(type).ShouldNotBeNull();
    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void IfAcceptsProjectIdShouldImplementIProjectEntityId(Type type)
    {
        if (type == typeof(ProjectIdentification))
        {
            return;
        }
        var acceptsProjectEntityId = type.GetConstructors().Any(c => c.GetParameters().Any(
            p => p.ParameterType == typeof(ProjectIdentification)
            || p.ParameterType.IsAssignableTo(typeof(IProjectEntityId))
            || p.Name?.Equals("ProjectId", StringComparison.InvariantCultureIgnoreCase) == true
            ));

        if (acceptsProjectEntityId)
        {
            type.IsAssignableTo(typeof(IProjectEntityId)).ShouldBeTrue();
        }
        else
        {
            type.IsAssignableTo(typeof(IProjectEntityId)).ShouldBeFalse();
        }

    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldHaveFromOptional(Type type)
    {
        var methods = type.GetMethods().Where(m => m.IsStatic && m.Name == "FromOptional");
        methods.ShouldNotBeEmpty();
    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldRoundTripThroughJson(Type type)
    {
        var instance = TryEasyConstruct(type);
        var serialized = JsonSerializer.Serialize(instance, type).ShouldNotBeNull();
        var deserialized = JsonSerializer.Deserialize(serialized, type).ShouldNotBeNull();
        deserialized.ShouldBeEquivalentTo(instance);
    }

    [SkippableTheory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldRoundTripThroughText(Type type)
    {
        Skip.If(SkipISpanParsable.Contains(type.Name));
        var instance = TryEasyConstruct(type);
        var serialized = instance.ToString().ShouldNotBeNull();
        var parseMethod = type.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)]).ShouldNotBeNull();
        var deserialized = parseMethod.Invoke(null, [serialized, null]);
        deserialized.ShouldBeEquivalentTo(instance);
    }

    [SkippableTheory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldHaveParseMethodDirectlyNotAtInterface(Type type)
    {
        Skip.If(SkipISpanParsable.Contains(type.Name));
        type.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)]).ShouldNotBeNull();
    }

    [SkippableTheory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldHaveSpanParseMethodDirectlyNotAtInterface(Type type)
    {
        Skip.If(SkipISpanParsable.Contains(type.Name));
        type.GetMethod("Parse", [typeof(ReadOnlySpan<char>), typeof(IFormatProvider)]).ShouldNotBeNull();
    }
}
