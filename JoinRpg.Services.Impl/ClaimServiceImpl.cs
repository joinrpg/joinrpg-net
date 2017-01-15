using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class ClaimServiceImpl : ClaimImplBase, IClaimService
  {
    
    public async Task AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText, IDictionary<int, string> fields)
    {
      var source = await ProjectRepository.GetClaimSource(projectId, characterGroupId, characterId);

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
        ResponsibleMasterUserId = responsibleMaster?.UserId,
        ResponsibleMasterUser = responsibleMaster,
        LastUpdateDateTime = addClaimDate,
        Discussion = new CommentDiscussion(),
      };

      if (!string.IsNullOrWhiteSpace(claimText))
      {
        claim.Discussion.Comments.Add(new Comment
        {
          AuthorUserId = currentUserId,
          CommentText = new CommentText {Text =  new MarkdownString(claimText)},
          CreatedAt = addClaimDate,
          IsCommentByPlayer = true,
          IsVisibleToPlayer = true,
          ProjectId = projectId,
          LastEditTime = addClaimDate,
        });
      }
      UnitOfWork.GetDbSet<Claim>().Add(claim);

      FieldSaveHelper.SaveCharacterFields(currentUserId, claim, fields);

      await UnitOfWork.SaveChangesAsync();

      await
        EmailService.Email(
          EmailHelpers.CreateClaimEmail<NewClaimEmail>(claim, currentUserId, claimText ?? "", s => s.ClaimStatusChange, true,
              CommentExtraAction.NewClaim, await UserRepository.GetById(currentUserId)));
    }

    private static void EnsureCanAddClaim<T>(int currentUserId, T claimSource) where T: IClaimSource
    {
      //TODO add more validation checks, move to Domain
      if (claimSource.HasClaimForUser(currentUserId))
      {
        throw new ClaimAlreadyPresentException();
      }
      claimSource.EnsureAvailable();

      claimSource.EnsureProjectActive();
    }

    public async Task AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction)
    {
      var claim = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(currentUserId);
      var now = DateTime.UtcNow;

      claim.SetDiscussed(currentUserId, isVisibleToPlayer);

      var parentComment = claim.Discussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

      Func<UserSubscription, bool> predicate = s => s.Comments;
      CommentExtraAction? extraAction = null;

      if (financeAction != FinanceOperationAction.None)
      {
        extraAction = PerformFinanceOperation(currentUserId, financeAction, parentComment, now, claim);
        predicate = s => s.Comments || s.MoneyOperation;
      }

      
      var email = await AddCommentWithEmail<AddCommentEmail>(currentUserId, commentText, claim, isVisibleToPlayer,
        predicate, parentComment, extraAction);      

      await UnitOfWork.SaveChangesAsync();

      await EmailService.Email(email);
    }

    private static CommentExtraAction? PerformFinanceOperation(int currentUserId, FinanceOperationAction financeAction,
      Comment parentComment, DateTime now, Claim claim)
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
          claim.UpdateClaimFeeIfRequired(finance.OperationDate);
          return CommentExtraAction.ApproveFinance;
        case FinanceOperationAction.Decline:
          finance.State = FinanceOperationState.Declined;
          return CommentExtraAction.RejectFinance;
        default:
          throw new ArgumentOutOfRangeException(nameof(financeAction), financeAction, null);
      }
    }

    public async Task AppoveByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      var now = DateTime.UtcNow;

      claim.MasterAcceptedDate = now;
      claim.ChangeStatusWithCheck(Claim.Status.Approved);

      claim.ResponsibleMasterUserId = claim.ResponsibleMasterUserId ?? currentUserId;
      claim.AddCommentImpl(currentUserId, null, commentText, true, CommentExtraAction.ApproveByMaster);

      foreach (var otherClaim in claim.OtherPendingClaimsForThisPlayer())
      {
        otherClaim.EnsureStatus(Claim.Status.AddedByUser, Claim.Status.AddedByMaster, Claim.Status.Discussed, Claim.Status.OnHold);
        otherClaim.MasterDeclinedDate = now;
        otherClaim.ClaimStatus = Claim.Status.DeclinedByMaster;
        await
          EmailService.Email(
            await
              AddCommentWithEmail<DeclineByMasterEmail>(currentUserId,
                "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект",
                otherClaim, true, s => s.ClaimStatusChange, null, CommentExtraAction.DeclineByMaster));
      }

      if (claim.Group != null)
      {
        //TODO: Добавить здесь возможность ввести имя персонажа или брать его из заявки
        claim.ConvertToIndividual();
      }

      //We need to resave fields here, because it may cause some field values to move from Claim to Characters
      //which also could trigger changing of special groups
      FieldSaveHelper.SaveCharacterFields(currentUserId, claim, new Dictionary<int, string>());

      //TODO: Reorder and save emails only after save

      await
        EmailService.Email(
          EmailHelpers.CreateClaimEmail<ApproveByMasterEmail>(claim, currentUserId, commentText, s => s.ClaimStatusChange, true,
              CommentExtraAction.ApproveByMaster, await UserRepository.GetById(currentUserId)));
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task DeclineByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      claim.EnsureStatus(Claim.Status.AddedByUser, Claim.Status.AddedByMaster, Claim.Status.Discussed, Claim.Status.OnHold, Claim.Status.Approved);

      DateTime now = DateTime.UtcNow;

      claim.MasterDeclinedDate = now;
      claim.ClaimStatus = Claim.Status.DeclinedByMaster;
      var email =
        await
          AddCommentWithEmail<DeclineByMasterEmail>(currentUserId, commentText, claim, true,
            s => s.ClaimStatusChange, null, CommentExtraAction.DeclineByMaster);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    public async Task RestoreByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      var now = DateTime.UtcNow;

      claim.EnsureStatus(Claim.Status.DeclinedByUser, Claim.Status.DeclinedByMaster, Claim.Status.OnHold);
      claim.ClaimStatus = Claim.Status.AddedByUser; //TODO: Actually should be "AddedByMaster" but we don't support it yet.
      claim.ResponsibleMasterUserId = claim.ResponsibleMasterUserId ?? currentUserId;


      if (claim.CharacterId != null)
      {
        if (claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved))
        {
          claim.CharacterId = null;
          claim.CharacterGroupId = claim.Project.RootGroup.CharacterGroupId;
        } else if (claim.Character.IsActive == false)
        {
          claim.Character.IsActive = true;
        }
      }

      var email =
        await
          AddCommentWithEmail<RestoreByMasterEmail>(currentUserId, commentText, claim, true,
            s => s.ClaimStatusChange, null, CommentExtraAction.RestoreByMaster);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    public async Task MoveByMaster(int projectId, int claimId, int currentUserId, string contents, int? characterGroupId, int? characterId)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      var source = await ProjectRepository.GetClaimSource(projectId, characterGroupId, characterId);

      //Grab subscribtions before change 
      var subscribe = claim.GetSubscriptions(s => s.ClaimStatusChange, null, true);

      EnsureCanAddClaim(claim.PlayerUserId, source);

      claim.CharacterGroupId = characterGroupId;
      claim.CharacterId = characterId;
      claim.Group = source as CharacterGroup; //That fields is required later
      claim.Character = source as Character; //That fields is required later

      if (claim.IsApproved && claim.CharacterId == null)
      {
        throw new DbEntityValidationException();
      }

      claim.ResponsibleMasterUserId = claim.ResponsibleMasterUserId ?? currentUserId;
      var email =
        await
          AddCommentWithEmail<MoveByMasterEmail>(currentUserId, contents, claim,
            isVisibleToPlayer: true, predicate: s => s.ClaimStatusChange, parentComment: null,
            extraAction: CommentExtraAction.MoveByMaster, extraSubscriptions: subscribe);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    public async Task UpdateReadCommentWatermark(int projectId, int claimId, int currentUserId, int maxCommentId)
    {
      var watermarks =
        UnitOfWork.GetDbSet<ReadCommentWatermark>()
          .Where(w => w.ClaimId == claimId && w.UserId == currentUserId)
          .OrderByDescending(wm => wm.ReadCommentWatermarkId)
          .ToList();

      //Sometimes watermarks can duplicate. If so, let's remove them.
      foreach (var wm in watermarks.Skip(1))
      {
        UnitOfWork.GetDbSet<ReadCommentWatermark>().Remove(wm);
      }

      var watermark = watermarks.FirstOrDefault();

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
      await UnitOfWork.SaveChangesAsync();
    }


    public async Task DeclineByPlayer(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);
      claim.RequestPlayerAccess(currentUserId);
      claim.EnsureStatus(Claim.Status.AddedByUser, Claim.Status.AddedByMaster, Claim.Status.Approved, Claim.Status.OnHold, Claim.Status.Discussed);

      claim.PlayerDeclinedDate = DateTime.UtcNow;
      claim.ClaimStatus = Claim.Status.DeclinedByUser;

      var email =
        await
          AddCommentWithEmail<DeclineByPlayerEmail>(currentUserId, commentText, claim, true,
            s => s.ClaimStatusChange, null, CommentExtraAction.DeclineByPlayer);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    private async Task<T> AddCommentWithEmail<T>(int currentUserId, string commentText, Claim claim,
      bool isVisibleToPlayer, Func<UserSubscription, bool> predicate, Comment parentComment,
      CommentExtraAction? extraAction = null, IEnumerable<User> extraSubscriptions = null) where T : ClaimEmailModel, new()
    {
      var visibleToPlayerUpdated = isVisibleToPlayer && parentComment?.IsVisibleToPlayer != false; 
      claim.AddCommentImpl(currentUserId, parentComment, commentText, visibleToPlayerUpdated, extraAction);

      var extraRecepients =
        new[] {parentComment?.Author, parentComment?.Finance?.PaymentType?.User}.
        Union(extraSubscriptions ?? Enumerable.Empty<User>());
      return
        EmailHelpers.CreateClaimEmail<T>(claim, currentUserId, commentText, predicate, visibleToPlayerUpdated,
          extraAction, await UserRepository.GetById(currentUserId), extraRecepients);
    }

    public async Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId)
    {
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      claim.RequestMasterAccess(currentUserId);
      claim.RequestMasterAccess(responsibleMasterId);

      if (responsibleMasterId == claim.ResponsibleMasterUserId)
      {
        return; // Just do nothing
      }

      var newMaster = await UserRepository.GetById(responsibleMasterId);

      var email = await
        AddCommentWithEmail<ChangeResponsibleMasterEmail>(currentUserId,
          $"{claim.ResponsibleMasterUser?.DisplayName ?? "N/A"} → {newMaster.DisplayName}", claim,
          isVisibleToPlayer: true, predicate: s => s.ClaimStatusChange, parentComment: null,
          extraAction: CommentExtraAction.ChangeResponsible, extraSubscriptions: new [] {newMaster});
      
      claim.ResponsibleMasterUserId = responsibleMasterId;

      await UnitOfWork.SaveChangesAsync();

      await EmailService.Email(email);
    }

    private async Task<Claim> LoadClaimForApprovalDecline(int projectId, int claimId, int currentUserId)
    {
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);
      if (claim == null)
      {
        throw new ArgumentNullException(nameof(claim));
      }
      if (claim.Project == null)
      {
        throw new ArgumentNullException(nameof(IProjectEntity.Project));
      }
      if (!claim.CanManageClaim(currentUserId))
      {
        throw new NoAccessToProjectException(claim.Project, currentUserId, acl => acl.CanManageClaims);
      }
      return claim;
    }

    public async Task SaveFieldsFromClaim(int projectId, int characterId, int currentUserId, IDictionary<int, string> newFieldValue)
    {
      //TODO: Prevent lazy load here - use repository 
      var claim = await LoadProjectSubEntityAsync<Claim>(projectId, characterId);

      FieldSaveHelper.SaveCharacterFields(currentUserId, claim, newFieldValue);
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task OnHoldByMaster(int projectId, int claimId, int currentUserId, string contents)
    {
      
      var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
      claim.ChangeStatusWithCheck(Claim.Status.OnHold);

      var email =
        await
          AddCommentWithEmail<OnHoldByMasterEmail>(currentUserId, contents, claim, true,
            s => s.ClaimStatusChange, null, CommentExtraAction.OnHoldByMaster);

      await UnitOfWork.SaveChangesAsync();
      await EmailService.Email(email);
    }

    public ClaimServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService) : base(unitOfWork, emailService)
    {
    }
  }

  internal static class ClaimStaticExtensions
  {
    public static Comment AddCommentImpl(this Claim claim, int currentUserId, Comment parentComment, string commentText,
      bool isVisibleToPlayer, CommentExtraAction? extraAction)
    {
      if (!isVisibleToPlayer)
      {
        claim.RequestMasterAccess(currentUserId);
      }

      var comment = new Comment
      {
        ProjectId = claim.ProjectId,
        AuthorUserId = currentUserId,
        CommentDiscussionId = claim.Discussion.CommentDiscussionId,
        CommentText = new CommentText() {Text = new MarkdownString(commentText)},
        IsCommentByPlayer = claim.PlayerUserId == currentUserId,
        IsVisibleToPlayer = isVisibleToPlayer,
        Parent = parentComment,
        ExtraAction = extraAction
      };
      claim.Discussion.Comments.Add(comment);

      claim.LastUpdateDateTime = DateTime.UtcNow;


      return comment;
    }

    public static void ConvertToIndividual(this Claim claim)
    {
      if (claim.Group.AvaiableDirectSlots > 0)
      {
        claim.Group.AvaiableDirectSlots -= 1;
      }
      var character = new Character()
      {
        //TODO LOcalize
        CharacterName = $"Новый персонаж в группе {claim.Group.CharacterGroupName}",
        Project = claim.Project,
        ProjectId = claim.ProjectId,
        IsAcceptingClaims = true,
        IsPublic = claim.Group.IsPublic,
        IsActive = true,
        ParentCharacterGroupIds = new[] {claim.CharacterGroupId.Value},
        CharacterId = -1
      };
      claim.CharacterGroupId = null;
      claim.Character = character;
      claim.CharacterId = character.CharacterId;
    }

    public static void SetDiscussed(this Claim claim, int currentUserId, bool isVisibleToPlayer)
    {
      claim.LastUpdateDateTime = DateTime.UtcNow;
      if (claim.ClaimStatus == Claim.Status.AddedByMaster && currentUserId == claim.PlayerUserId)
      {
        claim.ClaimStatus = Claim.Status.Discussed;
      }

      if (claim.ClaimStatus == Claim.Status.AddedByUser && currentUserId != claim.PlayerUserId && isVisibleToPlayer)
      {
        claim.ClaimStatus = Claim.Status.Discussed;
      }
    }
  }
}

