using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Characters;

internal static class ShouldyDataModelExtensions
{
    public static void FieldValuesShouldBe(this IFieldContainter mockCharacter,
        params FieldWithValue[] field2) =>
        mockCharacter.JsonData.ShouldBe(field2.SerializeFields());
}
