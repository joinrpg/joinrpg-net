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

      public override string ToString()
      {
        return $"{"Foo"}{Id}";
      }
    }

    private readonly Foo f1 = new Foo(1);
    private readonly Foo f2 = new Foo(2);

    [TestMethod]
    public void TestUnorderedOrderedById()
    {
      var voc = new VirtualOrderContainer<Foo>("", new [] {f2, f1});
      Assert.AreEqual(f1, voc.OrderedItems[0]);
      Assert.AreEqual(f2, voc.OrderedItems[1]);
    }

    [TestMethod]
    public void TestOrderKept()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { f2, f1 });
      var voc2 = new VirtualOrderContainer<Foo>(voc.GetStoredOrder(), new[] {f1, f2});
      CollectionAssert.AreEqual(voc.OrderedItems, voc2.OrderedItems);
    }

    [TestMethod, ExpectedException(typeof(InvalidOperationException))]
    public void TestMoveDownBeyondEdges()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { f2, f1 });
      voc.MoveDown(f2);
    }

    [TestMethod]
    public void TestMoveUp()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { f1, f2 });
      voc.MoveUp(f2);
      CollectionAssert.AreEqual(new [] {f2, f1}, voc.OrderedItems);
    }

    [TestMethod]
    public void TestMoveDown()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { f1, f2 });
      voc.MoveDown(f1);
      CollectionAssert.AreEqual(new[] { f2, f1 }, voc.OrderedItems);
    }

    [TestMethod, ExpectedException(typeof(InvalidOperationException))]
    public void TestMoveUpBeyondEdges()
    {
      var voc = new VirtualOrderContainer<Foo>("", new[] { f2, f1 });
      voc.MoveUp(f1);
    }
  }
}
