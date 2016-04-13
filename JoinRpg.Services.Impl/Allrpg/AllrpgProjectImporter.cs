using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
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

    private IDictionary<int, CharacterGroup> Locations { get; } = new Dictionary<int, CharacterGroup>();

    /// <summary>
    /// In allrpg we have roles(kolvo>1). In joinrpg it maps to CharacterGroup(HaveDirectSlots=true)
    /// </summary>
    private IDictionary<int, CharacterGroup> LocationsFromVacancies { get; } = new Dictionary<int, CharacterGroup>();

    private IDictionary<int, Character> Characters { get; } = new Dictionary<int, Character>();

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

    private ICollection<ParentRelation> LocationParentRelation { get; } = new List<ParentRelation>();

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
      await SaveToDatabase();
      _operationLog.Info("DATA_SAVED");

      ReorderLocations(Project.RootGroup);
      await SaveToDatabase();
      _operationLog.Info("DATA_REORDED_SAVED");
    }

    private async Task SaveToDatabase()
    {
      try
      {
        await UnitOfWork.SaveChangesAsync();
      }
      catch (DbEntityValidationException e)
      {
        foreach (var failure in e.EntityValidationErrors)
        {
          _operationLog.Error($"Validation failed: {failure.Entry.Entity} ");
          foreach (var error in failure.ValidationErrors)
          {
            _operationLog.Error($"- {error.PropertyName} : {error.ErrorMessage}");
          }
        }
        throw;
      }
    }

    private void ReorderLocations(CharacterGroup @group)
    {
      group.ChildGroupsOrdering = string.Join(",",
        @group.ChildGroups.OrEmptyList().OrderBy(g => g.ChildGroupsOrdering).Select(g => g.CharacterGroupId));

      group.ChildCharactersOrdering = string.Join(",",
        @group.Characters.OrEmptyList().OrderBy(g => g.PlotElementOrderData).Select(c => c.CharacterId));
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
      };
      comment.IsCommentByPlayer = comment.Author == comment.Claim.Player;
      comment.Claim.Comments.Add(comment);
      comment.Claim.LastUpdateDateTime = new DateTime[] {comment.Claim.LastUpdateDateTime, comment.CreatedTime}.Max();
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
        Comments = new List<Comment>()
        {
          new Comment()
          {
            Author = Project.ProjectAcls.Single(acl => acl.IsOwner).User, 
            CommentText = new MarkdownString($"<a href=\"http://site.allrpg.info/orders/orders/{roleData.id}/act=view&site={Project.Details.AllrpgId}\">Заявка в allrpg</a>"),
            CreatedTime = UnixTime.ToDateTime(roleData.datesent),
            IsCommentByPlayer = false,
            IsVisibleToPlayer = false,
            LastEditTime = UnixTime.ToDateTime(roleData.datesent),
            ParentCommentId = null,
            Project = Project,
          }
        },
        CreateDate = UnixTime.ToDateTime(roleData.datesent),
        Player = Users[roleData.sid],
        MasterDeclinedDate =
          roleData.todelete2 == 0 && roleData.status != 4 ? (DateTime?) null : UnixTime.ToDateTime(roleData.date),
        PlayerDeclinedDate = roleData.todelete == 0 ? (DateTime?) null : UnixTime.ToDateTime(roleData.date),
        PlayerAcceptedDate = UnixTime.ToDateTime(roleData.datesent),
        LastUpdateDateTime = UnixTime.ToDateTime(roleData.date)
      };

      foreach (var virtualField in roleData.@virtual)
      {
        if (virtualField.Key == 7152) //Known steam2016 "responsible master"
        {
          int responsibleMasterIdx;
          if (int.TryParse(virtualField.Value, out responsibleMasterIdx))
          {
            var responsibleSid = Steam2016ResponsibleMasters[responsibleMasterIdx];
            claim.ResponsibleMasterUser = responsibleSid == null ? null : Users[(int) responsibleSid];
          }
        } else if (ConvertToCommentVirtualFields.Contains(virtualField.Key) && !string.IsNullOrWhiteSpace(virtualField.Value))
        {
          claim.Comments.Add(new Comment()
          {
            Author = Users[roleData.sid],
            CommentText = new MarkdownString(virtualField.Value),
            CreatedTime = claim.CreateDate,
            IsCommentByPlayer = true,
            IsVisibleToPlayer = true,
            LastEditTime = DateTime.UtcNow,
            ParentCommentId = null,
            Project = Project,
          });
        }
      }

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

      claim.MasterAcceptedDate = canbeApproved && roleData.status == 3
        ? UnixTime.ToDateTime(roleData.date)
        : (DateTime?) null;

      claim.ClaimStatus = ConvertAllrpgStatus(roleData, canbeApproved);

      Claims.Add(roleData.id, claim);
    }

    private Claim.Status ConvertAllrpgStatus(RoleData roleData, bool canbeApproved)
    {
      if (roleData.todelete == 1)
      {
        return Claim.Status.DeclinedByUser;
      }
      if (roleData.todelete2 == 1 || roleData.status == 4)
      {
        return Claim.Status.DeclinedByMaster;
      }
      if (roleData.status == 3 && canbeApproved)
      {
        return Claim.Status.Approved;
      }
      return Claim.Status.AddedByUser;
    }

    /// <summary>
    /// Allrpg virtual fields that should be converted to comments.
    /// </summary>
    private IReadOnlyList<int> ConvertToCommentVirtualFields { get; } = new [] { 7126};

    private IReadOnlyDictionary<int, int?> Steam2016ResponsibleMasters { get; } = new Dictionary<int, int?>()
    {
      {1, 8270}, //Enno
      {2,11536 }, //Teoni
      {3,4400 }, //Nadya Sertakova
      {4, 8881 }, //Asya
      {5, 14636 }, //Veter
      {6, 16484 }, //Astaya
      {7, 2233 }, //Warpo
      {8, 11479 }, //Freexee
      {9, 4503 }, //Ksiontes
      {10, 13506 }, //Ranma
      {11, 7536 }, //Martell
      {12,  null}, //Yusja
      {13, null }, //Weddika
      {14, 339 }, //Kubela
      {15, 8834 }, //DonnaAnna
      {16, 1490 }, //Myfa
      {17, null }, //Not defined
      {18, 1298 } //Atana
    };

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
        IsAcceptingClaims = true,
        Groups = new List<CharacterGroup>() {GetGroupByAllrpgId(vacancy.locat)},
        PlotElementOrderData = $"allrpg{vacancy.code:00000}"
      };

      Characters.Add(vacancy.id, character);
    }

    private void CleanProject()
    {
      UnitOfWork.GetDbSet<FinanceOperation>()
        .RemoveRange(Project.Claims.SelectMany(c => c.Comments).Select(c => c.Finance).WhereNotNull());
      UnitOfWork.GetDbSet<PlotElement>().RemoveRange(Project.PlotFolders.SelectMany(f => f.Elements).ToList());
      UnitOfWork.GetDbSet<PlotFolder>().RemoveRange(Project.PlotFolders.ToList());
      UnitOfWork.GetDbSet<ReadCommentWatermark>()
        .RemoveRange(Project.Claims.SelectMany(c => c.Watermarks));
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

    [NotNull]
    private CharacterGroup GetGroupByAllrpgId(int parentAllrpgId)
    {
      if (parentAllrpgId == 0) return Project.RootGroup;
      if (Locations.ContainsKey(parentAllrpgId))
      {
        return Locations[parentAllrpgId];
      }
      _operationLog.Info($"LOCATION NOT FOUND ALLRPG_ID = {parentAllrpgId}");
      return Project.RootGroup;
    }

    private void ImportLocation(VacancyData locationData)
    {
      if (LocationsFromVacancies.ContainsKey(locationData.id))
      {
        return;
      }
      _operationLog.Info($"GROUP FROM VACANCY {locationData}");
      var characterGroup = new CharacterGroup()
      {
        AvaiableDirectSlots = locationData.kolvo,
        CharacterGroupName = locationData.name.Trim(),
        ProjectId = Project.ProjectId,
        Project = Project,
        IsRoot = false,
        ParentGroups = new List<CharacterGroup>() {GetGroupByAllrpgId(locationData.locat)},
        IsActive = true,
        HaveDirectSlots = true,
        IsPublic = true, 
        Description = new MarkdownString(locationData.content.Trim()),
        ChildGroupsOrdering = $"allrpg!{locationData.code:00000}"
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
        ParentGroups = new List<CharacterGroup>(),
        IsActive = true,
        HaveDirectSlots = false,
        IsPublic = locationData.rights == 0,
        Description = new MarkdownString(locationData.description.Trim()),
        ChildGroupsOrdering = $"allrpg!{locationData.code:00000}"
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
        _operationLog.Info($"USER.FOUND: Id = {user.Id}, Email = {user.Email}, Allrpg = {user.Allrpg.Sid}");
        Users.Add(allrpgUser.sid, user);
        return;
      }

      var email = allrpgUser.em.ToLowerInvariant();
      var email2 = allrpgUser.em2.ToLowerInvariant();
      user = await usersRepository.GetByEmail(email) ?? await usersRepository.GetByEmail(email2);

      string action;
      if (user == null)
      {
        var username = new[] {email, email2}.WhereNotNullOrWhiteSpace().First();
        user = new User
        {
          Email = username,
          UserName = username
        };
        UnitOfWork.GetDbSet<User>().Add(user);
        action = "CREATE";
      }
      else
      {
        action = "UPDATE";
      }
      Users.Add(allrpgUser.sid, user);
      user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
      user.Allrpg.Sid = allrpgUser.sid;
      AllrpgImportUtilities.ImportUserFromResult(user, allrpgUser);

      _operationLog.Info($"USER.{action} Id = {user.Id}, Email = {user.Email}, Allrpg = {user.Allrpg.Sid}");
    }
  }


}
