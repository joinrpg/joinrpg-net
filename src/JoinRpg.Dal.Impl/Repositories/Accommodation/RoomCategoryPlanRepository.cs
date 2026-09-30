using JoinRpg.Data.Interfaces.Accommodation;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Dal.Impl.Repositories.Accommodation;

/// <summary>
/// Загрузчик <see cref="RoomCategoryPlan"/> (ADR018).
/// </summary>
/// <remarks>
/// Тонкая обёртка над <see cref="RoomCategoryPlanLoader"/>: сам запрос живёт там, здесь только
/// получение <see cref="ProjectInfo"/> из <see cref="IProjectMetadataRepository"/> (за ним
/// в Portal стоит кеш запроса).
/// </remarks>
internal class RoomCategoryPlanRepository(MyDbContext ctx, IProjectMetadataRepository projectMetadataRepository)
    : IRoomCategoryPlanRepository
{
    private readonly RoomCategoryPlanLoader loader = new(ctx);

    public async Task<RoomCategoryPlan?> GetPlanForTypeOrDefault(AccommodationTypeIdentification typeId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(typeId.ProjectId);

        // Категорию по типу знают только метаданные — конвертации идентификаторов нет (ADR018, §2).
        var type = projectInfo.AccommodationSettings.GetTypeByIdOrDefault(typeId);
        if (type is null)
        {
            return null;
        }

        return await loader.LoadOneAsync(projectInfo, type.RoomCategoryId);
    }
}
