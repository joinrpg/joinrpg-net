using System.Collections.Generic;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces.Allrpg
{
  public interface IAllrpgService
  {
    Task<DownloadResult> DownloadAllrpgProfile(int userId);

    Task<LegacyLoginResult> TryToLoginWithOldPassword(string email, string password);
    Task AssociateProject(int currentUserId, int projectId, int allrpgProjectId);
    Task<IEnumerable<string>> UpdateProject(int currentUserId, int projectId);
  }

  public enum LegacyLoginResult
  {
    NoSuchUserOrPassword,
    Success,
    ImportDisabled,
    NetworkError,
    ParseError,
    WrongKey,
    RegisterNewUser
  }

}
