using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.WebPortal.Managers.Claims;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/massmail/[action]")]
public class MassMailController(
    IClaimsRepository claimRepository,
    IMassProjectEmailService massMailManager,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet]
    public async Task<ActionResult> ForClaims(ProjectIdentification projectId, CompressedIntList claimIds)
    {
        (var project, var filteredClaims, var somethingFiltered) = await LoadData(projectId, claimIds);

        return View(new MassMailViewModel
        {
            AlsoMailToMasters = claimIds.List.Count == 0,
            ProjectId = projectId,
            Subject = "",
            ProjectName = project.ProjectName,
            ClaimIds = new CompressedIntList(filteredClaims.Select(c => c.ClaimId)),
            Claims = filteredClaims.ToClaimViewModels(),
            ToMyClaimsOnlyWarning = somethingFiltered,
            Body = "Добрый день, %NAME%, \nспешим уведомить вас о всяком. \n Не забудьте заглянуть в свою заявку %СLAIM%",
        });
    }



    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> ForClaims(MassMailViewModel viewModel)
    {
        ProjectIdentification projectId = new(viewModel.ProjectId);
        try
        {

            await massMailManager.MassMail(
                [.. viewModel.ClaimIds.ToClaimIds(projectId)],
                new MarkdownString(viewModel.Body),
                viewModel.Subject,
                viewModel.AlsoMailToMasters
                );
            return View("Success");
        }
        catch (Exception exception)
        {
            (var project, var filteredClaims, var somethingFiltered) = await LoadData(projectId, viewModel.ClaimIds);

            viewModel.Claims = filteredClaims.ToClaimViewModels();
            viewModel.ToMyClaimsOnlyWarning = somethingFiltered;
            viewModel.ProjectName = project.ProjectName;
            AddModelException(exception);
            return View(viewModel);
        }
    }

    private async Task<(ProjectInfo project, List<ClaimWithPlayer> filteredClaims, bool somethingFiltered)> LoadData(ProjectIdentification projectId, CompressedIntList claimIds)
    {
        var claims = (await claimRepository.GetClaimHeadersWithPlayer(claimIds.ToClaimIds(projectId))).ToList();
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);

        if (!project.IsActive)
        {
            throw new ProjectDeactivatedException(projectId);
        }
        var canSendMassEmails = project.HasMasterAccess(currentUserAccessor.UserIdentification, Permission.CanSendMassMails);
        var filteredClaims = canSendMassEmails ? claims : [.. claims.Where(c => c.ResponsibleMasterUserId == currentUserAccessor.UserIdentification)];
        return (project, filteredClaims, filteredClaims.Count != claims.Count);
    }
}
