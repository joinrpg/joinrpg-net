using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class OrderingExtensions
  {
    public static IReadOnlyList<Character> GetOrderedCharacters(this CharacterGroup characterGroup) => characterGroup.GetCharactersContainer().OrderedItems;

    public static VirtualOrderContainer<Character> GetCharactersContainer(this CharacterGroup characterGroup)
      => VirtualOrderContainerFacade.Create(characterGroup.Characters, characterGroup.ChildCharactersOrdering);

    public static IReadOnlyList<CharacterGroup> GetOrderedChildGroups(this CharacterGroup characterGroup)
      => characterGroup.GetCharacterGroupsContainer().OrderedItems;

    public static VirtualOrderContainer<CharacterGroup> GetCharacterGroupsContainer(this CharacterGroup characterGroup)
      => VirtualOrderContainerFacade.Create(characterGroup.ChildGroups, characterGroup.ChildGroupsOrdering);

    public static IReadOnlyList<PlotElement> GetOrderedPlots(this Character character, ICollection<PlotElement> elements)
      => character.GetCharacterPlotContainer(elements).OrderedItems;

    public static VirtualOrderContainer<PlotElement> GetCharacterPlotContainer(this Character character,
      ICollection<PlotElement> plots) => VirtualOrderContainerFacade.Create(plots, character.PlotElementOrderData);

    public static IReadOnlyList<ProjectCharacterFieldDropdownValue> GetOrderedValues(this ProjectCharacterField field)
      => field.GetFieldValuesContainer().OrderedItems;

    public static VirtualOrderContainer<ProjectCharacterFieldDropdownValue> GetFieldValuesContainer(
      this ProjectCharacterField field)
      => VirtualOrderContainerFacade.Create(field.DropdownValues, field.ValuesOrdering);

    public static IReadOnlyList<ProjectCharacterField> GetActiveOrderedFields(this Project field)
  => field.GetFieldsContainer().GetOrderedItemsWithFilter(f => f.IsActive);

    public static IReadOnlyList<ProjectCharacterField> GetOrderedFields(this Project field)
      =>
        field.GetFieldsContainer()
          .OrderedItems;

    public static VirtualOrderContainer<ProjectCharacterField> GetFieldsContainer(
      this Project field)
      => VirtualOrderContainerFacade.Create(field.ProjectFields, field.ProjectFieldsOrdering);
  }
}
