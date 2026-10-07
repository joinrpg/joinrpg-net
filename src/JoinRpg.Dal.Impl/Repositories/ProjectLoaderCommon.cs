namespace JoinRpg.Dal.Impl.Repositories;

internal static class ProjectLoaderCommon
{
    // TODO не грузить тут лишнего, в частности в Details
    public static async Task<Project?> GetProjectWithFieldsAsync(MyDbContext ctx, int project, bool skipCache)
    {
        var query = skipCache ? ctx.ProjectsSet.AsNoTracking() : ctx.ProjectsSet;
        return await query
         .Include(p => p.Details)
         // Поля имени и описания у ProjectDetails — только навигации, FK-свойства не отображены,
         // а ProjectInfo читает их идентификаторы всегда. Без Include это две ленивые загрузки
         // на каждую сборку метаданных — и в чтении, и в каждой доменной операции (#4987).
         .Include(p => p.Details.CharacterNameField)
         .Include(p => p.Details.CharacterDescription)
         .Include(p => p.ProjectAcls.Select(a => a.User))
         .Include(p => p.ProjectFields.Select(f => f.DropdownValues))
         .Include(p => p.PaymentTypes.Select(p => p.User))
         .Include(p => p.ProjectFeeSettings)
         .Include(p => p.KogdaIgraGames)
         .Include(p => p.ProjectRolesLists)
         .Include(p => p.CharacterGroups)
         // Типы проживания — настройка проекта и часть ProjectInfo (ADR015). Строки узкие,
         // типов на проект единицы-десятки, связанных коллекций не тянем.
         .Include(p => p.ProjectAccommodationTypes)
         // Категории комнат — тоже настройка мастера (ADR020), строки ещё уже: id и имя.
         .Include(p => p.ProjectRoomCategories)
         .SingleOrDefaultAsync(p => p.ProjectId == project);
    }

    public static ProjectLifecycleStatus CreateStatus(bool active, bool isAcceptingClaims)
    {
        return (active, isAcceptingClaims) switch
        {
            (true, false) => ProjectLifecycleStatus.ActiveClaimsClosed,
            (true, true) => ProjectLifecycleStatus.ActiveClaimsOpen,
            (false, false) => ProjectLifecycleStatus.Archived,
            (false, true) => throw new InvalidOperationException()
        };
    }
}
