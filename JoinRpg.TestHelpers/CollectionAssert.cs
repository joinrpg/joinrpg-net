using System.Collections.Generic;
using System.Linq;
using MSCollectionAssert = Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;

namespace JoinRpg.TestHelpers
{
  //TODO redirect everything from MSCollectionAssert
  public static class CollectionAssert
  {
    public static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
      MSCollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
    }
  }
}
