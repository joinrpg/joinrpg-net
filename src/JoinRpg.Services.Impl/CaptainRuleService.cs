using System.Data;
using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.Claims;

namespace JoinRpg.Services.Impl;
internal class CaptainRuleService(IUnitOfWork unitOfWork,
                                     ICurrentUserAccessor currentUserAccessor,
                                     IProjectMetadataRepository projectMetadataRepository,
                                     ILogger<CaptainRuleService> logger)
    : DbServiceImplBase(unitOfWork, currentUserAccessor), ICaptainRuleService
{
    async Task ICaptainRuleService.RemoveRule(CaptainAccessRule captainAccessSetting)
    {
        var rule = await GetRule(captainAccessSetting);

        logger.LogInformation("Удаляем настройки капитана {captainAccess}", captainAccessSetting);

        UnitOfWork.GetDbSet<CaptainAccessRuleEntity>().Remove(rule);

        await UnitOfWork.SaveChangesAsync();
    }
    async Task ICaptainRuleService.AddOrChangeRule(CaptainAccessRule captainAccessSetting)
    {
        var rule = await GetRule(captainAccessSetting);

        logger.LogInformation("Настраиваем капитана {captainAccess}", captainAccessSetting);

        if (rule is null)
        {
            rule = new CaptainAccessRuleEntity()
            {
                CanApprove = captainAccessSetting.CanApprove,
                CaptainAccessRuleEntityId = -1,
                CaptainUserId = captainAccessSetting.Player.Value,
                CharacterGroupId = captainAccessSetting.CharacterGroup.CharacterGroupId,
                ProjectId = captainAccessSetting.ProjectId,
            };
            UnitOfWork.GetDbSet<CaptainAccessRuleEntity>().Add(rule);
        }
        else
        {
            rule.CanApprove = captainAccessSetting.CanApprove;
        }

        await UnitOfWork.SaveChangesAsync();
    }

    private async Task<CaptainAccessRuleEntity> GetRule(CaptainAccessRule captainAccessSetting)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(captainAccessSetting.ProjectId);
        projectInfo
            .RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims)
            .EnsureProjectActive();


        var rule = await UnitOfWork.GetDbSet<CaptainAccessRuleEntity>()
            .Where(x => x.CharacterGroupId == captainAccessSetting.CharacterGroup.CharacterGroupId &&
            x.ProjectId == captainAccessSetting.CharacterGroup.ProjectId
            && x.CaptainUserId == captainAccessSetting.Player)
            .SingleOrDefaultAsync();
        return rule;
    }

}
