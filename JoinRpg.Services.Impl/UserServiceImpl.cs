using System.Security.Permissions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class UserServiceImpl : DbServiceImplBase, IUserService
  {
    public UserServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task UpdateProfile(int currentUserId, int userId, string surName, string fatherName, string bornName, string prefferedName, Gender gender, string phoneNumber, string nicknames, string groupNames, string skype, string vk, string livejournal)
    {
      if (currentUserId != userId)
      {
        throw new JoinRpgInvalidUserException();
      }
      var user = await UserRepository.WithProfile(userId);

      user.SurName = surName;
      user.FatherName = fatherName;
      user.BornName = bornName;
      user.PrefferedName = prefferedName;

      user.Extra = user.Extra ?? new UserExtra();
      user.Extra.Gender = gender;
      user.Extra.PhoneNumber = phoneNumber;
      user.Extra.Nicknames = nicknames;
      user.Extra.GroupNames = groupNames;
      user.Extra.Skype = skype;
      var tokensToRemove = new[] {"http://", "https://", "vk.com/", "vkontakte.ru/", ".livejournal.com", ".lj.ru", "/", };
      user.Extra.Livejournal = livejournal?.RemoveFromString(tokensToRemove);
      user.Extra.Vk = vk?.RemoveFromString(tokensToRemove);

      await UnitOfWork.SaveChangesAsync();
    }

    [PrincipalPermission(SecurityAction.Demand, Role = Security.AdminRoleName)]
    public async Task ChangeEmail(int userId, string newEmail)
    {
      var user = await UserRepository.GetById(userId);
      user.Email = newEmail;
      //TODO: Send email
      await UnitOfWork.SaveChangesAsync();
    }
  }
}
