using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Impl.ClaimProblemFilters;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class ClaimServiceImpl : DbServiceImplBase, IClaimService
  {
    private readonly IEmailService _emailService;

    public async Task AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText)
    {
      IClaimSource source;
      if (characterGroupId != null)
      {
        source = await ProjectRepository.LoadGroupAsync(projectId, characterGroupId.Value);
      }
      else if (characterId != null)
      {
        source = await ProjectRepository.GetCharacterAsync(projectId, characterId.Value);
      }
      else
      {
        throw new DbEntityValidationException();
      }

      EnsureCanAddClaim(currentUserId, source);

      var addClaimDate = DateTime.UtcNow;
      var responsibleMaster = source.GetResponsibleMasters().FirstOrDefault();
      var claim = new Claim()
      {
        CharacterGroupId = characterGroupId,
        CharacterId = characterId,
        ProjectId = projectId,
        PlayerUserId = currentUserId,
        PlayerAcceptedDate = addClaimDate,
        Comments = new List<Comment>()
        {
          new Comment()
          {
            AuthorUserId = currentUserId,
            CommentText = new MarkdownString(claimText),
            CreatedTime = addClaimDate,
            IsCommentByPlayer = true,
            IsVisibleToPlayer = true,
            ProjectId = projectId,
            LastEditTime = addClaimDate,
          }
        },
        ResponsibleMasterUserId = responsibleMaster?.UserId,
        ResponsibleMasterUser = responsibleMaster,
        Subscriptions = new List<UserSubscription>() //We need this as we are using it later
      };
      UnitOfWork.GetDbSet<Claim>().Add(claim);
      await UnitOfWork.SaveChangesAsync();

      var email = await CreateClaimEmail<NewClaimEmail>(claim, currentUserId, claimText, s => s.ClaimStatusChange);
      await _emailService.Email(email);
    }

    private static void EnsureCanAddClaim<T>(int currentUserId, T claimSource) where T: IClaimSource
    {
      if (claimSource.HasClaimForUser(currentUserId))
      {
        throw new DbEntityValidationException();
      }
    }

    public async Task AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer, bool isMyClaim, string commentText)
    {
      var claim = LoadClaim(projectId, claimId, currentUserId);

      claim.AddCommentImpl(currentUserId, parentCommentId, commentText, DateTime.UtcNow, isVisibleToPlayer, isMyClaim);
      await UnitOfWork.SaveChangesAsync();

      var addCommentEmail = await CreateClaimEmail<AddCommentEmail>(claim, currentUserId, commentText, s => s.Comments);
      addCommentEmail.Recepients.Add(claim.Comments.SingleOrDefault(c => c.CommentId == parentCommentId)?.Author);
      await _emailService.Email(addCommentEmail);
    }

    private async Task<TEmail> CreateClaimEmail<TEmail>(Claim claim, int currentUserId, string commentText, Func<UserSubscription, bool> predicate) where TEmail:ClaimEmailBase, new()
    {
      return new TEmail()
      {
        Claim = claim,
        ProjectName = claim.Project.ProjectName,
        Initiator = await UserRepository.GetById(currentUserId),
        InitiatorType = currentUserId == claim.PlayerUserId ? ParcipantType.Player : ParcipantType.Master,
        Recepients = claim.GetSubscriptions(predicate, currentUserId).ToList(),
        Text = new MarkdownString(commentText)
      };
    }

    public async Task AppoveByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      if (claim.MasterAcceptedDate != null)
      {
        throw new DbEntityValidationException();
      }
      if (claim.PlayerAcceptedDate == null)
      {
        throw new DbEntityValidationException();
      }

      var now = DateTime.UtcNow;
      claim.MasterAcceptedDate = now;
      claim.AddCommentImpl(currentUserId, null, commentText, now, true, false);

      foreach (var otherClaim in claim.OtherClaimsForThisPlayer())
      {
        if (otherClaim.IsApproved)
        {
          throw new DbEntityValidationException();
        }
        otherClaim.MasterDeclinedDate = now;
        await
          _emailService.Email(
            await
              AddCommentWithEmail<DeclineByMasterEmail>(currentUserId,
                "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект",
                otherClaim, now, true, s => s.ClaimStatusChange));
      }

      if (claim.Group != null)
      {
        //TODO: Добавить здесь возможность ввести имя персонажа или брать его из заявки
        claim.ConvertToIndividual();
      }

      await _emailService.Email(await CreateClaimEmail<ApproveByMasterEmail>(claim, currentUserId, commentText, s => s.ClaimStatusChange));
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeclineByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      if (claim.MasterDeclinedDate != null)
      {
        throw new DbEntityValidationException();
      }

      DateTime now = DateTime.UtcNow;

      claim.MasterDeclinedDate = now;
      var email = await AddCommentWithEmail<DeclineByMasterEmail>(currentUserId, commentText, claim, now, true, s => s.ClaimStatusChange);

      await UnitOfWork.SaveChangesAsync();
      await _emailService.Email(email);
    }

    public async Task DeclineByPlayer(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = LoadMyClaim(projectId, claimId, currentUserId);
      if (claim.PlayerDeclinedDate != null)
      {
        throw new DbEntityValidationException();
      }

      DateTime now = DateTime.UtcNow;
      claim.PlayerDeclinedDate = now;

      var email = await AddCommentWithEmail<DeclineByPlayerEmail>(currentUserId, commentText, claim, now, true, s => s.ClaimStatusChange);
     
      await UnitOfWork.SaveChangesAsync();
      await _emailService.Email(email);
    }

    private async Task<T> AddCommentWithEmail<T>(int currentUserId, string commentText, Claim claim, DateTime now, bool isVisibleToPlayer, Func<UserSubscription, bool> predicate) where T : ClaimEmailBase, new()
    {
      claim.AddCommentImpl(currentUserId, null, commentText, now, isVisibleToPlayer, isMyClaim: true);
      return await CreateClaimEmail<T>(claim, currentUserId, commentText, predicate);
    }

    public async Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      if (!claim.Project.HasAccess(currentUserId))
      {
        throw new Exception();
      }
      if (!claim.Project.HasAccess(responsibleMasterId))
      {
        throw new DbEntityValidationException();
      }
      var newMaster = await UserRepository.GetById(responsibleMasterId);
      claim.ResponsibleMasterUserId = responsibleMasterId;

      claim.AddCommentImpl(currentUserId, null,
        $"Отвественный мастер: {claim.ResponsibleMasterUser?.DisplayName ?? "нет"} → {newMaster.DisplayName}", DateTime.UtcNow,
        isVisibleToPlayer: false, isMyClaim: false);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task<IList<ClaimProblem>> GetProblemClaims(int projectId)
    {
      var project = await ClaimsRepository.GetClaims(projectId);
      var filters = new IClaimProblemFilter[] {new ResponsibleMasterProblemFilter(), new NotAnsweredClaim(),};
      return
        project.Claims.Where(claim => claim.IsActive)
          .SelectMany(claim => filters.SelectMany(f => f.GetProblems(project, claim))).ToList();
    }

    private async Task<Claim> LoadClaimForApprovalDecline(int projectId, int claimId, int currentUserId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      if (!claim.Project.HasSpecificAccess(currentUserId, acl => acl.CanApproveClaims))
      {
        throw new DbEntityValidationException();
      }
      return claim;
    }

    private Claim LoadMyClaim(int projectId, int claimId, int currentUserId)
    {
      var claim = LoadClaim(projectId, claimId, currentUserId);
      if (claim.PlayerUserId != currentUserId)
      {
        throw new DbEntityValidationException();
      }
      return claim;
    }

    private Claim LoadClaim(int projectId, int claimId, int currentUserId)
    {
      var claim = LoadProjectSubEntity<Claim>(projectId, claimId);
      if (!claim.HasAccess(currentUserId))
      {
        throw new DbEntityValidationException();
      }
      return claim;
    }

    public ClaimServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork)
    {
      _emailService = emailService;
    }
  }

  internal static class ClaimStaticExtensions
  {
    public static void AddCommentImpl(this Claim claim, int currentUserId, int? parentCommentId, string commentText,
      DateTime now, bool isVisibleToPlayer, bool isMyClaim)
    {
      if (string.IsNullOrWhiteSpace(commentText))
      {
        throw new DbEntityValidationException();
      }
      if (!isVisibleToPlayer && isMyClaim)
      {
        throw new DbEntityValidationException();
      }

      var comment = new Comment()
      {
        ProjectId = claim.ProjectId,
        AuthorUserId = currentUserId,
        ClaimId = claim.ClaimId,
        CommentText = new MarkdownString(commentText),
        CreatedTime = now,
        IsCommentByPlayer = isMyClaim,
        IsVisibleToPlayer = isVisibleToPlayer,
        ParentCommentId = parentCommentId,
        LastEditTime = now
      };
      claim.Comments.Add(comment);
    }

    public static void ConvertToIndividual(this Claim claim)
    {
      if (claim.Group.AvaiableDirectSlots == 0)
      {
        throw new DbEntityValidationException();
      }
      if (claim.Group.AvaiableDirectSlots > 0)
      {
        claim.Group.AvaiableDirectSlots -= 1;
      }
      var character = new Character()
      {
        CharacterName = $"Новый персонаж в группе {claim.Group.CharacterGroupName}",
        ProjectId = claim.ProjectId,
        IsAcceptingClaims = true,
        IsPublic = claim.Group.IsPublic,
        Groups = new List<CharacterGroup>()
        {
          claim.Group
        }
      };
      claim.CharacterGroupId = null;
      claim.Character = character;
    }

    public static IEnumerable<User> GetSubscriptions(this Claim claim, Func<UserSubscription, bool> predicate, int initiatorUserId)
    {
      return claim.GetTarget() //Character or group
        .GetParentGroups() //Get parents
        .Union(claim.Group) //Don't forget group himself
        .WhereNotNull() //..that can be null
        .SelectMany(g => g.Subscriptions) //get subscriptions on groups
        .Union(claim.Subscriptions) //subscribtions on claim
        .Union(claim.Character?.Subscriptions ?? new UserSubscription[] {}) //and on characters
        .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
        .Select(u => u.User) //Select users
        .Union(claim.ResponsibleMasterUser) //Responsible master is always subscribed on everything
        .Union(claim.Player) //...and player himself also
        .Where(u => u != null && u.UserId != initiatorUserId) //Do not send mail to self (and also will remove nulls)
        .Distinct() //One user can be subscribed by multiple reasons
        ;
    }
  }
}
