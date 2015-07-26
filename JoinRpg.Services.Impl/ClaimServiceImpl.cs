using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
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
        LoadProjectSubEntity<CharacterGroup>(projectId, characterGroupId.Value);
      } else if (characterId != null)
      {
        LoadProjectSubEntity<Character>(projectId, characterId.Value);
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

    public void AddComment(int projectId, int claimId, int currentUserId, int? parentCommentId, bool isVisibleToPlayer, bool isMyClaim, string commentText)
    {
      LoadProjectSubEntity<Claim>(projectId, claimId);
      if (string.IsNullOrWhiteSpace(commentText))
      {
        throw new DbEntityValidationException();
      }
      if (!isVisibleToPlayer && isMyClaim)
      {
        throw new DbEntityValidationException();
      }
      var now = DateTime.Now;
      var comment = new Comment()
      {
        ProjectId = projectId,
        AuthorUserId = currentUserId,
        ClaimId = claimId,
        CommentText = new MarkdownString(commentText),
        CreatedTime = now,
        IsCommentByPlayer = isMyClaim,
        IsVisibleToPlayer = isVisibleToPlayer,
        ParentCommentId = parentCommentId,
        LastEditTime = now
      };
      UnitOfWork.GetDbSet<Comment>().Add(comment);
      UnitOfWork.SaveChanges();

    }

    public ClaimServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
