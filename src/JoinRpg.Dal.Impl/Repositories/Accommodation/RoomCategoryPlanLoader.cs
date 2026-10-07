using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories.Accommodation;

/// <summary>
/// Чистое ядро загрузки <see cref="RoomCategoryPlan"/> (ADR018): один запрос с явной проекцией
/// в row-типы плюс <see cref="RoomCategoryPlanMapper"/>.
/// </summary>
/// <remarks>
/// <para>
/// Сознательно НЕ обращается к <c>IProjectMetadataRepository</c>: готовый <see cref="ProjectInfo"/>
/// приходит снаружи. Это нужно пути записи, где <see cref="ProjectInfo"/> собирается свой и должен
/// быть ровно тем же экземпляром, что и внутри плана — иначе сработает проверка <c>ReferenceEquals</c>
/// в конструкторе агрегата (ADR018, §3).
/// </para>
/// <para>
/// Сознательно НЕ наследуется от <c>GameRepositoryImplBase</c>: тот прогревает контекст всем
/// проектом целиком — главная причина медленной сетки ролей из ADR011. Здесь каждая загрузка
/// делает ровно один запрос с явной проекцией, без <c>Include</c> и без lazy load.
/// </para>
/// <para>
/// Типы проживания и вместимость из БД не читаются вовсе: и то и другое — настройка мастера,
/// она приходит из <see cref="ProjectInfo"/> (ADR018, §3; ADR020, §2).
/// </para>
/// </remarks>
internal sealed class RoomCategoryPlanLoader(MyDbContext ctx)
{
    /// <summary>
    /// Планы категорий проекта <paramref name="projectInfo"/>, удовлетворяющих
    /// <paramref name="predicate"/>. Все они разделяют переданный экземпляр
    /// <see cref="ProjectInfo"/>.
    /// </summary>
    public async Task<IReadOnlyCollection<RoomCategoryPlan>> LoadAsync(
        ProjectInfo projectInfo,
        Expression<Func<ProjectRoomCategory, bool>> predicate)
    {
        var projectId = projectInfo.ProjectId;

        var query =
            from category in ctx.Set<ProjectRoomCategory>().AsNoTracking().AsExpandable()
            where category.ProjectId == projectId.Value
            where predicate.Invoke(category)
            select new RoomCategoryPlanRow
            {
                RoomCategoryId = category.Id,
                Rooms = category.Rooms.Select(room => new RoomCategoryPlanRoomRow
                {
                    RoomId = room.Id,
                    Name = room.Name,
                }),
                // Группы — вложенным Select, а не Include: нужны все группы пула, включая ещё
                // не расселённые (у них AccommodationId пуст). Группа покупает тип, поэтому в пул
                // она попадает через свой тип проживания (ADR020).
                Groups = category.AccommodationTypes.SelectMany(type => type.Desirous).Select(request => new RoomCategoryPlanGroupRow
                {
                    GroupId = request.Id,
                    AccommodationTypeId = request.AccommodationTypeId,
                    RoomId = request.AccommodationId,
                    SubjectClaimIds = request.Subjects.Select(claim => claim.ClaimId),
                }),
            };

        var rows = await query.ToListAsync();

        return [.. rows.Select(row => RoomCategoryPlanMapper.Map(row, projectInfo))];
    }

    /// <summary>
    /// Планы всех категорий проекта — одним запросом, как и план одной категории.
    /// </summary>
    public Task<IReadOnlyCollection<RoomCategoryPlan>> LoadAllAsync(ProjectInfo projectInfo)
        => LoadAsync(projectInfo, category => true);

    /// <summary>
    /// План одной категории или <c>null</c>, если такой категории в проекте нет.
    /// </summary>
    public async Task<RoomCategoryPlan?> LoadOneAsync(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId)
    {
        var categoryIntId = categoryId.RoomCategoryId;
        var plans = await LoadAsync(projectInfo, category => category.Id == categoryIntId);
        return plans.SingleOrDefault();
    }
}
