using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Impl.ClaimProblemFilters;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class ClaimServiceImpl : ClaimImplBase, IClaimService
  {
    
    public async Task AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText)
    {
      var source = await GetClaimSource(projectId, characterGroupId, characterId);

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
        CreateDate =  addClaimDate,
        ClaimStatus = Claim.Status.AddedByUser,
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
        Subscriptions = new List<UserSubscription>(), //We need this as we are using it later
        LastUpdateDateTime = addClaimDate
      };
      UnitOfWork.GetDbSet<Claim>().Add(claim);
      await UnitOfWork.SaveChangesAsync();

      var email = await CreateClaimEmail<NewClaimEmail>(claim, currentUserId, claimText, s => s.ClaimStatusChange, true, CommentExtraAction.NewClaim);
      await EmailService.Email(email);
    }

    private async Task<IClaimSource> GetClaimSource(int projectId, int? characterGroupId, int? characterId)
    {
      var characterGroup = characterGroupId != null
        ? await ProjectRepository.LoadGroupAsync(projectId, characterGroupId.Value)
        : null;
      var character = characterId != null ? await ProjectRepository.GetCharacterAsync(projectId, characterId.Value) : null;

      var source = new IClaimSource[] {characterGroup, character}.WhereNotNull().Single();
      if (!source.IsAvailable)
      {
        throw new DbEntityValidationException();
      }
      return source;
    }

    private static void EnsureCanAddClaim<T>(int currentUserId, T claimSource) where T: IClaimSource
    {
      //TODO add more validation checks, move to Domain
      if (claimSource.HasClaimForUser(currentUserId))
      {
        throw new DbEntityValidationException();
      }
      if (!claimSource.IsAvailable)
      {
        throw new DbEntityValidationException();
      }
    }

    public async Task AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction)
    {
      var claim = await LoadClaim(projectId, claimId, currentUserId);
      var now = DateTime.UtcNow;

      var parentComment = claim.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

      CommentExtraAction? extraAction = null;

      if (financeAction != FinanceOperationAction.None)
      {
        var finance = parentComment?.Finance;
        if (finance == null)
        {
          throw new InvalidOperationException();
        }

        if (!finance.RequireModeration)
        {
          throw new ValueAlreadySetException("Finance entry is already moderated.");
        }

        finance.RequestModerationAccess(currentUserId);
        finance.Changed = now;
        switch (financeAction)
        {
          case FinanceOperationAction.Approve:
            finance.State = FinanceOperationState.Approved;
            extraAction = CommentExtraAction.ApproveFinance;
            claim.UpdateClaimFeeIfRequired(finance.OperationDate);
            break;
          case FinanceOperationAction.Decline:
            finance.State = FinanceOperationState.Declined;
            extraAction = CommentExtraAction.RejectFinance;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(financeAction), financeAction, null);
        }
      }

      var email = await AddCommentWithEmail<AddCommentEmail>(currentUserId, commentText, claim, now, isVisibleToPlayer,
        s => s.Comments, parentComment, extraAction);      

      await UnitOfWork.SaveChangesAsync();

      await EmailService.Email(email);
    }

    public async Task AppoveByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      var now = DateTime.UtcNow;

      claim.EnsureStatus(Claim.Status.AddedByUser);
      claim.MasterAcceptedDate = now;
      claim.ClaimStatus = Claim.Status.Approved;

      claim.ResponsibleMasterUserId = claim.ResponsibleMasterUserId ?? currentUserId;
      claim.AddCommentImpl(currentUserId, null, commentText, now, true, CommentExtraAction.ApproveByMaster);

      foreach (var otherClaim in claim.OtherActiveClaimsForThisPlayer())
      {
        otherClaim.EnsureStatus(Claim.Status.AddedByUser, Claim.Status.AddedByMaster);
        otherClaim.MasterDeclinedDate = now;
        otherClaim.ClaimStatus = Claim.Status.DeclinedByMaster;
        await
          EmailService.Email(
            await
              AddCommentWithEmail<DeclineByMasterEmail>(currentUserId,
                "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект",
                otherClaim, now, true, s => s.ClaimStatusChange, null, CommentExtraAction.DeclineByMaster));
      }

      if (claim.Group != null)
      {
        //TODO: Добавить здесь возможность ввести имя персонажа или брать его из заявки
        claim.ConvertToIndividual();
      }

      //TODO: Reorder and save emails only after save

      await
        EmailService.Email(
          await
            CreateClaimEmail<ApproveByMasterEmail>(claim, currentUserId, commentText, s => s.ClaimStatusChange, true,
              CommentExtraAction.ApproveByMaster));
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeclineByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      claim.EnsureStatus(Claim.Status.AddedByUser, Claim.Status.AddedByMaster, Claim.Status.Approved);

      DateTime now = DateTime.UtcNow;

      claim.MasterDeclinedDate = now;
      claim.ClaimStatus = Claim.Status.DeclinedByMaster;
      var email =
        await
          AddCommentWithEmail<DeclineByMasterEmail>(currentUserId, commentText, claim, now, true,
            s => s.ClaimStatusChange, null, CommentExtraAction.DeclineByMaster);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    public async Task RestoreByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      var now = DateTime.UtcNow;

      claim.EnsureStatus(Claim.Status.DeclinedByUser, Claim.Status.DeclinedByMaster);

      claim.ClaimStatus = Claim.Status.AddedByUser; //TODO: Actually should be "AddedByMaster" but we don't support it yet.

      claim.ResponsibleMasterUserId = claim.ResponsibleMasterUserId ?? currentUserId;

      if (claim.CharacterId != null && claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved))
      {
        claim.CharacterId = null;
        claim.CharacterGroupId = claim.Project.RootGroup.CharacterGroupId;
      }
      
      var email = await AddCommentWithEmail<RestoreByMasterEmail>(currentUserId, commentText, claim, now, true, s => s.ClaimStatusChange, null, CommentExtraAction.RestoreByMaster);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    public async Task MoveByMaster(int projectId, int claimId, int currentUserId, string contents, int? characterGroupId, int? characterId)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      await GetClaimSource(projectId, characterGroupId, characterId);

      claim.CharacterGroupId = characterGroupId;
      claim.CharacterId = characterId;

      if (claim.IsApproved && claim.CharacterId == null)
      {
        throw new DbEntityValidationException();
      }

      claim.ResponsibleMasterUserId = claim.ResponsibleMasterUserId ?? currentUserId;
      var email =
        await
          AddCommentWithEmail<MoveByMasterEmail>(currentUserId, contents, claim, DateTime.UtcNow,
            isVisibleToPlayer: true, predicate: s => s.ClaimStatusChange, parentComment: null,
            extraAction: CommentExtraAction.MoveByMaster);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    public void UpdateReadCommentWatermark(int projectId, int claimId, int currentUserId, int maxCommentId)
    {
      Task.Run(() =>
      {
        var watermark =
          UnitOfWork.GetDbSet<ReadCommentWatermark>()
            .SingleOrDefault(w => w.ClaimId == claimId && w.UserId == currentUserId);

        if (watermark == null)
        {
          watermark = new ReadCommentWatermark()
          {
            ClaimId = claimId,
            ProjectId = projectId,
            UserId = currentUserId
          };
          UnitOfWork.GetDbSet<ReadCommentWatermark>().Add(watermark);
        }

        if (watermark.CommentId > maxCommentId)
        {
          return;
        }
        watermark.CommentId = maxCommentId;
        UnitOfWork.SaveChangesAsync();
      });
    }


    public async Task DeclineByPlayer(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadMyClaim(projectId, claimId, currentUserId);
      claim.EnsureStatus(Claim.Status.AddedByUser, Claim.Status.AddedByMaster, Claim.Status.Approved);

      DateTime now = DateTime.UtcNow;
      claim.PlayerDeclinedDate = now;
      claim.ClaimStatus = Claim.Status.DeclinedByUser;

      var email =
        await
          AddCommentWithEmail<DeclineByPlayerEmail>(currentUserId, commentText, claim, now, true,
            s => s.ClaimStatusChange, null, CommentExtraAction.DeclineByPlayer);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    private async Task<T> AddCommentWithEmail<T>(int currentUserId, string commentText, Claim claim, DateTime now,
      bool isVisibleToPlayer, Func<UserSubscription, bool> predicate, Comment parentComment,
      CommentExtraAction? extraAction = null) where T : ClaimEmailModel, new()
    {
      var visibleToPlayerUpdated = isVisibleToPlayer && parentComment?.IsVisibleToPlayer != false; 
      claim.AddCommentImpl(currentUserId, parentComment, commentText, now, visibleToPlayerUpdated, extraAction);
      return
        await
          CreateClaimEmail<T>(claim, currentUserId, commentText, predicate, visibleToPlayerUpdated, extraAction, new[] {parentComment?.Author});
    }

    public async Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      claim.RequestMasterAccess(currentUserId);
      claim.RequestMasterAccess(responsibleMasterId);


      var newMaster = await UserRepository.GetById(responsibleMasterId);
      claim.ResponsibleMasterUserId = responsibleMasterId;

      //TODO: Maybe send email here
      claim.AddCommentImpl(currentUserId, null,
        $"{claim.ResponsibleMasterUser?.DisplayName ?? "N/A"} → {newMaster.DisplayName}",
        DateTime.UtcNow, isVisibleToPlayer: true, extraAction: CommentExtraAction.ChangeResponsible);
      await UnitOfWork.SaveChangesAsync();
    }

    public IEnumerable<ClaimProblem> GetProblems(IEnumerable<Claim> claims)
    {
      var filters = new IClaimProblemFilter[]
      {
        new ResponsibleMasterProblemFilter(), new NotAnsweredClaim(), new BrokenClaimsAndCharacters(),
        new FinanceProblemsFilter(),
      };


      return claims.SelectMany(claim => filters.SelectMany(f => f.GetProblems(claim))).ToList();
    }

    private async Task<Claim> LoadClaimForApprovalDecline(int projectId, int claimId, int currentUserId)
    {
      var claim = await ProjectRepository.GetClaim(projectId, claimId);
      claim.RequestMasterAccess(currentUserId, acl => acl.CanApproveClaims);
      return claim;
    }

    private async Task<Claim> LoadMyClaim(int projectId, int claimId, int currentUserId)
    {
      var claim = await LoadClaim(projectId, claimId, currentUserId);
      if (claim.PlayerUserId != currentUserId)
      {
        throw new DbEntityValidationException();
      }
      return claim;
    }

    public ClaimServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork, emailService)
    {
    }
  }

  internal static class ClaimStaticExtensions
  {
    public static Comment AddCommentImpl(this Claim claim, int currentUserId, Comment parentComment, string commentText, DateTime now, bool isVisibleToPlayer, CommentExtraAction? extraAction)
    {
      if (!isVisibleToPlayer && claim.PlayerUserId == currentUserId)
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
        IsCommentByPlayer = claim.PlayerUserId == currentUserId,
        IsVisibleToPlayer = isVisibleToPlayer,
        Parent = parentComment,
        LastEditTime = now,
        ExtraAction = extraAction
      };
      claim.Comments.Add(comment);

      claim.LastUpdateDateTime = now;

      return comment;
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
        IsActive = true,
        Groups = new List<CharacterGroup>()
        {
          claim.Group
        }
      };
      claim.CharacterGroupId = null;
      claim.Character = character;
    }
  }
}
