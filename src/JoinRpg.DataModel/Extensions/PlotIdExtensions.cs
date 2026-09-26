using JoinRpg.DomainTypes.Plots;

namespace JoinRpg.DataModel.Extensions;

/// <summary>
/// Извлечение идентификаторов сущностей сюжета. Живёт в <c>JoinRpg.DataModel</c>, а не в
/// <c>JoinRpg.Domain</c>, потому что нужно и слою доступа к данным: папку сюжета в DTO
/// перекладывают и <c>JoinRpg.Dal.Impl</c>, и <c>JoinRpg.Data.Interfaces</c>, а на
/// <c>JoinRpg.Domain</c> ни тот, ни другой не ссылается. Тот же случай, что
/// у <see cref="ClaimExtensions"/>.
/// </summary>
/// <remarks>
/// Одноимённые методы в <c>JoinRpg.Domain.IdExtensions</c> пока остаются: их вычистка задела бы
/// 8 файлов и не относится к текущей задаче. Оба namespace в одном файле подключать нельзя —
/// вызов станет неоднозначным.
/// </remarks>
public static class PlotIdExtensions
{
    /// <summary>Идентификатор папки сюжета.</summary>
    public static PlotFolderIdentification GetId(this PlotFolder folder) => new(folder.ProjectId, folder.PlotFolderId);

    /// <summary>Идентификатор вводной.</summary>
    public static PlotElementIdentification GetId(this PlotElement element)
        => new(element.ProjectId, element.PlotFolderId, element.PlotElementId);
}
