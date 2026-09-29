namespace JoinRpg.Domain;

public static class WorldObjectExtensions
{
    /// <summary>
    /// Виден ли объект пользователю (или анонимному посетителю, если <paramref name="currentUserId"/> не задан).
    /// </summary>
    public static bool IsVisible(this IWorldObject cg, UserIdentification? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(cg);

        return cg.IsPublic || cg.Project.Details.PublishPlot || cg.HasMasterAccess(currentUserId);
    }

    /// <summary>
    /// Виден ли объект пользователю — поверх уже загруженного <see cref="ProjectInfo"/>
    /// (кешируется на запрос, см. <c>IProjectMetadataRepository</c>), без ленивых загрузок (#4991).
    /// </summary>
    public static bool IsVisible(this IWorldObject cg, ProjectInfo projectInfo, UserIdentification? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(cg);
        ArgumentNullException.ThrowIfNull(projectInfo);

        return cg.IsPublic || projectInfo.PublishPlot || projectInfo.HasMasterAccess(currentUserId);
    }

    // Перегрузка для кода, который ещё не перешёл на типизированные id. Вызывающим с UserIdentification?
    // она не нужна и даже опасна: nullable-значение ушло бы в неё через сгенерированный
    // implicit operator int(UserIdentification), который на null бросает NullReferenceException.
    [Obsolete("Use AccessArguments & ProjectInfo")]
    public static bool IsVisible(this IWorldObject cg, int? currentUserId)
        => cg.IsVisible(UserIdentification.FromOptional(currentUserId));
}
