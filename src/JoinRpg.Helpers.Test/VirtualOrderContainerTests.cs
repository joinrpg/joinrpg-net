using Shouldly;
using Xunit;

namespace JoinRpg.Helpers.Test;

public class VirtualOrderContainerTests
{
    private class Foo : IOrderableEntity
    {
        public Foo(int id) => Id = id;

        public int Id { get; }

        public override string ToString() => $"Foo{Id}";
    }

    private readonly Foo _f1 = new(1);
    private readonly Foo _f2 = new(2);
    private readonly Foo _f3 = new(3);

    [Fact]
    public void TestUnorderedOrderedById()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f2, _f1 }, preserveOrder: false);
        voc.OrderedItems[0].ShouldBe(_f1);
        voc.OrderedItems[1].ShouldBe(_f2);
    }

    [Fact]
    public void TestOrderKept()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f2, _f1 }, preserveOrder: false);
        var voc2 = new VirtualOrderContainer<Foo>(voc.GetStoredOrder(), new[] { _f1, _f2 }, preserveOrder: false);
        voc2.OrderedItems.ShouldBe((IEnumerable<Foo>)voc.OrderedItems);
    }

    [Fact]
    public void TestMoveDownBeyondEdges()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 }, preserveOrder: false);
        _ = Should.Throw<InvalidOperationException>(() => voc.Move(_f2, 1));
    }

    [Fact]
    public void TestMoveUp()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 }, preserveOrder: false);
        voc.MoveUp(_f2);
        voc.OrderedItems.ShouldBe((IEnumerable<Foo>)new[] { _f2, _f1 });
    }

    [Fact]
    public void TestMoveDown()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 }, preserveOrder: false);
        voc.MoveDown(_f1);
        voc.OrderedItems.ShouldBe((IEnumerable<Foo>)new[] { _f2, _f1 });
    }

    [Fact]
    public void TestMoveUpBeyondEdges()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 }, preserveOrder: false);
        _ = Should.Throw<InvalidOperationException>(() => voc.Move(_f1, -1));
    }

    [Fact]
    public void TestMoveAfter()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 }, preserveOrder: false);
        _ = voc.MoveAfter(_f3, _f1);
        voc.OrderedItems.ShouldBe((IEnumerable<Foo>)new[] { _f1, _f3, _f2 });
    }

    [Fact]
    public void TestMoveToStart()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 }, preserveOrder: false);
        _ = voc.MoveAfter(_f3, null);
        voc.OrderedItems.ShouldBe((IEnumerable<Foo>)new[] { _f3, _f1, _f2 });
    }

    [Fact]
    public void TestMoveAfter2()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 }, preserveOrder: false);
        _ = voc.MoveAfter(_f1, _f2);
        voc.OrderedItems.ShouldBe((IEnumerable<Foo>)new[] { _f2, _f1, _f3 });
    }

    [Fact]
    public void TestMoveToEnd()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 }, preserveOrder: false);
        _ = voc.MoveAfter(_f1, _f3);
        voc.OrderedItems.ShouldBe((IEnumerable<Foo>)new[] { _f2, _f3, _f1 });
    }

    [Fact]
    public void TestMoveAfterNotExsts()
    {
        var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 }, preserveOrder: false);
        _ = Should.Throw<ArgumentException>(() => voc.MoveAfter(_f2, _f3));
    }
}
