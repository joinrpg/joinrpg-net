using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  public abstract class ClaimImplBase : DbServiceImplBase
  {
    protected IEmailService EmailService { get; }

    protected ClaimImplBase(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork)
    {
      EmailService = emailService;
    }

    protected async Task<TEmail> CreateClaimEmail<TEmail>(Claim claim, int currentUserId, string commentText, Func<UserSubscription, bool> subscribePredicate, bool isVisibleToPlayer, CommentExtraAction? commentExtraAction, IEnumerable<User> extraRecepients = null)
      where TEmail : ClaimEmailModel, new()
    {
      var subscriptions =
        claim.GetSubscriptions(subscribePredicate, currentUserId, extraRecepients ?? Enumerable.Empty<User>(),
          isVisibleToPlayer).ToList();
      return new TEmail()
      {
        Claim = claim,
        ProjectName = claim.Project.ProjectName,
        Initiator = await UserRepository.GetById(currentUserId),
        InitiatorType = currentUserId == claim.PlayerUserId ? ParcipantType.Player : ParcipantType.Master,
        Recepients = subscriptions.ToList(),
        Text = new MarkdownString(commentText),
        CommentExtraAction = commentExtraAction
      };
    }
  }
}
