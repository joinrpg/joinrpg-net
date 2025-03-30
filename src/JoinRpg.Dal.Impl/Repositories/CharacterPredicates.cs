using System.Linq.Expressions;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class CharacterPredicates
{
    internal static Expression<Func<Character, bool>> Hot() => character => character.IsHot && character.IsActive && character.ApprovedClaim == null;
}
