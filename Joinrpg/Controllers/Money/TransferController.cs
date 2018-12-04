using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CharacterGroups;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Controllers.Money
{
    [MasterAuthorize()]
    public class TransferController : Common.ControllerGameBase
    {
        public IFinanceService FinanceService { get; }

        public TransferController(ApplicationUserManager userManager,
            [NotNull]
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IFinanceService financeService
        ) : base(userManager,
            projectRepository,
            projectService,
            exportDataService)
        {
            FinanceService = financeService;
        }

        [HttpGet]
        public async Task<ActionResult> Create(int projectId)
        {
            var project = await ProjectRepository.GetProjectAsync(projectId);
            if (project == null)
            {
                return HttpNotFound();
            }

            var viewModel = new CreateMoneyTransferViewModel
            {
                ProjectId = projectId, Receiver = CurrentUserId,
            };

            Fill(viewModel, project);

            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateMoneyTransferViewModel viewModel)
        {
            var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
            if (project == null)
            {
                return HttpNotFound();
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
                OperationDate = viewModel.OperationDate,
            };

            try
            {
                await FinanceService.CreateTransfer(request);
            }
            catch (Exception e)
            {
                ModelState.AddException(e);
                return View(viewModel);
            }

            return RedirectToAction("MoneySummary", "Finances", new {viewModel.ProjectId});

        }


        private void Fill(CreateMoneyTransferViewModel viewModel, Project project)
        {
            viewModel.Masters = project.GetMasterListViewModel();
            viewModel.HasAdminAccess =
                project.HasMasterAccess(CurrentUserId, acl => acl.CanManageMoney);
        }
    }
}
