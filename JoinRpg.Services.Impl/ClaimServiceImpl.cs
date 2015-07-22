using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
      var claim = new Claim()
      {
        CharacterGroupId = characterGroupId,
        CharacterId = characterId,
        ProjectId = projectId,
        PlayerUserId = currentUserId,
        PlayerAcceptedDate = DateTime.Now,
      };
      //TODO: Add claim text as first comment
      UnitOfWork.GetDbSet<Claim>().Add(claim);
      UnitOfWork.SaveChanges();
    }

    public ClaimServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
  }
}
