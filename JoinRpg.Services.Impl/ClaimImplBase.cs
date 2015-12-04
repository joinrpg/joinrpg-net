using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
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

    protected async Task<Claim> LoadClaim(int projectId, int claimId, int currentUserId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      if (claim == null || claim.ProjectId != projectId) throw new DbEntityValidationException();
      if (!claim.HasAccess(currentUserId))
      {
        throw new NoAccessToProjectException(claim, currentUserId);
      }
      return claim;
    }

    protected async Task<TEmail> CreateClaimEmail<TEmail>(Claim claim, int currentUserId, string commentText,
      Func<UserSubscription, bool> subscribePredicate, bool isVisibleToPlayer, IEnumerable<User> extraRecepients = null)
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
        Text = new MarkdownString(commentText)
      };
    }
  }
}
