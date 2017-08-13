using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public class AccessArguments
  {
    public AccessArguments(bool masterAccess, bool characterAccess, bool playerAccesToClaim)
    {
      MasterAccess = masterAccess;
      CharacterAccess = characterAccess;
      PlayerAccesToClaim = playerAccesToClaim;
    }

    public AccessArguments([NotNull] Character character, int? userId)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));

      MasterAccess = character.HasMasterAccess(userId);
      CharacterAccess = character.HasPlayerAccess(userId);
      PlayerAccesToClaim = character.ApprovedClaim?.HasPlayerAccesToClaim(userId) ?? false;
    }

    public AccessArguments([NotNull] Claim claim, int? userId)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));

      MasterAccess = claim.HasMasterAccess(userId);
      CharacterAccess = claim.Character?.HasPlayerAccess(userId) ?? false;
      PlayerAccesToClaim = claim.HasPlayerAccesToClaim(userId);
    }

    public bool MasterAccess { get; }
    public bool CharacterAccess { get; }
    public bool PlayerAccesToClaim { get; }

    public override string ToString()
    {
      return $"AccessArguments(MasterAccess:{MasterAccess}, CharacterAccess:{CharacterAccess}, PlayerAccesToClaim:{PlayerAccesToClaim}";
    }
  }
}
