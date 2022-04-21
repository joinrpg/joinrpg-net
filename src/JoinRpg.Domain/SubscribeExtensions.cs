using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain;

public static class SubscribeExtensions
{
    public static IEnumerable<User> GetSubscriptions(
      this ForumThread forumThread,
      IEnumerable<User?>? extraRecipients,
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
      IEnumerable<User>? extraRecipients = null,
      bool mastersOnly = false
      )
    {
        if (character == null)
        {
            return Enumerable.Empty<User>();
        }

        return character.GetGroupsPartOf() //Get all groups for the character
          .SelectMany(g => g.Subscriptions) //get subscriptions on groups
          .Union(character.Subscriptions) //Subscriptions of the character itself.
          .Union(character.ApprovedClaim?.Subscriptions ?? Enumerable.Empty<UserSubscription>()) //Subscriptions of the claim itself.
          .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
          .Select(u => u.User) //Select users
          .Append(character.ApprovedClaim?.Player) //...and player who claimed for the character
          .Append(character.ApprovedClaim?.ResponsibleMasterUser) //claim esponsible master is always subscribed on everything related to the claim
          .Append(character.ResponsibleMasterUser) //...and the master who's responsible for the character
          .Union(extraRecipients ?? Enumerable.Empty<User>()) //add extra recipients
          .VerifySubscriptions(mastersOnly, character)
          .Distinct(); //we make union of subscriptions and directly taken users. Duplicates may appear.
    }

    //TODO: KK think how to merge them together to reduce copypaste.
    public static IEnumerable<User> GetSubscriptions(
      this Claim claim,
      Func<UserSubscription, bool> predicate,
      IEnumerable<User?>? extraRecipients = null,
      bool mastersOnly = false)
    {
        return claim.GetTarget().GetGroupsPartOf() //Get all groups for claim
            .SelectMany(g => g.Subscriptions) //get subscriptions on groups
            .Union(claim.Subscriptions) //subscribtions on claim
            .Union(claim.Character?.Subscriptions ?? Enumerable.Empty<UserSubscription>()) //and on characters
            .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
            .Select(u => u.User) //Select users
            .Append(claim.ResponsibleMasterUser) //Responsible master is always subscribed on everything
            .Append(claim.Player) //...and player himself also
            .Append(claim.Character?.ResponsibleMasterUser)
            .Union(extraRecipients ?? Enumerable.Empty<User>()) //add extra recipients
            .VerifySubscriptions(mastersOnly, claim)
            .Distinct(); //we make union of subscriptions and directly taken users. Duplicates may appear.
    }

    private static IEnumerable<User> VerifySubscriptions<TEntity>(
      this IEnumerable<User?> users,
      bool mastersOnly,
      TEntity entity)
      where TEntity : IProjectEntity
    {
        //TODO: currently there're no need to check for user access to entity but in general it's good to have
        return users
            .WhereNotNull()
            .Where(u => !mastersOnly || entity.HasMasterAccess(u.UserId)); //remove player if we doing something not player visible
    }

    public static IEnumerable<User> GetSubscriptions(this ProjectAccommodation room)
    {
        return room.Inhabitants.SelectMany(i => i.Subjects).SelectMany(claim =>
           claim.GetSubscriptions(subs => subs.AccommodationChange, Enumerable.Empty<User>()).Distinct().ToList());
    }

    public static ICollection<User> GetInviteSubscriptions(this Claim[] possibleRecipients)
    {
        return possibleRecipients.Select(claim =>
            claim.GetSubscriptions(subs => subs.AccommodationChange, Enumerable.Empty<User>()))
            .SelectMany(user => user)
            .Distinct()
            .ToList();
    }
}
