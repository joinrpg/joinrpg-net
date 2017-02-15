using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static  class SubscribeExtensions
  {
    public static IEnumerable<User> GetSubscriptions(this ForumThread claim,
  int initiatorUserId, [CanBeNull] IEnumerable<User> extraRecepients, bool isVisibleToPlayer)
    {
      return
        claim.Subscriptions //get subscriptions on forum
          .Select(u => u.User) //Select users
          .Union(extraRecepients ?? Enumerable.Empty<User>()) //add extra recepients
          .Where(u => isVisibleToPlayer || !claim.HasMasterAccess(u.UserId)); //remove player if we doing something not player visible

    }

    public static IEnumerable<User> GetSubscriptions(
      this Claim claim, 
      Func<UserSubscription, bool> predicate,
      [CanBeNull] IEnumerable<User> extraRecepients, 
      bool isVisibleToPlayer)
    {
      return claim.GetTarget().GetGroupsPartOf() //Get all groups for claim
          .SelectMany(g => g.Subscriptions) //get subscriptions on groups
          .Union(claim.Subscriptions) //subscribtions on claim
          .Union(claim.Character?.Subscriptions ?? new UserSubscription[] {}) //and on characters
          .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
          .Select(u => u.User) //Select users
          .Union(claim.ResponsibleMasterUser) //Responsible master is always subscribed on everything
          .Union(claim.Player) //...and player himself also
          .Union(extraRecepients ?? Enumerable.Empty<User>()) //add extra recepients
          .Where(u => isVisibleToPlayer || u != claim.Player) //remove player if we doing something not player visible
        ;
    }
  }
}
