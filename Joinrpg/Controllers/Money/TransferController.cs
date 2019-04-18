using System;
using System.Threading.Tasks;
using System.Web.Mvc;
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
        private IFinanceService FinanceService { get; }

        public TransferController(ApplicationUserManager userManager,
            IProjectRepository projectRepository,
            IProjectService projectService,
            IExportDataService exportDataService,
            IFinanceService financeService,
            IUserRepository userRepository) : base(projectRepository,
                projectService,
                userRepository)
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
                Comment = viewModel.CommentText,
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

        public async Task<ActionResult> Approve(int projectId, int moneyTransferId)
            => await ApproveRejectTransfer(new ApproveRejectTransferRequest()
            {
                Approved = true,
                ProjectId = projectId,
                MoneyTranferId = moneyTransferId,
            });

        public async Task<ActionResult> Decline(int projectId, int moneyTransferId)
            => await ApproveRejectTransfer(new ApproveRejectTransferRequest()
            {
                Approved = false,
                ProjectId = projectId,
                MoneyTranferId = moneyTransferId,
            });

        private async Task<ActionResult> ApproveRejectTransfer(ApproveRejectTransferRequest request)
        {
            try
            {
                await FinanceService.MarkTransfer(request);
            }
            catch
            {
                //TODO handle error
                return RedirectToAction("MoneySummary", "Finances", new {request.ProjectId});
            }

            return RedirectToAction("MoneySummary", "Finances", new {request.ProjectId});
        }
    }
}
