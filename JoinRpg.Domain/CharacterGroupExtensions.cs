using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class CharacterGroupExtensions
  {
    private abstract class CharacterGroupContainerBase
    {
      protected CharacterGroupContainerBase(CharacterGroup self)
      {
        Self = self;
      }

      protected CharacterGroup Self { get; }
    }

    private class CharacterGroupIsCharacterContainer : CharacterGroupContainerBase, IParentEntity<Character>
    {
      public CharacterGroupIsCharacterContainer(CharacterGroup self) : base(self) {}

      public ICollection<Character> Childs => Self.Characters;

      public string Ordering
      {
        get { return Self.ChildCharactersOrdering; }
        set { Self.ChildCharactersOrdering = value; }
      }
    }

    private class CharacterGroupIsCharacterGroupContainer : CharacterGroupContainerBase, IParentEntity<CharacterGroup>
    {
      public CharacterGroupIsCharacterGroupContainer(CharacterGroup self) : base(self)
      { }

      public ICollection<CharacterGroup> Childs => Self.ChildGroups;

      public string Ordering
      {
        get { return Self.ChildGroupsOrdering; }
        set { Self.ChildGroupsOrdering = value; }
      }
    }

    public static IReadOnlyList<Character> GetOrderedCharacters(this CharacterGroup characterGroup)
    {
      return new CharacterGroupIsCharacterContainer(characterGroup).GetOrderedChilds();
    }

    public static IReadOnlyList<CharacterGroup> GetOrderedChildGroups(this CharacterGroup characterGroup)
    {
      return new CharacterGroupIsCharacterGroupContainer(characterGroup).GetOrderedChilds();
    }
  }
}
