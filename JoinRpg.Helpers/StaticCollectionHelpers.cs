using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace JoinRpg.Helpers
{
  public static class StaticCollectionHelpers
  {
    public static Dictionary<string, string> ToDictionary(this NameValueCollection collection)
    {
      return collection.AllKeys.ToDictionary(key => key, key => collection[key]);
    }
  }
}
