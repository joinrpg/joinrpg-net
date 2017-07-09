using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CollectionAssert = JoinRpg.TestHelpers.CollectionAssert;

namespace JoinRpg.Helpers.Test
{
  [TestClass]
  public class VirtualOrderContainerTests
  {
    private class Foo : IOrderableEntity
    {
      public Foo(int id)
      {
        Id = id;
      }

      public int Id { get; }

      public override string ToString() => $"Foo{Id}";
    }

    private readonly Foo _f1 = new Foo(1);
    private readonly Foo _f2 = new Foo(2);
    private readonly Foo _f3 = new Foo(3);

    [TestMethod]
    public void TestUnorderedOrderedById()
    {
      var voc = new VirtualOrderContainer<Foo>("", new [] {_f2, _f1});
      Assert.AreEqual(_f1, voc.OrderedItems[0]);
      Assert.AreEqual(_f2, voc.OrderedItems[1]);
    }

    [TestMethod]
    public void TestOrderKept()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f2, _f1 });
      var voc2 = new VirtualOrderContainer<Foo>(voc.GetStoredOrder(), new[] {_f1, _f2});
      CollectionAssert.AreEqual(voc.OrderedItems, voc2.OrderedItems);
    }

    [TestMethod, ExpectedException(typeof(InvalidOperationException))]
    public void TestMoveDownBeyondEdges()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 });
      voc.Move(_f2, 1);
    }

    [TestMethod]
    public void TestMoveUp()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 });
      voc.MoveUp(_f2);
      CollectionAssert.AreEqual(new [] {_f2, _f1}, voc.OrderedItems);
    }

    [TestMethod]
    public void TestMoveDown()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 });
      voc.MoveDown(_f1);
      CollectionAssert.AreEqual(new[] { _f2, _f1 }, voc.OrderedItems);
    }

    [TestMethod, ExpectedException(typeof(InvalidOperationException))]
    public void TestMoveUpBeyondEdges()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2 });
      voc.Move(_f1, -1);
    }

    [TestMethod]
    public void TestMoveAfter()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 });
      voc.MoveAfter(_f3, _f1);
      CollectionAssert.AreEqual(new[] { _f1, _f3, _f2 }, voc.OrderedItems);
    }

    [TestMethod]
    public void TestMoveToStart()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 });
      voc.MoveAfter(_f3, null);
      CollectionAssert.AreEqual(new[] { _f3, _f1, _f2 }, voc.OrderedItems);
    }

    [TestMethod]
    public void TestMoveAfter2()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 });
      voc.MoveAfter(_f1, _f2);
      CollectionAssert.AreEqual(new[] { _f2, _f1, _f3 }, voc.OrderedItems);
    }

    [TestMethod]
    public void TestMoveToEnd()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2, _f3 });
      voc.MoveAfter(_f1, _f3);
      CollectionAssert.AreEqual(new[] { _f2, _f3, _f1 }, voc.OrderedItems);
    }

    [TestMethod, ExpectedException(typeof(ArgumentException))]
    public void TestMoveAfterNotExsts()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { _f1, _f2});
      voc.MoveAfter(_f2, _f3);
    }
  }
}
