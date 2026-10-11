using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Dal.Impl.Repositories;

internal class UserMergeRepository(MyDbContext ctx) : IUserMergeRepository
{
    async Task<UserMergePreview> IUserMergeRepository.GetPreview(UserIdentification primary, UserIdentification secondary)
    {
        int a = primary.Value, b = secondary.Value;
        var conflicts = new List<UserMergeConflict>();

        // UK (ProjectId, UserId): две строки в проекте — это ровно оба аккаунта.
        var bothMasters = await ctx.Set<ProjectAcl>()
            .Where(acl => acl.UserId == a || acl.UserId == b)
            .GroupBy(acl => acl.ProjectId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();
        conflicts.AddRange(bothMasters.Select(p => new UserMergeConflict.BothMasters(new(p))));

        var bothClaimed = await ctx.ClaimSet
            .Where(c => c.PlayerUserId == a || c.PlayerUserId == b)
            .Where(ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active))
            .GroupBy(c => new { c.ProjectId, c.CharacterId })
            .Where(g => g.Select(c => c.PlayerUserId).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();
        conflicts.AddRange(bothClaimed.Select(k => new UserMergeConflict.BothClaimedCharacter(new(new(k.ProjectId), k.CharacterId))));

        // Инвариант «один персонаж»: при утверждённой заявке других активных у игрока нет — утверждение
        // отклоняет остальные (ClaimServiceImpl.ApproveCore). Две активные без утверждённой — норма.
        var singleCharacterClaims = await ctx.ClaimSet
            .Where(c => c.PlayerUserId == a || c.PlayerUserId == b)
            .Where(ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active))
            .Where(c => !c.Project.Details.EnableManyCharacters)
            .GroupBy(c => c.ProjectId)
            .Where(g => g.Select(c => c.PlayerUserId).Distinct().Count() > 1
                && g.Any(c => c.ClaimStatus == ClaimStatus.Approved || c.ClaimStatus == ClaimStatus.CheckedIn))
            .Select(g => g.Key)
            .ToListAsync();
        conflicts.AddRange(singleCharacterClaims.Select(p => new UserMergeConflict.ApprovedClaimInSingleCharacterProject(new(p))));

        // Неактивную кассу тоже считаем: ProjectFinanceSettings ищет кассу мастера без фильтра по IsActive.
        var bothCash = await ctx.Set<PaymentType>()
            .Where(pt => pt.UserId == a || pt.UserId == b)
            .Where(pt => pt.TypeKind == PaymentTypeKind.Cash)
            .GroupBy(pt => pt.ProjectId)
            .Where(g => g.Select(pt => pt.UserId).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();
        conflicts.AddRange(bothCash.Select(p => new UserMergeConflict.BothHaveCashPaymentType(new(p))));

        var transfers = await ctx.Set<MoneyTransfer>()
            .Where(t => (t.SenderId == a && t.ReceiverId == b) || (t.SenderId == b && t.ReceiverId == a))
            .Select(t => t.ProjectId)
            .Distinct()
            .ToListAsync();
        conflicts.AddRange(transfers.Select(p => new UserMergeConflict.MoneyTransfersBetween(new(p))));

        var bothCaptains = await ctx.Set<CaptainAccessRuleEntity>()
            .Where(r => r.CaptainUserId == a || r.CaptainUserId == b)
            .GroupBy(r => new { r.ProjectId, r.CharacterGroupId })
            .Where(g => g.Select(r => r.CaptainUserId).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();
        conflicts.AddRange(bothCaptains.Select(k => new UserMergeConflict.BothCaptains(new(new(k.ProjectId), k.CharacterGroupId))));

        // Не только проекты с данными этих аккаунтов: восстановление ещё может их туда вставить (ADR024).
        var blockedProjects = await ctx.ProjectsSet
            .Where(ProjectPredicates.Status(ProjectLifecycleStatus.Blocked))
            .Select(p => p.ProjectId)
            .OrderBy(p => p)
            .ToListAsync();
        conflicts.AddRange(blockedProjects.Select(p => new UserMergeConflict.BlockedProject(new(p))));

        var primaryProviders = ctx.Set<UserExternalLogin>().Where(l => l.UserId == a).Select(l => l.Provider);
        var droppedProviders = await ctx.Set<UserExternalLogin>()
            .Where(l => l.UserId == b && primaryProviders.Contains(l.Provider))
            .Select(l => l.Provider)
            .ToListAsync();

        var stats = new UserMergeSecondaryStats(
            Claims: await ctx.ClaimSet.CountAsync(c => c.PlayerUserId == b),
            Comments: await ctx.Set<Comment>().CountAsync(c => c.AuthorUserId == b),
            MasterProjects: await ctx.Set<ProjectAcl>().CountAsync(acl => acl.UserId == b));

        return new UserMergePreview(primary, secondary, conflicts, droppedProviders, stats);
    }
}
