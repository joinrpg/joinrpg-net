using JoinRpg.Helpers;

namespace JoinRpg.Domain;

public static class OrderingExtensions
{
    public static IReadOnlyList<Character> GetOrderedCharacters(this CharacterGroup characterGroup) => characterGroup.GetCharactersContainer().OrderedItems;

    public static VirtualOrderContainer<Character> GetCharactersContainer(this CharacterGroup characterGroup)
      => VirtualOrderContainerFacade.Create(characterGroup.Characters, characterGroup.ChildCharactersOrdering);

    public static IReadOnlyList<CharacterGroup> GetOrderedChildGroups(this CharacterGroup characterGroup)
      => characterGroup.GetCharacterGroupsContainer().OrderedItems;

    public static VirtualOrderContainer<CharacterGroup> GetCharacterGroupsContainer(this CharacterGroup characterGroup)
      => VirtualOrderContainerFacade.Create(characterGroup.ChildGroups, characterGroup.ChildGroupsOrdering);

    public static VirtualOrderContainer<PlotElement> GetCharacterPlotContainer(this Character character,
      IReadOnlyCollection<PlotElement> plots) => VirtualOrderContainerFacade.Create(plots.OrderBy(pe => pe.PlotFolderId), character.PlotElementOrderData, preserveOrder: true);

    public static VirtualOrderContainer<ProjectFieldDropdownValue> GetFieldValuesContainer(
      this ProjectField field)
      => VirtualOrderContainerFacade.Create(field.DropdownValues, field.ValuesOrdering);

    public static VirtualOrderContainer<ProjectField> GetFieldsContainer(
      this Project field)
      => VirtualOrderContainerFacade.Create(field.ProjectFields, field.Details.FieldsOrdering);

    public static VirtualOrderContainer<PlotFolder> GetPlotFoldersContainer(this Project field)
        => VirtualOrderContainerFacade.Create(field.PlotFolders, field.Details.PlotFoldersOrdering);
}
