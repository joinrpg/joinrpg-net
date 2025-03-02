using System.Text.Json;

namespace JoinRpg.PrimitiveTypes.Test;

public class IdentificationCommonTest
{
    [Theory(Skip = "Не все поддерживает IParsable, а должно")]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldImplementIParsable(Type type)
    {
        type.IsAssignableTo(typeof(IParsable<>).MakeGenericType(type)).ShouldBeTrue();
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
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldBeConstructedWithProjectId(Type type)
    {
        if (type == typeof(ProjectIdentification) || !type.IsAssignableTo(typeof(IProjectEntityId)))
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
        var acceptsProjectEntityId = type.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(ProjectIdentification) || p.ParameterType.IsAssignableTo(typeof(IProjectEntityId))));

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
