using System;

namespace JoinRpg.PrimitiveTypes
{
    public record CharacterTypeInfo
    {
        public CharacterType CharacterType { get; }

        public bool IsHot { get; }

        public bool IsAcceptingClaims => CharacterType != CharacterType.NonPlayer;

        public int? SlotLimit { get; }

        public CharacterTypeInfo(CharacterType CharacterType, bool IsHot, int? SlotLimit)
        {
            this.CharacterType = CharacterType;
            if (IsHot && CharacterType == CharacterType.NonPlayer)
            {
                throw new ArgumentException("NPCs are not hot", nameof(IsHot));
            }

            if (SlotLimit is not null && CharacterType != CharacterType.Slot)
            {
                throw new ArgumentException("Not-slots could not be limited", nameof(SlotLimit));
            }

            this.IsHot = IsHot;
            this.SlotLimit = SlotLimit;
        }

        public static CharacterTypeInfo Default() => new(CharacterType.Player, false, null);

        public void Deconstruct(out CharacterType characterType, out bool isHot, out int? slotLimit, out bool isAcceptingClaims)
        {
            characterType = CharacterType;
            isHot = IsHot;
            slotLimit = SlotLimit;
            isAcceptingClaims = CharacterType != CharacterType.NonPlayer;
        }

        public void Deconstruct(out CharacterType characterType, out bool isHot, out int? slotLimit)
        {
            characterType = CharacterType;
            isHot = IsHot;
            slotLimit = SlotLimit;
        }
    }

    public enum CharacterType
    {
        Player,
        NonPlayer,
        Slot
    }
}
