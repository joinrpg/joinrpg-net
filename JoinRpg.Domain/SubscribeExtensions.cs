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
    public static IEnumerable<User> GetSubscriptions(
      this ForumThread forumThread,
      [CanBeNull] IEnumerable<User> extraRecepients,
      bool isVisibleToPlayer)
    {
      return
        forumThread.Subscriptions //get subscriptions on forum
          .Select(u => u.User) //Select users
          .Union(extraRecepients ?? Enumerable.Empty<User>()) //add extra recepients
          .VerifySubscriptions(isVisibleToPlayer, forumThread);
    }

    public static IEnumerable<User> GetSubscriptions(
      this Character character,
      Func<UserSubscription, bool> predicate,
      [CanBeNull] IEnumerable<User> extraRecepients,
      bool isVisibleToPlayer)
    {
      if (character == null) return new User[0];

      //TODO: KK recheck
      return character.GetGroupsPartOf() //Get all groups for the character
        .SelectMany(g => g.Subscriptions) //get subscriptions on groups
        .Union(character.Subscriptions) //Subscriptions of the character itself.
        .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
        .Select(u => u.User) //Select users
        .Union(character.ApprovedClaim?.Player) //...and player who claimed for the character
        .Union(extraRecepients ?? Enumerable.Empty<User>()) //add extra recepients
        .VerifySubscriptions(isVisibleToPlayer, character);
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
          .VerifySubscriptions(isVisibleToPlayer, claim)
      ;
    }

    private static IEnumerable<User> VerifySubscriptions<TEntity>(
      this IEnumerable<User> users,
      bool isVisibleToPlayer,
      TEntity entity)
      where TEntity : IProjectEntity
    {
      return users
          .Where(u => u != null)
          .Where(u => isVisibleToPlayer || entity.HasMasterAccess(u.UserId)); //remove player if we doing something not player visible
    }
  }
}
