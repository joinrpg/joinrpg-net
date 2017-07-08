using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  internal class CharacterRepositoryImpl : RepositoryImplBase, ICharacterRepository
  {
    public async Task<IReadOnlyCollection<int>> GetCharacterIds(int projectId, DateTime? modifiedSince)
    {
      return await Ctx.Set<Character>().Where(c => c.ProjectId == projectId && c.IsActive &&
                                             (modifiedSince == null ||
                                              c.UpdatedAt >= modifiedSince))
        .Select(c => c.CharacterId).ToListAsync();
    }

    public async Task<IReadOnlyCollection<Character>> GetCharacters(int projectId, IReadOnlyCollection<int> characterIds)
    {
      return await Ctx.Set<Character>().Where(cg => cg.ProjectId == projectId && characterIds.Contains(cg.CharacterId)).ToListAsync();
    }

    public CharacterRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }
  }
}
