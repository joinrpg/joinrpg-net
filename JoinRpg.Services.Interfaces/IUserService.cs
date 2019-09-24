using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IUserService
  {
    Task UpdateProfile(int userId, string surName, string fatherName, string bornName, string prefferedName, Gender gender, string phoneNumber, string nicknames, string groupNames, string skype, string vk, string livejournal, string telegram);
    Task ChangeEmail(int userId, string newEmail);
    Task SetAdminFlag(int userId, bool administratorFlag);
        Task SetVerificationFlag(int userId, bool verificationFlag);
    }
}
