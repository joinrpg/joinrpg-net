using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models;

/// <summary>
/// Страница «Настройка финансов». Панели — острова и данные берут сами; здесь только то, что нужно выгрузке в Google Таблицы.
/// </summary>
public class FinanceSetupViewModel
{
    public int ProjectId { get; }

    public string CurrentUserToken { get; }

    public FinanceSetupViewModel(Project project, int currentUserId)
    {
        ProjectId = project.ProjectId;
        CurrentUserToken = project.ProjectAcls.Single(acl => acl.UserId == currentUserId)
            .Token.ToHexString();
    }
}
