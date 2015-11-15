using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Services.Impl.Allrpg
{
  internal class AllrpgProjectImporter
  {
    private readonly OperationLog _operationLog;

    public AllrpgProjectImporter(Project project, IUnitOfWork unitOfWork, OperationLog operationLog)
    {
      Project = project;
      _operationLog = operationLog;
      UnitOfWork = unitOfWork;
      
    }

    private Project Project { get; }

    private IUnitOfWork UnitOfWork { get; }

    private IDictionary<int, CharacterGroup> Locations { get; }= new Dictionary<int, CharacterGroup>();

    /// <summary>
    /// In allrpg we have roles(kolvo>1). In joinrpg it maps to CharacterGroup(HaveDirectSlots=true)
    /// </summary>
    private IDictionary<int, CharacterGroup> LocationsFromVacancies { get; } = new Dictionary<int, CharacterGroup>();

    private IDictionary<int, Character>  Characters { get; } = new Dictionary<int, Character>();

    private IDictionary<int, User> Users { get; } = new Dictionary<int, User>();

    private IDictionary<int, Claim> Claims { get; } = new Dictionary<int, Claim>();

    private IDictionary<int, Comment> Comments { get; } = new Dictionary<int, Comment>();

    private struct ParentRelation
    {
      public int ParentAllrpgId { get; }
      public int ChildAllrpgId { get; }

      public ParentRelation(int parentAllrpgId, int childAllrpgId)
      {
        ParentAllrpgId = parentAllrpgId;
        ChildAllrpgId = childAllrpgId;
      }
    }

    private ICollection<ParentRelation> LocationParentRelation { get; }= new List<ParentRelation>();

    public async Task Apply(ProjectReply projectReply)
    {
      foreach (var allrpgUser in projectReply.users)
      {
        await ImportUser(allrpgUser);
      }

      CleanProject();

      ImportLocations(projectReply.locations);

      ImportCharacters(projectReply);

      ImportClaims(projectReply.roles);

      ImportComments(projectReply.comments);

      _operationLog.Info("SUCCESS");
      await UnitOfWork.SaveChangesAsync();
      _operationLog.Info("DATA_SAVED");

      ReorderLocations(Project.RootGroup);
      await UnitOfWork.SaveChangesAsync();
      _operationLog.Info("DATA_REORDED_SAVED");
    }

    private void ReorderLocations(CharacterGroup @group)
    {
      group.ChildGroupsOrdering = string.Join(",", @group.ChildGroups.OrEmptyList().OrderBy(g => g.ChildGroupsOrdering).Select(g => g.CharacterGroupId));
      
      group.ChildCharactersOrdering = string.Join(",", @group.Characters.OrEmptyList().OrderBy(g => g.PlotElementOrderData).Select(c => c.CharacterId));
      foreach (var child in @group.ChildGroups.OrEmptyList())
      {
        ReorderLocations(child);
      }
      foreach (var character in @group.Characters.OrEmptyList())
      {
        character.PlotElementOrderData = null;
      }
    }

    private void ImportComments(ICollection<CommentData> comments)
    {
      foreach (var comment in comments)
      {
        ImportComment(comment);
      }

      UnitOfWork.GetDbSet<Comment>().AddRange(Comments.Values);
    }

    private void ImportComment(CommentData data)
    {
      Claim claim;
      if (!Claims.TryGetValue(data.role_id, out claim))
      {
        return;
      }
      var comment = new Comment
      {
        Project = Project,
        ProjectId = Project.ProjectId,
        Claim = claim,
        Author = Users[data.sid],
        CommentText = new MarkdownString(data.content.Replace("&lt;", "<").Replace("&gt;", ">").Replace("<br>", "\n")),
        LastEditTime = UnixTime.ToDateTime(data.date),
        CreatedTime = UnixTime.ToDateTime(data.date),
        IsVisibleToPlayer = data.type != 2,
        ParentCommentId = null,
        ChildsComments = new List<Comment>()
      };
      comment.IsCommentByPlayer = comment.Author == comment.Claim.Player;
      comment.Claim.Comments.Add(comment);
    }

    private void ImportClaims(ICollection<RoleData> roles)
    {
      foreach (var roleData in roles)
      {
        ImportClaim(roleData);
      }

      Project.Claims.AddLinkList(Claims.Values);
      UnitOfWork.GetDbSet<Claim>().AddRange(Claims.Values);
    }

    private void ImportClaim(RoleData roleData)
    {
      if (Claims.ContainsKey(roleData.id))
      {
        return;
      }

      var claim = new Claim
      {
        Project = Project,
        ProjectId = Project.ProjectId,
        Character = null, //see later
        Group = null, // see later
        Comments = new List<Comment>(),
        CreateDate = UnixTime.ToDateTime(roleData.datesent),
        Player = Users[roleData.sid],
        MasterDeclinedDate = roleData.todelete2 == 0 && roleData.status != 4 ? (DateTime?) null : UnixTime.ToDateTime(roleData.date),
        PlayerDeclinedDate = roleData.todelete == 0 ? (DateTime?) null : UnixTime.ToDateTime(roleData.date),
        PlayerAcceptedDate = UnixTime.ToDateTime(roleData.datesent)
      };

      bool canbeApproved = false;

      Character character;
      CharacterGroup characterGroup;
      if (Characters.TryGetValue(roleData.vacancy, out character))
      {
        claim.Character = character;
        canbeApproved = true;

      }
      else if (LocationsFromVacancies.TryGetValue(roleData.vacancy, out characterGroup))
      {
        claim.Group = characterGroup;
      }
      else if (Locations.TryGetValue(roleData.locat, out characterGroup))
      {
        claim.Group = characterGroup;
      }
      else
      {
        claim.Group = Project.RootGroup;
      }

      claim.MasterAcceptedDate = canbeApproved && roleData.status == 3 ? UnixTime.ToDateTime(roleData.date) : (DateTime?) null;

      Claims.Add(roleData.id, claim);
    }

    private void ImportCharacters(ProjectReply projectReply)
    {
      foreach (var vacancy in projectReply.vacancies.OrderByDescending(l => l.code)) // Poor man attempt to keep order
      {
        if (vacancy.kolvo == 1)
        {
          ImportCharacter(vacancy);
        }
        else
        {
          ImportLocation(vacancy);
        }
      }

      Project.Characters.AddLinkList(Characters.Values);
      UnitOfWork.GetDbSet<Character>().AddRange(Characters.Values);

      Project.CharacterGroups.AddLinkList(LocationsFromVacancies.Values);
      UnitOfWork.GetDbSet<CharacterGroup>().AddRange(LocationsFromVacancies.Values);
    }

    private void ImportCharacter(VacancyData vacancy)
    {
      if (Characters.ContainsKey(vacancy.id))
      {
        return;
      }

      var character = new Character()
      {
        Project = Project,
        ProjectId = Project.ProjectId,
        Description = new MarkdownString(vacancy.content),
        IsPublic = true,
        IsActive = true,
        CharacterName = vacancy.name,
        Claims = new List<Claim>(),
        IsAcceptingClaims = true,
        Groups = new List<CharacterGroup>() {GetGroupByAllrpgId(vacancy.locat)},
        PlotElementOrderData = "allrpg" + vacancy.code
      };

      Characters.Add(vacancy.id, character);
    }

    private void CleanProject()
    {
      UnitOfWork.GetDbSet<PlotElement>().RemoveRange(Project.PlotFolders.SelectMany(f => f.Elements).ToList());
      UnitOfWork.GetDbSet<PlotFolder>().RemoveRange(Project.PlotFolders.ToList());
      UnitOfWork.GetDbSet<Comment>().RemoveRange(Project.Claims.SelectMany(c => c.Comments).ToList());
      UnitOfWork.GetDbSet<Claim>().RemoveRange(Project.Claims.ToList());

      var characters = Project.Characters.ToList();
      _operationLog.Info($"PROJECT.CHARS to remove {characters.Count}");

      foreach (var character in characters)
      {
        character.Groups.CleanLinksList();
      }
      UnitOfWork.GetDbSet<Character>().RemoveRange(characters);

      _operationLog.Info($"PROJECT.CHARS remove finished");

      var characterGroups = Project.CharacterGroups.Where(cg => !cg.IsRoot).ToList();
      _operationLog.Info($"PROJECT.LOCATIONS to remove {characterGroups.Count}");

      foreach (var characterGroup in characterGroups)
      {
        characterGroup.ParentGroups.CleanLinksList();
      }
      UnitOfWork.GetDbSet<CharacterGroup>().RemoveRange(characterGroups);

      _operationLog.Info($"PROJECT.LOCATIONS remove finished");
    }

    private void ImportLocations(ICollection<LocationData> locationDatas)
    {
      foreach (var locationData in locationDatas) 
      {
        ImportLocation(locationData);
      }

      //Fixing parent-child relations
      foreach (var parentRelation in LocationParentRelation)
      {
        var child = Locations[parentRelation.ChildAllrpgId];
        var parent = GetGroupByAllrpgId(parentRelation.ParentAllrpgId);
        _operationLog.Info($"LOCATION.PARENT {parent.CharacterGroupName} of CHILD {child.CharacterGroupName}");
        child.ParentGroups.Add(parent);
      }

      var sortedLocations =
        Locations
          .Values
          .ToList();

      Project.CharacterGroups.AddLinkList(sortedLocations);
      UnitOfWork.GetDbSet<CharacterGroup>().AddRange(sortedLocations);
    }

    private CharacterGroup GetGroupByAllrpgId(int parentAllrpgId)
    {
      return parentAllrpgId == 0
        ? Project.RootGroup
        : Locations[parentAllrpgId];
    }


    private void ImportLocation(VacancyData locationData)
    {
      if (LocationsFromVacancies.ContainsKey(locationData.id))
      {
        return;
      }
      var characterGroup = new CharacterGroup()
      {
        AvaiableDirectSlots = locationData.kolvo,
        CharacterGroupName = locationData.name.Trim(),
        ProjectId = Project.ProjectId,
        Project = Project,
        IsRoot = false,
        Characters = new List<Character>(),
        ParentGroups = new List<CharacterGroup>() {GetGroupByAllrpgId(locationData.locat)},
        IsActive = true,
        HaveDirectSlots = true,
        IsPublic = true, 
        Description = new MarkdownString(locationData.content.Trim()),
        ChildGroupsOrdering = "allrpg!"+locationData.code
      };

      LocationsFromVacancies.Add(locationData.id, characterGroup);

      _operationLog.Info($"GROUP.CREATE FROM VACANCY {characterGroup}");
    }


    private void ImportLocation(LocationData locationData)
    {
      if (Locations.ContainsKey(locationData.id))
      {
        return;
      }
      var characterGroup = new CharacterGroup()
      {
        AvaiableDirectSlots = 0,
        CharacterGroupName = locationData.name.Trim(),
        ProjectId = Project.ProjectId,
        Project = Project,
        IsRoot = false,
        Characters = new List<Character>(),
        ParentGroups = new List<CharacterGroup>(),
        IsActive = true,
        HaveDirectSlots = false,
        IsPublic = locationData.rights == 0,
        Description = new MarkdownString(locationData.description.Trim()),
        ChildGroupsOrdering = "allrpg" + locationData.code
      };

      Locations.Add(locationData.id,characterGroup);

      _operationLog.Info($"GROUP.CREATE {characterGroup}");

      LocationParentRelation.Add(new ParentRelation(locationData.parent, locationData.id));
    }

    private async Task ImportUser(ProfileReply allrpgUser)
    {
      var usersRepository = UnitOfWork.GetUsersRepository();
      var user = await usersRepository.GetByAllRpgId(allrpgUser.sid);
      if (user != null)
      {
        _operationLog.Info("USER.FOUND: " + user);
        Users.Add(allrpgUser.sid, user);
        return;
      }

      user = await usersRepository.GetByEmail(allrpgUser.em) ?? await usersRepository.GetByEmail(allrpgUser.em2);
      if (user == null)
      {
        user = new User {Email = new[] {allrpgUser.em, allrpgUser.em2}.WhereNotNullOrWhiteSpace().First() };
        user.UserName = user.Email;
        _operationLog.Info($"USER.CREATE email={user.Email}");
        UnitOfWork.GetDbSet<User>().Add(user);
      }
      else
      {
        _operationLog.Info($"USER.UPDATE user={user}");
      }
      Users.Add(allrpgUser.sid, user);
      user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
      user.Allrpg.Sid = allrpgUser.sid;
      AllrpgImportUtilities.ImportUserFromResult(user, allrpgUser);

      _operationLog.Info($"USER.RESULT user={user}");
    }
  }


}
