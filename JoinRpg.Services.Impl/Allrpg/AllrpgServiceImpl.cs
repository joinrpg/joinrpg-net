using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Allrpg;

namespace JoinRpg.Services.Impl.Allrpg
{
  [UsedImplicitly]
  public class AllrpgServiceImpl : DbServiceImplBase, IAllrpgService
  {
    private readonly AllrpgApi _api;

    public async Task<DownloadResult> DownloadAllrpgProfile(int userId)
    {
      var user = await UserRepository.WithProfile(userId);

      if (user.Allrpg?.Sid != null)
      {
        return DownloadResult.AlreadyDownloaded;
      }

      var reply = await _api.GetProfile(user.Email);

      switch (reply.Status)
      {
        case AllrpgApi.Status.Success:
          break;
        case AllrpgApi.Status.NetworkError:
          return DownloadResult.NetworkError;
        case AllrpgApi.Status.ParseError:
          return DownloadResult.ParseError;
        case AllrpgApi.Status.NoSuchUser:
          user.Allrpg = user.Allrpg ?? new AllrpgUserDetails();
          user.Allrpg.Sid = 0;
          user.Allrpg.PreventAllrpgPassword = true;
          await UnitOfWork.SaveChangesAsync();
          return DownloadResult.Success;
        case AllrpgApi.Status.WrongKey:
          return DownloadResult.WrongKey;
        default:
          throw new ArgumentOutOfRangeException();
      }

      var result = reply.Result;

      user.Allrpg = user.Allrpg ?? new AllrpgUserDetails() {PreventAllrpgPassword = user.PasswordHash != null};
      user.Allrpg.JsonProfile = reply.RawResult; //Save raw value, we m.b. like to parse it later

      AllrpgImportUtilities.ImportUserFromResult(user, result);

      await UnitOfWork.SaveChangesAsync();
      return DownloadResult.Success;
    }

    public async Task<LegacyLoginResult> TryToLoginWithOldPassword(string email, string password)
    {
      var user = await UserRepository.GetByEmail(email);
      if (user == null)
      {
        //TODO: try to import user from scratch
        return LegacyLoginResult.NoSuchUserOrPassword;
      }

      if (user.PasswordHash != null || user.Allrpg?.Sid == null || user.Allrpg?.PreventAllrpgPassword == true)
      {
        return LegacyLoginResult.ImportDisabled;
      }

      var reply = await  _api.CheckPassword(email, password);


      switch (reply.Status)
      {
        case AllrpgApi.Status.Success:
          return reply.Result.Success ? LegacyLoginResult.Success : LegacyLoginResult.NoSuchUserOrPassword;
        case AllrpgApi.Status.NetworkError:
          return LegacyLoginResult.NetworkError;
        case AllrpgApi.Status.ParseError:
          return LegacyLoginResult.ParseError;
        case AllrpgApi.Status.NoSuchUser:
          return LegacyLoginResult.NoSuchUserOrPassword;
        case AllrpgApi.Status.WrongKey:
          return LegacyLoginResult.WrongKey;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public async Task AssociateProject(int currentUserId, int projectId, int allrpgProjectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(currentUserId, acl => acl.IsOwner);
      project.Details = project.Details ?? new ProjectDetails();
      if (project.Details.AllrpgId != null)
      {
        throw new ValueAlreadySetException("Project is already associated with allrpg");
      }
      project.Details.AllrpgId = allrpgProjectId;
      await UnitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> UpdateProject(int currentUserId, int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      project.RequestMasterAccess(currentUserId, acl => acl.IsOwner);
      if (project.Details?.AllrpgId == null)
      {
        return new[] {"Проект не ассоциирован с allrpg"};
      }
      
      var reply = await _api.DownloadProject((int) project.Details.AllrpgId);


      switch (reply.Status)
      {
        case AllrpgApi.Status.Success:
        {
          var log = new OperationLog();
          try
          {
            var importer = new AllrpgProjectImporter(project, UnitOfWork, log);
            await importer.Apply(reply.Result);
          }
          catch (Exception e)
          {
            log.Error($"EXCEPTION: {e}");
          }
          return log.Results;
        }
        case AllrpgApi.Status.NetworkError:
        return new[] { "Сетевая ошибка" };
        case AllrpgApi.Status.ParseError:
        return new[] { "Не разобран ответ allrpg" };
        case AllrpgApi.Status.WrongKey:
        return new[] { "Ошибочный ключ" };
        default:
        throw new ArgumentOutOfRangeException(nameof(reply.Status));
      }
    }

    public AllrpgServiceImpl(IUnitOfWork unitOfWork, IAllrpgApiKeyStorage keyStorage) : base(unitOfWork)
    {
      _api = new AllrpgApi(keyStorage.Key);
    }
  }

}
