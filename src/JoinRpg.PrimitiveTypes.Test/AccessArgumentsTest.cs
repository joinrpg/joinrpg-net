using JoinRpg.PrimitiveTypes.Access;

namespace JoinRpg.PrimitiveTypes.Test;

public class AccessArgumentsTest
{
    // Базовый хелпер: все флаги false
    private static AccessArguments None() => AccessArguments.None;

    [Fact]
    public void CanViewCharacterAtAll_PublicCharacter_Visible()
    {
        var args = None() with { CharacterPublic = true };
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void CanViewCharacterAtAll_MasterAccess_Visible()
    {
        var args = None() with { MasterAccess = true };
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void CanViewCharacterAtAll_PlayerAccessToCharacter_Visible()
    {
        var args = None() with { PlayerAccessToCharacter = true };
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void CanViewCharacterAtAll_Published_Visible()
    {
        var args = None() with { Published = true };
        args.CanViewCharacterAtAll.ShouldBeTrue();
    }

    [Fact]
    public void CanViewCharacterAtAll_NoAccess_NotVisible()
    {
        // Скрытый персонаж без каких-либо прав — не должен быть виден
        None().CanViewCharacterAtAll.ShouldBeFalse();
    }
}
