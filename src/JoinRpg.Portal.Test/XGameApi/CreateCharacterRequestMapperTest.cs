using JoinRpg.Portal.Controllers.XGameApi;
using JoinRpg.PrimitiveTypes;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.Portal.Test.XGameApi;

public class CreateCharacterRequestMapperTest
{
    [Theory]
    [InlineData(CharacterTypeApi.Player, CharacterType.Player)]
    [InlineData(CharacterTypeApi.NonPlayer, CharacterType.NonPlayer)]
    [InlineData(CharacterTypeApi.Slot, CharacterType.Slot)]
    public void MapCharacterType_AllValues(CharacterTypeApi input, CharacterType expected)
    {
        CreateCharacterRequestMapper.MapCharacterType(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(CharacterVisibilityApi.Public, CharacterVisibility.Public)]
    [InlineData(CharacterVisibilityApi.PlayerHidden, CharacterVisibility.PlayerHidden)]
    [InlineData(CharacterVisibilityApi.Private, CharacterVisibility.Private)]
    public void MapCharacterVisibility_AllValues(CharacterVisibilityApi input, CharacterVisibility expected)
    {
        CreateCharacterRequestMapper.MapCharacterVisibility(input).ShouldBe(expected);
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
            SlotName = "Тестовый слот",
            SlotLimit = 5,
        };
        var info = CreateCharacterRequestMapper.ToCharacterTypeInfo(request);
        info.CharacterType.ShouldBe(CharacterType.Slot);
        info.SlotName.ShouldBe("Тестовый слот");
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
    [InlineData(CharacterVisibilityApi.Public, CharacterVisibility.Public)]
    [InlineData(CharacterVisibilityApi.PlayerHidden, CharacterVisibility.PlayerHidden)]
    [InlineData(CharacterVisibilityApi.Private, CharacterVisibility.Private)]
    public void ToCharacterTypeInfo_AllVisibilities_Mapped(CharacterVisibilityApi visibilityApi, CharacterVisibility expectedVisibility)
    {
        var request = new CreateCharacterRequest { CharacterVisibility = visibilityApi };
        var info = CreateCharacterRequestMapper.ToCharacterTypeInfo(request);
        info.CharacterVisibility.ShouldBe(expectedVisibility);
    }
}
