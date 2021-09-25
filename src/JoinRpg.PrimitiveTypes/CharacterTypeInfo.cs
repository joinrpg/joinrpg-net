using System;

namespace JoinRpg.PrimitiveTypes
{
    public record CharacterTypeInfo
    {
        public CharacterType CharacterType { get; }
        public bool IsHot { get; }

        public bool IsAcceptingClaims => CharacterType != CharacterType.NonPlayer;

        public CharacterTypeInfo(CharacterType CharacterType, bool IsHot = false)
        {
            this.CharacterType = CharacterType;
            if (IsHot && CharacterType == CharacterType.NonPlayer)
            {
                throw new ArgumentException("NPCs are not hot", nameof(IsHot));
            }

            this.IsHot = IsHot;
        }

        public void Deconstruct(out CharacterType characterType, out bool isHot, out bool isAcceptingClaims)
        {
            characterType = CharacterType;
            isHot = IsHot;
            isAcceptingClaims = CharacterType != CharacterType.NonPlayer;
        }

        public void Deconstruct(out CharacterType characterType, out bool isHot)
        {
            characterType = CharacterType;
            isHot = IsHot;
        }
    }

    public enum CharacterType
    {
        Player,
        NonPlayer,
    }
}
