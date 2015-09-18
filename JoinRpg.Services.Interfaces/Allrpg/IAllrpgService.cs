using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces.Allrpg
{
  public interface IAllrpgService
  {
    Task<DownloadResult> DownloadAllrpgProfile(int userId, string allrpgKey);
  }

}
