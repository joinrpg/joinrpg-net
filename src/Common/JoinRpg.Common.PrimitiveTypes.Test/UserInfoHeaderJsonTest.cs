using System.Text.Json;
using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.Common.PrimitiveTypes.Test;

/// <summary>
/// UserInfoHeader ездит по проводу (например, список мастеров проекта в /webapi/master/GetList),
/// поэтому сериализация должна быть обратимой.
/// </summary>
public class UserInfoHeaderJsonTest
{
    [Fact]
    public void ShouldRoundtrip()
    {
        var instance = new UserInfoHeader(new UserIdentification(42), new UserDisplayName("Вася", "Василий Пупкин"), Avatar: null);

        var deserialized = JsonSerializer.Deserialize<UserInfoHeader>(JsonSerializer.Serialize(instance));

        deserialized.ShouldBe(instance);
    }

    [Fact]
    public void ShouldRoundtripWithAvatar()
    {
        var instance = new UserInfoHeader(new UserIdentification(42), new UserDisplayName("Вася", "Василий Пупкин"), new AvatarIdentification(17));

        var deserialized = JsonSerializer.Deserialize<UserInfoHeader>(JsonSerializer.Serialize(instance));

        deserialized.ShouldBe(instance);
        deserialized!.Avatar.ShouldBe(new AvatarIdentification(17));
    }

    [Fact]
    public void ShouldRoundtripWithoutFullName()
    {
        var instance = new UserInfoHeader(new UserIdentification(42), new UserDisplayName("Вася", null), Avatar: null);

        var deserialized = JsonSerializer.Deserialize<UserInfoHeader>(JsonSerializer.Serialize(instance));

        deserialized.ShouldBe(instance);
    }
}
