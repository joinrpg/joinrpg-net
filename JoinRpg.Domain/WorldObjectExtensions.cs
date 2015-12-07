using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class WorldObjectExtensions
  {
    public static bool IsVisible(this IWorldObject cg, int? currentUserId)
    {
      return cg.IsPublic || cg.HasMasterAccess(currentUserId);
    }
  }
}
