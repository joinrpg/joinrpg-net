using System.Text.Json;

namespace JoinRpg.PrimitiveTypes.Test;

public class IdentificationCommonTest
{
    // Это временный вайтлист. Не надо добавлять сюда ничего
    public static string[] SkipISpanParsable = ["AvatarIdentification", "CharacterGroupIdentification", "CharacterIdentification", "ClaimIdentification",
    "PlotVersionIdentification", "ProjectFieldIdentification", "ProjectFieldVariantIdentification"];

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
    [ClassData(typeof(WhiteListDataSource))]
    public void WhiteListIsActual(Type type)
    {
        // Если это тест начал падать, надо удалить тип из вайтлиста
        _ = Should.Throw<ArgumentException>(() => type.IsAssignableTo(typeof(ISpanParsable<>).MakeGenericType(type)));
    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldBeConstructedEasily(Type type)
    {
        TryEasyConstruct(type).ShouldNotBeNull();
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
        var parameters = new List<object?>() { new ProjectIdentification(1) };
        for (var paramCount = 2; paramCount < 5; paramCount++)
        {
            parameters.Add(paramCount);
            try
            {
                var instance = Activator.CreateInstance(type, parameters.ToArray());
                return;
            }
            catch
            {

            }

        }
        true.ShouldBeFalse("There is no easy constructor for type");
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
}
