using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test;

public class CharacterViewModeExtensionsTest
{
    private readonly MockedProject _mock = new();

    [Fact]
    public void GetViewModeForCharacter_AnonymousUser_DoesNotThrow()
    {
        // Анонимный посетитель: на варианте над EF-сущностью страница персонажа из-за этого падала (#4570).
        var viewMode = _mock.GetCharacterInfo(_mock.Character).GetViewModeForCharacter(null);

        viewMode.ShouldBe(ViewMode.Show);
    }

    [Fact]
    public void GetCharacterPlayerLinkViewModel_AnonymousUser_DoesNotThrow()
    {
        var character = _mock.GetCharacterInfo(_mock.Character);
        _ = Should.NotThrow(() => character.GetCharacterPlayerLinkViewModel(null));
    }
}
