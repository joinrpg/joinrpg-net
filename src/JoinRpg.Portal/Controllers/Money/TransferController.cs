using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Money;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Money;

[MasterAuthorize()]
[Route("{projectId}/money/transfer/[action]")]
public class TransferController(
    IProjectMetadataRepository projectMetadataRepository,
    IFinanceService financeService,
    ICurrentUserAccessor currentUserAccessor
    ) : JoinControllerGameBase
{
    [HttpGet]
    public async Task<IActionResult> Create(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (project == null)
        {
            return NotFound();
        }

        var viewModel = new CreateMoneyTransferViewModel
        {
            ProjectId = projectId,
            Receiver = currentUserAccessor.UserIdentification,
            OperationDate = DateOnly.FromDateTime(DateTime.Now),
        };

        Fill(viewModel, project);

        return View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(CreateMoneyTransferViewModel viewModel)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(new(viewModel.ProjectId));
        if (project == null)
        {
            return NotFound();
        }

        Fill(viewModel, project);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var request = new CreateTransferRequest()
        {
            ProjectId = viewModel.ProjectId,
            Sender = viewModel.Sender,
            Receiver = viewModel.Receiver,
            Amount = viewModel.Money,
            OperationDate = viewModel.OperationDate.ToDateTime(TimeOnly.MinValue),
            Comment = viewModel.CommentText,
        };

        try
        {
            await financeService.CreateTransfer(request);
        }
        catch (Exception e)
        {
            AddModelException(e);
            return View(viewModel);
        }

        return RedirectToAction("MoneySummary", "Finances", new { viewModel.ProjectId });
    }


    private void Fill(CreateMoneyTransferViewModel viewModel, ProjectInfo project)
    {
        viewModel.HasAdminAccess =
            project.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney);
    }
}
