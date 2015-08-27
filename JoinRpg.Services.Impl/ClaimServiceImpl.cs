using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  /// <summary>
  /// TODO: Rewrite this calls using real Command pattern.
  /// </summary>
  [UsedImplicitly]
  public class ClaimServiceImpl : DbServiceImplBase, IClaimService
  {
    public void AddClaimFromUser(int projectId, int? characterGroupId, int? characterId, int currentUserId, string claimText)
    {
      if (characterGroupId != null && characterId != null)
      {
        throw new DbEntityValidationException();
      }
      if (characterGroupId != null)
      {
        EnsureCanAddClaim<CharacterGroup>(projectId, characterGroupId.Value, currentUserId);
      } else if (characterId != null)
      {
        EnsureCanAddClaim<Character>(projectId, characterId.Value, currentUserId);
      }
      var addClaimDate = DateTime.Now;
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
        }
      };
      UnitOfWork.GetDbSet<Claim>().Add(claim);
      UnitOfWork.SaveChanges();
    }

    private void EnsureCanAddClaim<T>(int projectId, int characterGroupId, int currentUserId) where T:class, IClaimSource
    {
      if (
        LoadProjectSubEntity<T>(projectId, characterGroupId)
          .Claims.Any(c => c.PlayerUserId == currentUserId && c.IsActive))
      {
        throw new DbEntityValidationException();
      }
    }

    public void AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer, bool isMyClaim, string commentText)
    {
      var claim = LoadClaim(projectId, claimId, currentUserId);

      claim.AddCommentImpl(currentUserId, parentCommentId, isVisibleToPlayer, isMyClaim, commentText, DateTime.Now);
      UnitOfWork.SaveChanges();
    }

    public void AppoveByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = LoadClaimAsMaster(projectId, claimId, currentUserId);
      if (claim.MasterAcceptedDate != null)
      {
        throw new DbEntityValidationException();
      }
      if (claim.PlayerAcceptedDate == null)
      {
        throw new DbEntityValidationException();
      }

      var now = DateTime.Now;
      claim.MasterAcceptedDate = now;
      claim.AddCommentImpl(currentUserId, null, true, false, commentText, now);

      foreach (var otherClaim in claim.OtherClaimsForThisPlayer())
      {
        if (otherClaim.IsApproved)
        {
          throw new DbEntityValidationException();
        }
        DeclineClaimByMasterImpl(currentUserId, otherClaim, now, "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект");
      }

      if (claim.Group != null)
      {
        //TODO: Добавить здесь возможность ввести имя персонажа или брать его из заявки
        claim.ConvertToIndividual();
      }
      UnitOfWork.SaveChanges();
    }

    public void DeclineByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = LoadClaimAsMaster(projectId, claimId, currentUserId);
      if (claim.MasterDeclinedDate != null)
      {
        throw new DbEntityValidationException();
      }

      DeclineClaimByMasterImpl(currentUserId, claim, DateTime.Now, commentText);

      UnitOfWork.SaveChanges();
    }

    public void DeclineByPlayer(int projectId, int claimId, int currentUserId, string commentText)
    {
      var claim = LoadMyClaim(projectId, claimId, currentUserId);
      if (claim.PlayerDeclinedDate != null)
      {
        throw new DbEntityValidationException();
      }

      DateTime now = DateTime.Now;
      claim.MasterDeclinedDate = now;
      claim.AddCommentImpl(currentUserId, null, isVisibleToPlayer: true, isMyClaim: true, commentText: commentText, now: now);

      UnitOfWork.SaveChanges();
    }

    private static void DeclineClaimByMasterImpl(int currentUserId, Claim otherClaim, DateTime now, string commentText)
    {
      otherClaim.MasterDeclinedDate = now;
      otherClaim.AddCommentImpl(currentUserId, null, true, false, commentText, now);
    }

    private Claim LoadClaimAsMaster(int projectId, int claimId, int currentUserId)
    {
      var claim = LoadClaim(projectId, claimId, currentUserId);
      if (!claim.Project.HasAccess(currentUserId))
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

    public ClaimServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }

  internal static class ClaimStaticExtensions
  {
    public static void AddCommentImpl(this Claim claim, int currentUserId, int? parentCommentId, bool isVisibleToPlayer,
      bool isMyClaim, string commentText, DateTime now)
    {
      if (String.IsNullOrWhiteSpace(commentText))
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
      claim.Group.AvaiableDirectSlots -= 1;
      var character = new Character()
      {
        CharacterName = "Новый персонаж в группе" + claim.Group.CharacterGroupName,
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
  }
}
