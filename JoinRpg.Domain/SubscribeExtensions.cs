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
      [CanBeNull] IEnumerable<User> extraRecipients,
      bool isVisibleToPlayer)
    {
      return
        forumThread.Subscriptions //get subscriptions on forum
          .Select(u => u.User) //Select users
          .Union(extraRecipients ?? Enumerable.Empty<User>()) //add extra recipients
          .VerifySubscriptions(isVisibleToPlayer, forumThread);
    }

    public static IEnumerable<User> GetSubscriptions(
      this Character character,
      Func<UserSubscription, bool> predicate,
      [CanBeNull] IEnumerable<User> extraRecipients = null,
      bool mastersOnly = false
      )
    {
      if (character == null) return Enumerable.Empty<User>();
      return character.GetGroupsPartOf() //Get all groups for the character
        .SelectMany(g => g.Subscriptions) //get subscriptions on groups
        .Union(character.Subscriptions) //Subscriptions of the character itself.
        .Union(character.ApprovedClaim?.Subscriptions ?? Enumerable.Empty<UserSubscription>()) //Subscriptions of the claim itself.
        .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
        .Select(u => u.User) //Select users
        .Union(character.ApprovedClaim?.Player) //...and player who claimed for the character
        .Union(character.ApprovedClaim?.ResponsibleMasterUser) //claim esponsible master is always subscribed on everything related to the claim
        .Union((character.ResponsibleMasterUser)) //...and the master who's responsible for the character
        .Union(extraRecipients ?? Enumerable.Empty<User>()) //add extra recipients
        .VerifySubscriptions(mastersOnly, character)
        .Distinct(); //we make union of subscriptions and directly taken users. Duplicates may appear.
    }

    //TODO: KK think how to merge them together to reduce copypaste.
    public static IEnumerable<User> GetSubscriptions(
      this Claim claim, 
      Func<UserSubscription, bool> predicate,
      [CanBeNull] IEnumerable<User> extraRecipients = null, 
      bool mastersOnly = false)
    {
      return claim.GetTarget().GetGroupsPartOf() //Get all groups for claim
          .SelectMany(g => g.Subscriptions) //get subscriptions on groups
          .Union(claim.Subscriptions) //subscribtions on claim
          .Union(claim.Character?.Subscriptions ?? Enumerable.Empty<UserSubscription>()) //and on characters
          .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
          .Select(u => u.User) //Select users
          .Union(claim.ResponsibleMasterUser) //Responsible master is always subscribed on everything
          .Union(claim.Player) //...and player himself also
          .Union(claim.Character?.ResponsibleMasterUser)
          .Union(extraRecipients ?? Enumerable.Empty<User>()) //add extra recipients
          .Where(u => u != null)
          .VerifySubscriptions(mastersOnly, claim)
          .Distinct(); //we make union of subscriptions and directly taken users. Duplicates may appear.
    }

    private static IEnumerable<User> VerifySubscriptions<TEntity>(
      this IEnumerable<User> users,
      bool mastersOnly,
      TEntity entity)
      where TEntity : IProjectEntity
    {
      //TODO: currently there're no need to check for user access to entity but in general it's good to have
      return users
          .Where(u => u != null)
          .Where(u => !mastersOnly || entity.HasMasterAccess(u.UserId)); //remove player if we doing something not player visible
    }
  }
}
