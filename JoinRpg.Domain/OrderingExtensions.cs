using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class OrderingExtensions
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

      public IEnumerable<Character> Childs => Self.Characters;

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

      public IEnumerable<CharacterGroup> Childs => Self.ChildGroups;

      public string Ordering
      {
        get { return Self.ChildGroupsOrdering; }
        set { Self.ChildGroupsOrdering = value; }
      }
    }

    private class CharacterIsPlotElementContainer : IParentEntity<PlotElement>
    {
      private Character character;
      private ICollection<PlotElement> plots;

      public CharacterIsPlotElementContainer(Character character, ICollection<PlotElement> plots)
      {
        this.character = character;
        this.plots = plots;
      }

      public IEnumerable<PlotElement> Childs => plots;

      public string Ordering
      {
        get { return character.PlotElementOrderData; }
        set { character.PlotElementOrderData = value; }
      }
    }

    private static VirtualOrderContainer<TChild> GetContainer<TChild>(IParentEntity<TChild> characterSource) where TChild : class, IOrderableEntity
    {
      return new VirtualOrderContainer<TChild>(characterSource.Ordering, characterSource.Childs);
    }

    public static IReadOnlyList<Character> GetOrderedCharacters(this CharacterGroup characterGroup)
    {
      return characterGroup.GetCharactersContainer().OrderedItems;
    }

    public static VirtualOrderContainer<Character> GetCharactersContainer(this CharacterGroup characterGroup)
    {
      return GetContainer(new CharacterGroupIsCharacterContainer(characterGroup));
    }

    public static IReadOnlyList<CharacterGroup> GetOrderedChildGroups(this CharacterGroup characterGroup)
    {
      return characterGroup.GetCharacterGroupsContainer().OrderedItems;
    }

    public static VirtualOrderContainer<CharacterGroup> GetCharacterGroupsContainer(this CharacterGroup characterGroup)
    {
      return GetContainer(new CharacterGroupIsCharacterGroupContainer(characterGroup));
    }

    public static IReadOnlyList<PlotElement> GetOrderedPlots(this Character character, ICollection<PlotElement> elements)
    {
      return character.GetCharacterPlotContainer(elements).OrderedItems;
    }

    public static VirtualOrderContainer<PlotElement> GetCharacterPlotContainer(this Character character,
      ICollection<PlotElement> plots)
    {
      return GetContainer(new CharacterIsPlotElementContainer(character, plots));
    }
  }
}
