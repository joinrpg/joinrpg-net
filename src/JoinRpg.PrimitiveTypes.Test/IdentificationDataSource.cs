using System.Collections;

namespace JoinRpg.PrimitiveTypes.Test;

internal class IdentificationDataSource : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var iprojectEntityId = typeof(IProjectEntityId);
        var candidates = iprojectEntityId.Assembly.ExportedTypes
            .Where(t => t.Name.EndsWith("Identification") || t.IsAssignableTo(iprojectEntityId) || t == typeof(ProjectIdentification))
            .Where(t => !t.IsInterface);
        List<object[]> list = [.. candidates.Select(c => new[] { c })];
        return list.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


internal class ProjectIdDataSource : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var iprojectEntityId = typeof(IProjectEntityId);
        var candidates = iprojectEntityId.Assembly.ExportedTypes
            .Where(t => t.IsAssignableTo(iprojectEntityId))
            .Where(t => !t.IsInterface);
        List<object[]> list = [.. candidates.Select(c => new[] { c })];
        return list.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class WhiteListDataSource : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var iprojectEntityId = typeof(IProjectEntityId);
        var candidates = iprojectEntityId.Assembly.ExportedTypes
            .Where(t => IdentificationCommonTest.SkipISpanParsable.Contains(t.Name))
            .Where(t => !t.IsInterface);
        List<object[]> list = [.. candidates.Select(c => new[] { c })];
        return list.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
