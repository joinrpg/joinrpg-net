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
/// Запрос строится от таблицы типов проживания: своей таблицы у категории комнат пока нет, и
/// «категория» — это тот же ряд <c>ProjectAccommodationType</c> (ADR018, «Задел на разделение»,
/// пункт 1). Конвертация «id категории ↔ колонка id типа» допустима только здесь, в DAL, где
/// отображение «id ↔ колонка» и так живёт для каждой сущности; в домене её нет.
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
        Expression<Func<ProjectAccommodationType, bool>> predicate)
    {
        var projectId = projectInfo.ProjectId;

        var query =
            from category in ctx.Set<ProjectAccommodationType>().AsNoTracking().AsExpandable()
            where category.ProjectId == projectId.Value
            where predicate.Invoke(category)
            select new RoomCategoryPlanRow
            {
                RoomCategoryId = category.Id,
                // Сегодня физическая вместимость пула и продаваемая вместимость типа — одно
                // число в одной колонке (ADR018, «Задел на разделение», пункт 3).
                RoomCapacity = category.Capacity,
                Rooms = category.ProjectAccommodations.Select(room => new RoomCategoryPlanRoomRow
                {
                    RoomId = room.Id,
                    Name = room.Name,
                }),
                // Группы — вложенным Select, а не Include: нужны все группы пула, включая ещё
                // не расселённые (у них AccommodationId пуст).
                Groups = category.Desirous.Select(request => new RoomCategoryPlanGroupRow
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
    /// План одной категории или <c>null</c>, если такой категории в проекте нет.
    /// </summary>
    public async Task<RoomCategoryPlan?> LoadOneAsync(
        ProjectInfo projectInfo,
        RoomCategoryIdentification categoryId)
    {
        // См. замечание к классу: сегодня id категории — это id ряда ProjectAccommodationType.
        var categoryIntId = categoryId.RoomCategoryId;
        var plans = await LoadAsync(projectInfo, category => category.Id == categoryIntId);
        return plans.SingleOrDefault();
    }
}
