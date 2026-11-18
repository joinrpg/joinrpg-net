using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Portal.Models;

/// <summary>
/// Главное меню: отдельно проекты, где я мастер, и отдельно мои активные заявки.
/// </summary>
/// <param name="MasterProjects">Активные проекты с мастерским доступом (уже отсортированы и обрезаны)</param>
/// <param name="MyClaims">Активные заявки в активных проектах (уже отсортированы и обрезаны; остальные — в профиле по ссылке «Архив»)</param>
/// <param name="HasMoreProjects">Показать ссылку на полный список игр</param>
/// <param name="IsAuthenticated">Анониму показывать нечего — прячем все выпадающее меню целиком</param>
/// <param name="CurrentProjectName">Название проекта, в котором мы сейчас находимся</param>
public record class MainMenuViewModel(
    ProjectPersonalizedInfo[] MasterProjects,
    MyClaimShortInfo[] MyClaims,
    bool HasMoreProjects,
    bool IsAuthenticated,
    string? CurrentProjectName)
{
    public static MainMenuViewModel Empty(bool isAuthenticated = false, string? currentProjectName = null)
        => new([], [], false, isAuthenticated, currentProjectName);
}
