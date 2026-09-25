using JoinRpg.Data.Interfaces.Plots;

namespace JoinRpg.Domain;

/// <summary>
/// Мост из EF-сущности вводной в <see cref="PlotElementDetailsDto"/>.
/// </summary>
/// <remarks>
/// Временный: он нужен, пока <c>IPlotRepository</c> отдаёт <c>PlotElement</c>. Когда репозиторий
/// научится отдавать DTO сам, этот класс уходит вместе с последним вызовом. Лежит рядом
/// с <see cref="PlotExtensions"/>, который так же переводит EF-сущность в доменные типы:
/// проектам <c>Web.*</c> работа с EF-сущностями не положена.
/// </remarks>
public static class PlotElementDetailsDtoBuilder
{
    /// <summary>
    /// Собирает DTO по вводной для указанной версии.
    /// </summary>
    /// <param name="element">EF-сущность вводной. Её таргеты и тексты должны быть уже загружены.</param>
    /// <param name="version">Версия для показа; <c>null</c> — последняя.</param>
    /// <exception cref="ArgumentOutOfRangeException">Такой версии у вводной нет.</exception>
    public static PlotElementDetailsDto GetDetails(this PlotElement element, int? version = null)
    {
        var lastVersionNumber = element.LastVersion().Version;
        var currentVersionNumber = version ?? lastVersionNumber;
        var currentVersion = element.SpecificVersion(currentVersionNumber)
            ?? throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                $"У вводной {element.GetId()} нет версии {currentVersionNumber}");

        return new PlotElementDetailsDto(
            element.GetId(),
            element.ElementType,
            element.IsMasterOnly,
            element.IsActive,
            element.Published,
            element.ToTarget(),
            element.PlotFolder.MasterTitle,
            ToVersionDto(currentVersion),
            lastVersionNumber,
            element.SpecificVersion(currentVersionNumber - 1)?.ModifiedDateTime,
            element.SpecificVersion(currentVersionNumber + 1)?.ModifiedDateTime);
    }

    private static PlotElementVersionDto ToVersionDto(PlotElementTexts text)
        => new(
            text.Version,
            text.Content,
            text.TodoField,
            text.ModifiedDateTime,
            text.AuthorUser?.ToUserInfoHeader());
}
