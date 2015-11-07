using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces.Allrpg
{
  public interface IAllrpgService
  {
    Task<DownloadResult> DownloadAllrpgProfile(int userId);

    Task<LegacyLoginResult> TryToLoginWithOldPassword(string email, string password);
    Task AssociateProject(int currentUserId, int projectId, int allrpgProjectId);
  }

  public enum LegacyLoginResult
  {
    NoSuchUserOrPassword,
    Success,
    ImportDisabled,
    NetworkError,
    ParseError,
    WrongKey
  }

}
