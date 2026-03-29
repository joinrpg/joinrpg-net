using JoinRpg.PrimitiveTypes;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.Portal.Controllers.XGameApi;

public static class CreateCharacterRequestMapper
{
    public static CharacterTypeInfo ToCharacterTypeInfo(CreateCharacterRequest request)
    {
        var characterType = MapCharacterType(request.CharacterType);
        var characterVisibility = MapCharacterVisibility(request.CharacterVisibility);
        return new CharacterTypeInfo(characterType, request.IsHot, request.SlotLimit, request.SlotName, characterVisibility);
    }

    public static CharacterType MapCharacterType(CharacterTypeApi type) => type switch
    {
        CharacterTypeApi.Player => CharacterType.Player,
        CharacterTypeApi.NonPlayer => CharacterType.NonPlayer,
        CharacterTypeApi.Slot => CharacterType.Slot,
        _ => throw new ArgumentException($"Unknown CharacterTypeApi value: {type}", nameof(type)),
    };

    public static CharacterVisibility MapCharacterVisibility(CharacterVisibilityApi visibility) => visibility switch
    {
        CharacterVisibilityApi.Public => CharacterVisibility.Public,
        CharacterVisibilityApi.PlayerHidden => CharacterVisibility.PlayerHidden,
        CharacterVisibilityApi.Private => CharacterVisibility.Private,
        _ => throw new ArgumentException($"Unknown CharacterVisibilityApi value: {visibility}", nameof(visibility)),
    };
}
