using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories;
//TODO: temporary duplication from domain
public static class UserTransformationExtensions
{
    public static UserFullName ExtractFullName(this User user)
    {
        return new UserFullName(
            PrefferedName.FromOptional(user.PrefferedName),
            BornName.FromOptional(user.BornName),
            SurName.FromOptional(user.SurName),
            FatherName.FromOptional(user.FatherName));
    }

    public static UserDisplayName ExtractDisplayName(this User user)
    {
        return UserDisplayName.Create(user.ExtractFullName(), new Email(user.Email));
    }
}
