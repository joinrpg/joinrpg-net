using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Dal.Impl;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
  [UsedImplicitly]
  public class UserServiceImpl : DbServiceImplBase, IUserService
  {
    public UserServiceImpl(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task UpdateProfile(int currentUserId, int userId, string surName, string fatherName, string bornName,
      string prefferedName, Gender gender, string phoneNumber, string nicknames, string groupNames, string skype)
    {
      if (currentUserId != userId)
      {
        throw new Exception("Not authorized");
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

      await UnitOfWork.SaveChangesAsync();
    }
  }
}
