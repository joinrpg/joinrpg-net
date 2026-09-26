using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;

namespace JoinRpg.Data.Interfaces.Plots;

/// <summary>
/// Мост из EF-сущностей сюжета в DTO.
/// </summary>
/// <remarks>
/// Лежит в <c>Data.Interfaces</c>, а не в <c>JoinRpg.Domain</c>, потому что нужен и репозиторию:
/// <c>Dal.Impl</c> на <c>Domain</c> не ссылается. По той же причине выбор версий и таргеты собраны
/// здесь вручную, а не через <c>PlotExtensions</c>.
///
/// Публичным он остаётся временно: последний вызов извне — перегрузка
/// <c>PlotElementListItemViewModel.FromFolder(PlotFolder …)</c> для списка сюжетов. Когда и её
/// переведут на DTO, класс уедет внутрь <c>Dal.Impl</c> и станет <c>internal</c>.
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
        var lastVersionNumber = LastVersion(element).Version;
        var currentVersionNumber = version ?? lastVersionNumber;
        var currentVersion = SpecificVersion(element, currentVersionNumber)
            ?? throw new ArgumentOutOfRangeException(
                nameof(version),
                version,
                $"У вводной {element.GetId()} нет версии {currentVersionNumber}");

        return new PlotElementDetailsDto(
            element.GetId(),
            element.ElementType,
            element.IsMasterOnly,
            element.IsActive,
            element.IsCompleted,
            element.Published,
            ToTarget(element),
            element.PlotFolder.MasterTitle,
            ToVersionDto(currentVersion),
            lastVersionNumber,
            SpecificVersion(element, currentVersionNumber - 1)?.ModifiedDateTime,
            SpecificVersion(element, currentVersionNumber + 1)?.ModifiedDateTime);
    }

    private static PlotElementTexts LastVersion(PlotElement element)
        => element.Texts.OrderByDescending(text => text.Version).First();

    private static PlotElementTexts? SpecificVersion(PlotElement element, int version)
        => element.Texts.SingleOrDefault(text => text.Version == version);

    private static TargetsInfo ToTarget(PlotElement element)
    {
        var projectId = new ProjectIdentification(element.ProjectId);
        return new TargetsInfo(
            [.. element.TargetCharacters.Select(
                x => new CharacterTarget(new CharacterIdentification(projectId, x.CharacterId), x.CharacterName))],
            [.. element.TargetGroups.Select(
                x => new GroupTarget(new CharacterGroupIdentification(projectId, x.CharacterGroupId), x.CharacterGroupName))]);
    }

    private static PlotElementVersionDto ToVersionDto(PlotElementTexts text)
        => new(
            text.Version,
            text.Content,
            text.TodoField,
            text.ModifiedDateTime,
            ToAuthor(text.AuthorUser));

    /// <remarks>
    /// Имя собирается вручную, а не через <c>UserExtensions.ToUserInfoHeader</c>: тот живёт
    /// в <c>JoinRpg.Domain</c>, на который <c>Dal.Impl</c> не ссылается. Так же поступают
    /// и соседние репозитории, например <c>CharacterGroupRepository</c>.
    /// </remarks>
    private static UserInfoHeader? ToAuthor(User? author)
        => author is null
            ? null
            : new UserInfoHeader(
                new UserIdentification(author.UserId),
                new UserDisplayName(
                    new UserFullName(
                        PrefferedName.FromOptional(author.PrefferedName),
                        BornName.FromOptional(author.BornName),
                        SurName.FromOptional(author.SurName),
                        FatherName.FromOptional(author.FatherName)),
                    new Email(author.Email)));
}
