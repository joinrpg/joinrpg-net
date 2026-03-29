using JoinRpg.Portal.Controllers.XGameApi;
using JoinRpg.PrimitiveTypes;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.Portal.Test.XGameApi;

public class CreateCharacterRequestMapperTest
{
    public static IEnumerable<object[]> AllCharacterTypeApiValues =>
        Enum.GetValues<CharacterTypeApi>().Select(v => new object[] { v });

    public static IEnumerable<object[]> AllCharacterVisibilityApiValues =>
        Enum.GetValues<CharacterVisibilityApi>().Select(v => new object[] { v });

    [Theory]
    [MemberData(nameof(AllCharacterTypeApiValues))]
    public void MapCharacterType_AllValues(CharacterTypeApi input)
    {
        Should.NotThrow(() => CreateCharacterRequestMapper.MapCharacterType(input));
    }

    [Theory]
    [MemberData(nameof(AllCharacterVisibilityApiValues))]
    public void MapCharacterVisibility_AllValues(CharacterVisibilityApi input)
    {
        Should.NotThrow(() => CreateCharacterRequestMapper.MapCharacterVisibility(input));
    }

    [Fact]
    public void ToCharacterTypeInfo_PlayerDefault_Success()
    {
        var request = new CreateCharacterRequest { CharacterType = CharacterTypeApi.Player };
        var info = CreateCharacterRequestMapper.ToCharacterTypeInfo(request);
        info.CharacterType.ShouldBe(CharacterType.Player);
        info.IsHot.ShouldBeFalse();
        info.CharacterVisibility.ShouldBe(CharacterVisibility.Public);
    }

    [Fact]
    public void ToCharacterTypeInfo_PlayerHot_Success()
    {
        var request = new CreateCharacterRequest { CharacterType = CharacterTypeApi.Player, IsHot = true };
        var info = CreateCharacterRequestMapper.ToCharacterTypeInfo(request);
        info.CharacterType.ShouldBe(CharacterType.Player);
        info.IsHot.ShouldBeTrue();
    }

    [Fact]
    public void ToCharacterTypeInfo_NonPlayerHot_ThrowsArgumentException()
    {
        var request = new CreateCharacterRequest { CharacterType = CharacterTypeApi.NonPlayer, IsHot = true };
        Should.Throw<ArgumentException>(() => CreateCharacterRequestMapper.ToCharacterTypeInfo(request));
    }

    [Fact]
    public void ToCharacterTypeInfo_Slot_Success()
    {
        var request = new CreateCharacterRequest
        {
            CharacterType = CharacterTypeApi.Slot,
            SlotName = "Тестовый шаблон",
            SlotLimit = 5,
        };
        var info = CreateCharacterRequestMapper.ToCharacterTypeInfo(request);
        info.CharacterType.ShouldBe(CharacterType.Slot);
        info.SlotName.ShouldBe("Тестовый шаблон");
        info.SlotLimit.ShouldBe(5);
    }

    [Fact]
    public void ToCharacterTypeInfo_PlayerWithSlotLimit_ThrowsArgumentException()
    {
        var request = new CreateCharacterRequest { CharacterType = CharacterTypeApi.Player, SlotLimit = 5 };
        Should.Throw<ArgumentException>(() => CreateCharacterRequestMapper.ToCharacterTypeInfo(request));
    }

    [Fact]
    public void ToCharacterTypeInfo_PlayerWithSlotName_ThrowsArgumentException()
    {
        var request = new CreateCharacterRequest { CharacterType = CharacterTypeApi.Player, SlotName = "имя" };
        Should.Throw<ArgumentException>(() => CreateCharacterRequestMapper.ToCharacterTypeInfo(request));
    }

    [Theory]
    [MemberData(nameof(AllCharacterVisibilityApiValues))]
    public void ToCharacterTypeInfo_AllVisibilities_Mapped(CharacterVisibilityApi visibilityApi)
    {
        var request = new CreateCharacterRequest { CharacterVisibility = visibilityApi };
        Should.NotThrow(() => CreateCharacterRequestMapper.ToCharacterTypeInfo(request));
    }
}
