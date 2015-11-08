using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Dal.Impl;
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

    private struct ParentRelation
    {
      public int ParentAllrpgId;
      public int ChildAllrpgId;
    }

    private ICollection<ParentRelation> LocationParentRelation { get; }= new List<ParentRelation>();

    public async Task Apply(AllrpgApi.Reply<ProjectReply> reply)
    {
      foreach (var allrpgUser in reply.Result.users)
      {
        await ImportUser(allrpgUser);
      }

      _operationLog.Info($"PROJECT.LOCATIONS to remove {Project.CharacterGroups.Count}");
      var characterGroups = Project.CharacterGroups.Where(cg => !cg.IsRoot).ToList(); 
      
      foreach (var characterGroup in characterGroups)
      {
        characterGroup.ParentGroups.CleanLinksList();
        UnitOfWork.GetDbSet<CharacterGroup>().Remove(characterGroup);
      }

      foreach (var locationData in reply.Result.locations.OrderByDescending(l => l.code)) // Poor man attempt to keep order
      { 
        ImportLocation(locationData);
      }

      foreach (var parentRelation in LocationParentRelation)
      {
        var child = Locations[parentRelation.ChildAllrpgId];
        var parent = parentRelation.ParentAllrpgId == 0
          ? Project.RootGroup
          : Locations[parentRelation.ParentAllrpgId];
        _operationLog.Info($"LOCATION.PARENT {parent.CharacterGroupName} of CHILD {child.CharacterGroupName}");
        child.ParentGroups.Add(parent);
      }
      
      Project.CharacterGroups.AddLinkList(Locations.Values);
      UnitOfWork.GetDbSet<CharacterGroup>().AddRange(Locations.Values);
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
        Description = new MarkdownString(locationData.description.Trim())
      };

      Locations.Add(locationData.id,characterGroup);

      _operationLog.Info($"GROUP.CREATE {characterGroup}");

      LocationParentRelation.Add(new ParentRelation {ChildAllrpgId = locationData.id, ParentAllrpgId = locationData.parent});
    }

    private async Task ImportUser(ProfileReply allrpgUser)
    {
      _operationLog.Info("USER.IMPORT: " + allrpgUser);
      var usersRepository = UnitOfWork.GetUsersRepository();
      var user = await usersRepository.GetByAllRpgId(allrpgUser.sid);
      if (user != null)
      {
        _operationLog.Info("USER.FOUND: " + user);
        return;
      }

      user = await usersRepository.GetByEmail(allrpgUser.em) ?? await usersRepository.GetByEmail(allrpgUser.em2);
      if (user == null)
      {
        user = new User {Email = new[] {allrpgUser.em, allrpgUser.em2}.WhereNotNullOrWhiteSpace().First()};
        _operationLog.Info($"USER.CREATE email={user.Email}");
      }
      else
      {
        _operationLog.Info($"USER.UPDATE user={user}");
      }
      user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
      user.Allrpg.Sid = allrpgUser.sid;
      AllrpgImportUtilities.ImportUserFromResult(user, allrpgUser);

      _operationLog.Info($"USER.RESULT user={user}");
    }
  }


}
