using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.CharacterGroups;

namespace JoinRpg.Web.Models.Characters
{
  public class CharacterListByGroupViewModel : CharacterListViewModel
  {
    public CharacterListByGroupViewModel(int currentUserId, IReadOnlyCollection<Character> characters, CharacterGroup group)
      : base(currentUserId, $"Персонажи — {group.CharacterGroupName}", characters, group.Project)
    {
      GroupModel = new CharacterGroupDetailsViewModel(group, currentUserId, GroupNavigationPage.Characters);
    }

    public CharacterGroupDetailsViewModel GroupModel { get; }
  }

  public class CharacterListViewModel : IOperationsAwareView
  {
    public IEnumerable<CharacterListItemViewModel> Items { get; }
    public int? ProjectId { get; }
    public IReadOnlyCollection<int> ClaimIds { get; }
    public IReadOnlyCollection<int> CharacterIds { get; }
    public string ProjectName { get; }
    public string Title { get; }

    public bool HasEditAccess { get; }

    public CharacterListViewModel(
      int currentUserId, 
      string title, 
      IReadOnlyCollection<Character> characters, 
      Project project)
    {
      Items = characters.Select(
        character =>
          new CharacterListItemViewModel(character, currentUserId, character.GetProblems())).ToArray();
      ProjectName = project.ProjectName;
      ProjectId = project.ProjectId;
      Title = title;
      Fields = project.GetOrderedFields().Where(f => f.IsActive).ToArray();
      ClaimIds = characters.Select(c => c.ApprovedClaim?.ClaimId).WhereNotNull().ToArray();
      CharacterIds = characters.Select(c => c.CharacterId).ToArray();
      HasEditAccess = project.HasEditRolesAccess(currentUserId);
    }

    public IReadOnlyCollection<ProjectField> Fields { get; }
  }

  public class CharacterListItemViewModel : ILinkable
  {
    [Display(Name="Занят?")]
    public CharacterBusyStatusView BusyStatus { get; }

    [Display(Name = "Персонаж")]
    public string Name { get; set; }

    public int CharacterId { get; }

    [NotNull, ReadOnly(true)]
    public IReadOnlyCollection<FieldWithValue> Fields { get; }

    public int? ApprovedClaimId { get; }

    [Display(Name = "Игрок"), CanBeNull]
    public User Player { get; set; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel Groups { get; }

    public CharacterListItemViewModel (
      [NotNull] Character character, 
      int currentUserId, 
      IEnumerable<ClaimProblem> problems)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));

      BusyStatus = character.GetBusyStatus();

      if (character.ApprovedClaim != null)
      {
        ApprovedClaimId = character.ApprovedClaim.ClaimId;
        Player = character.ApprovedClaim.Player;
      }
      Name = character.CharacterName;
      CharacterId = character.CharacterId;
      ProjectId = character.ProjectId;
      Fields = character.Project.GetFieldsNotFilledWithoutOrder().ToList();
      Fields.FillFrom(character);
      Fields.FillFrom(character.ApprovedClaim);
      Problems = problems.Select(p => new ProblemViewModel(p)).ToList();

      Groups = new CharacterParentGroupsViewModel(character, character.HasMasterAccess(currentUserId));
    }

    [Display(Name="Проблемы")]
    public ICollection<ProblemViewModel> Problems { get; set; }

    #region Implementation of ILinkable

    public LinkType LinkType => LinkType.ResultCharacter;
    public string Identification => CharacterId.ToString();
    public int? ProjectId { get; }

    #endregion
  }
}