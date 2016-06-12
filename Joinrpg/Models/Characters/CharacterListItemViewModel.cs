using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models
{
  public enum CharacterBusyStatusView
  {
    [Display(Name = "Занят")]
    HasPlayer,
    [Display(Name = "Обсуждается")]
    Discussed,
    [Display(Name = "Нет заявок")]
    NotSend,
    [Display(Name = "NPC")]
    Npc,
  }

  public class CharacterListViewModel : IOperationsAwareView
  {
    public IEnumerable<CharacterListItemViewModel> Items { get; }
    public int? ProjectId { get; }
    public IReadOnlyCollection<int> ClaimIds { get; }
    public IReadOnlyCollection<int> CharacterIds { get; }
    public string ProjectName { get; }
    public string Title { get; }

    public CharacterListViewModel(int currentUserId, string title, IReadOnlyCollection<Character> characters,
      IReadOnlyCollection<PlotFolder> plots, Project project)
      : this (currentUserId, title, characters, plots, project, vm => true)
    {
      
    }

    public CharacterListViewModel(int currentUserId, string title, IReadOnlyCollection<Character> characters,
      IReadOnlyCollection<PlotFolder> plots, Project project, Func<CharacterListItemViewModel, bool> viewPredicate)
    {
      var selectMany = plots.SelectMany(p => p.Elements).ToArray();

      var viewModel =
        characters.Select(
          character =>
            new CharacterListItemViewModel(character, currentUserId, character.GetProblems(),
              character.SelectPlots(selectMany)));

      Items = viewModel.Where(viewPredicate).ToArray();
      ProjectName = project.ProjectName;
      ProjectId = project.ProjectId;
      Title = title;
      Fields = project.GetOrderedFields().Where(f => f.IsActive && AnyItemHasValue(f.ProjectFieldId)).ToArray();
      ClaimIds = characters.Select(c => c.ApprovedClaim?.ClaimId).WhereNotNull().ToArray();
      CharacterIds = characters.Select(c => c.CharacterId).ToArray();
    }

    private bool AnyItemHasValue(int projectFieldId)
    {
      return Items.Select(i => i.Fields.FieldById(projectFieldId)).Any(f1 => f1?.HasValue == true);
    }

    public IReadOnlyCollection<ProjectField> Fields { get; }
  }

  public class CharacterListItemViewModel
  {
    [Display(Name="Занят?")]
    public CharacterBusyStatusView BusyStatus { get; }

    [Display(Name = "Персонаж")]
    public string Name { get; set; }

    public int CharacterId { get; }

    [NotNull, ReadOnly(true)]
    public CustomFieldsViewModel Fields { get; }

    public int? ApprovedClaimId { get; }

    [Display(Name = "Игрок"), CanBeNull]
    public User Player { get; set; }

    public int IndReadyPlotsCount { get; }
    public int IndAllPlotsCount { get; }
    public int ColReadyPlotsCount { get; }
    public int ColAllPlotsCount{ get; }

    [ReadOnly(true), DisplayName("Входит в группы")]
    public CharacterParentGroupsViewModel Groups { get; }

    public CharacterListItemViewModel ([NotNull] Character character, int currentUserId, IEnumerable<ClaimProblem> problems, IReadOnlyCollection<PlotElement> plots)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));

      
      if (character.ApprovedClaim != null)
      {
        BusyStatus = CharacterBusyStatusView.HasPlayer;
        ApprovedClaimId = character.ApprovedClaim.ClaimId;
        Player = character.ApprovedClaim.Player;
      }
      else if (character.Claims.Any(c => c.IsActive))
      {
        BusyStatus = CharacterBusyStatusView.Discussed;
      }
      else if (character.IsAcceptingClaims)
      {
        BusyStatus = CharacterBusyStatusView.NotSend;
      }
      else
      {
        BusyStatus = CharacterBusyStatusView.Npc;
      }
      Name = character.CharacterName;
      CharacterId = character.CharacterId;
      Fields = new CustomFieldsViewModel(currentUserId, character, disableEdit: true); //This disable edit will speed up some requests.
      Problems = problems.Select(p => new ProblemViewModel(p)).ToList();

      IndReadyPlotsCount = plots.Count(p => p.IsCompleted && p.TargetCharacters.Select(c => c.CharacterId).Contains(character.CharacterId));
      IndAllPlotsCount = plots.Count(p => p.IsActive && p.TargetCharacters.Select(c => c.CharacterId).Contains(character.CharacterId));
      ColReadyPlotsCount = plots.Count(p => p.IsCompleted && !p.TargetCharacters.Select(c => c.CharacterId).Contains(character.CharacterId));
      ColAllPlotsCount = plots.Count(p => p.IsActive && !p.TargetCharacters.Select(c => c.CharacterId).Contains(character.CharacterId));
      Groups = new CharacterParentGroupsViewModel(character, character.HasMasterAccess(currentUserId));
    }

    [Display(Name="Проблемы")]
    public ICollection<ProblemViewModel> Problems { get; set; }
  }
}