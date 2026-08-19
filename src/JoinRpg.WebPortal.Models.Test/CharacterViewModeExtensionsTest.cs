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
        // Регрессия для бага 4570: implicit operator int(UserIdentification) разыменовывал
        // null при передаче UserIdentification? в HasAnyAccess(int?), что роняло страницу
        // персонажа для неавторизованных пользователей с NullReferenceException.
        var viewMode = _mock.Character.GetViewModeForCharacter(null);

        viewMode.ShouldBe(ViewMode.Show);
    }

    [Fact]
    public void GetCharacterPlayerLinkViewModel_AnonymousUser_DoesNotThrow()
    {
        _ = Should.NotThrow(() => _mock.Character.GetCharacterPlayerLinkViewModel(null));
    }
}
