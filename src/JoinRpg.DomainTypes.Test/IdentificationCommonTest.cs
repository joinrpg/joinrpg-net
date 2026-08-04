using System.Text.Json;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Test;

public class IdentificationCommonTest
{
    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldImplementISpanParsable(Type type)
    {
        type.IsAssignableTo(typeof(ISpanParsable<>).MakeGenericType(type)).ShouldBeTrue();
    }

    [Theory]
    [ClassData(typeof(ProjectIdDataSource))]
    public void ShouldImplementPolymorphicParse(Type type)
    {

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

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldRoundTripThroughText(Type type)
    {
        var instance = TryEasyConstruct(type);
        var serialized = instance.ToString().ShouldNotBeNull();
        var parseMethod = type.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)]).ShouldNotBeNull();
        var deserialized = parseMethod.Invoke(null, [serialized, null]);
        deserialized.ShouldBeEquivalentTo(instance);
    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldHaveParseMethodDirectlyNotAtInterface(Type type)
    {
        type.GetMethod("Parse", [typeof(string), typeof(IFormatProvider)]).ShouldNotBeNull();
    }

    [Theory]
    [ClassData(typeof(IdentificationDataSource))]
    public void ShouldHaveSpanParseMethodDirectlyNotAtInterface(Type type)
    {
        type.GetMethod("Parse", [typeof(ReadOnlySpan<char>), typeof(IFormatProvider)]).ShouldNotBeNull();
    }

    // ProjectRolesList.cs использует new ProjectRolesListIdentification(projectId, -1) как sentinel «БД присвоит».
    // Отрицательная последняя компонента должна round-trip'иться и через ToString/Parse, и через JSON.
    [Fact]
    public void NegativeLastComponent_TwoLeafType_ShouldRoundTrip()
    {
        var instance = new ProjectRolesListIdentification(1, -1);

        instance.ToString().ShouldBe("ProjectRolesListId(1--1)");

        var parsed = ProjectRolesListIdentification.Parse(instance.ToString(), null);
        parsed.ShouldBe(instance);

        var json = JsonSerializer.Serialize(instance);
        JsonSerializer.Deserialize<ProjectRolesListIdentification>(json).ShouldBe(instance);
    }

    [Fact]
    public void NegativeLastComponent_FourLeafType_ShouldRoundTrip()
    {
        var plotElementId = new PlotElementIdentification(new PlotFolderIdentification(1, 2), 3);
        var instance = new PlotVersionIdentification(plotElementId, -1);

        var parsed = PlotVersionIdentification.Parse(instance.ToString(), null);
        parsed.ShouldBe(instance);

        var json = JsonSerializer.Serialize(instance);
        JsonSerializer.Deserialize<PlotVersionIdentification>(json).ShouldBe(instance);
    }

    // IdentificationParseHelper.TryParse1 отбрасывает значения <= 0 (управляет model binding: ?projectId=0
    // должен биндиться как отсутствующее значение, а не как 0). Одноногие Id поэтому сериализуются, но не
    // десериализуются при значении 0 или отрицательном. Тест фиксирует текущее поведение, не является багом.
    // Это поведение появляется только со строковым JSON-конвертером — до переопубликования
    // JoinRpg.Common.PrimitiveTypes.SourceGenerator (ADR011, PR4) старый вложенный формат не проходит через TryParse1.
    [Theory(Skip = "Требует переопубликованный JoinRpg.Common.PrimitiveTypes.SourceGenerator (ADR011, PR4)")]
    [InlineData(0)]
    [InlineData(-1)]
    public void SingleLeafType_ZeroOrNegativeValue_DoesNotRoundTripThroughJson(int value)
    {
        var instance = new UserIdentification(value);

        var json = JsonSerializer.Serialize(instance);

        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserIdentification>(json));
    }
}
