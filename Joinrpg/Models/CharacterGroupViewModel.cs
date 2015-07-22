using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Models
{

  public class CharacterGroupListViewModel
  {
    public int ProjectId
    { get; set; }

    public string ProjectName { get; set; }

    public IList<CharacterGroupViewModel> Groups { get; set; }

    public static CharacterGroupListViewModel FromGroup(CharacterGroup group)
    {
      return new CharacterGroupListViewModel()
      {
        ProjectId = group.ProjectId,
        ProjectName = group.Project.ProjectName,
        Groups = new CharacterGroupHierarchyBuilder(group).Generate()
      };
    }


    private class CharacterGroupHierarchyBuilder
    {
      private CharacterGroup Root
      { get; }

      private IList<int> AlreadyOutputedGroups { get; } = new List<int>();
      private IList<int> AlreadyOutputedChars { get; } = new List<int>();

      private IList<CharacterGroupViewModel> Results
      { get; }
      = new List<CharacterGroupViewModel>();

      public CharacterGroupHierarchyBuilder(CharacterGroup root)
      {
        Root = root;
      }

      public IList<CharacterGroupViewModel> Generate()
      {
        GenerateFrom(Root, 0);
        return Results;
      }

      private void GenerateFrom(CharacterGroup characterGroup, int deepLevel)
      {
        var vm = new CharacterGroupViewModel()
        {
          CharacterGroupId = characterGroup.CharacterGroupId,
          CanDelete = !characterGroup.IsRoot,
          DeepLevel = deepLevel,
          Name = characterGroup.CharacterGroupName,
          FirstCopy = !AlreadyOutputedGroups.Contains(characterGroup.CharacterGroupId),
          AvaiableDirectSlots = characterGroup.AvaiableDirectSlots,
          Characters = characterGroup.Characters.Select(GenerateCharacter).ToList(),
          Description = characterGroup.Description.ToHtmlString()
        };
        Results.Add(vm);

        if (!vm.FirstCopy)
          return;

        AlreadyOutputedGroups.Add(characterGroup.CharacterGroupId);

        foreach (var childGroup in characterGroup.ChildGroups)
        {
          GenerateFrom(childGroup, deepLevel + 1);
        }
      }

      private CharacterViewModel GenerateCharacter(Character arg)
      {
        var vm = new CharacterViewModel()
        {
          CharacterId = arg.CharacterId,
          CharacterName = arg.CharacterName,
          IsFirstCopy = !AlreadyOutputedChars.Contains(arg.CharacterId),
          IsAcceptingClaims = arg.IsAcceptingClaims,
          Description =  arg.Description.ToHtmlString()
        };
        if (vm.IsFirstCopy)
        {
          AlreadyOutputedChars.Add(vm.CharacterId);
        }
        return vm;
      }
    }
  }

  public class CharacterViewModel
  {
    public int CharacterId { get; set; }
    public string CharacterName { get; set; }

    public bool IsFirstCopy { get; set; }

    public bool IsAcceptingClaims { get; set; }

    public HtmlString Description { get; set; }
  }

  public class CharacterGroupViewModel
  {
    public int CharacterGroupId { get; set; }

    [DisplayName("Название локации (группы)")]
    public string Name { get; set; }

    public int DeepLevel { get; set; }

    public bool CanDelete { get; set; }

    public bool FirstCopy { get; set; }

    [DisplayName("Слотов в локации")]
    public int AvaiableDirectSlots { get; set; }

    public IList<CharacterViewModel> Characters { get; set; }

    public HtmlString Description { get; set; }
  }

}
