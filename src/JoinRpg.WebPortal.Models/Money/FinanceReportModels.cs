using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Validation;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Money;
using MoreLinq;

namespace JoinRpg.Web.Models
{
    public class MasterBalanceViewModel
    {
        public User Master { get; }

        [Display(Name = "Денег на руках")]
        public int Total { get; }

        [Display(Name = "Получено от других мастеров")]
        public int ReceiveBalance { get; }
        [Display(Name = "Отправлено другим мастерам")]
        public int SendBalance { get; }
        [Display(Name = "Собрано взносов")]
        public int FeeBalance { get; }
        [Display(Name = "Собрано взносов (не подтверждено)")]
        public int ModerationBalance { get; }

        [Display(Name = "Расходы")]
        public int ExpensesBalance { get; } = 0; //Not used

        public int ProjectId { get; }

        public MasterBalanceViewModel(
            [NotNull] User master,
            int projectId,
            IReadOnlyCollection<FinanceOperation> masterOperations,
            IReadOnlyCollection<MoneyTransfer> masterTransfers)
        {
            Master = master ?? throw new ArgumentNullException(nameof(master));
            ProjectId = projectId;
            ReceiveBalance = masterTransfers.ReceivedByMasterSum(master);
            SendBalance = masterTransfers.SendedByMasterSum(master);
            FeeBalance = masterOperations
                .Where(fo => fo.Approved)
                .Where(fo => fo.PaymentType?.User?.UserId == master.UserId)
                .Sum(fo => fo.MoneyAmount);

            ModerationBalance = masterOperations
                .Where(fo => fo.RequireModeration)
                .Where(fo => fo.PaymentType?.User.UserId == master.UserId)
                .Sum(fo => fo.MoneyAmount);

            Total = FeeBalance + ReceiveBalance + SendBalance;
        }

        public bool AnythingEverHappens() => ReceiveBalance != 0 || FeeBalance != 0 ||
                                             SendBalance != 0 || ModerationBalance != 0 ||
                                             ExpensesBalance != 0;
    }

    public static class MasterBalanceBuilder
    {
        public static IReadOnlyCollection<MasterBalanceViewModel> ToMasterBalanceViewModels(
            IReadOnlyCollection<FinanceOperation> masterOperations,
            IReadOnlyCollection<MoneyTransfer> masterTransfers,
            int projectId)
        {
            var masters = masterOperations.Select(fo => fo.PaymentType?.User)
                .WhereNotNull()
                .Union(masterTransfers.Select(mt => mt.Receiver))
                .Union(masterTransfers.Select(mt => mt.Sender))
                .DistinctBy(master => master.UserId);


            var summary = masters.Select(master =>
                    new MasterBalanceViewModel(master, projectId, masterOperations, masterTransfers))
                .Where(fr => fr.AnythingEverHappens())
                .OrderBy(fr => fr.Master.GetDisplayName());
            return summary.ToArray();
        }
    }

    public class MoneyInfoForUserViewModel
    {
        public UserProfileDetailsViewModel UserDetails { get; }
        public int ProjectId { get; }
        public IReadOnlyCollection<MoneyTransferListItemViewModel> Transfers { get; }

        public FinOperationListViewModel Operations { get; }

        public MasterBalanceViewModel Balance { get; }

        public IReadOnlyCollection<PaymentTypeSummaryViewModel> PaymentTypeSummary { get; }

        public MoneyInfoForUserViewModel(Project project,
            IReadOnlyCollection<MoneyTransfer> transfers,
            User master,
            IUriService urlHelper,
            IReadOnlyCollection<FinanceOperation> operations,
            PaymentTypeSummaryViewModel[] payments,
            int currentUserId)
        {
            Transfers = transfers
                .OrderBy(f => f.Id)
                .Select(f => new MoneyTransferListItemViewModel(f, currentUserId)).ToArray();
            ProjectId = project.ProjectId;
            UserDetails = new UserProfileDetailsViewModel(master, AccessReason.CoMaster);

            Operations = new FinOperationListViewModel(project, urlHelper, operations);

            Balance = new MasterBalanceViewModel(master, project.ProjectId, operations, transfers);

            PaymentTypeSummary = payments;
        }
    }

    public class FinOperationListViewModel : IOperationsAwareView
    {
        public IReadOnlyCollection<FinOperationListItemViewModel> Items { get; }

        public int? ProjectId { get; }

        public IReadOnlyCollection<int> ClaimIds { get; }
        public IReadOnlyCollection<int> CharacterIds => Array.Empty<int>();

        public FinOperationListViewModel(Project project, IUriService urlHelper, IReadOnlyCollection<FinanceOperation> operations)
        {
            Items = operations
              .OrderBy(f => f.CommentId)
              .Select(f => new FinOperationListItemViewModel(f, urlHelper)).ToArray();
            ProjectId = project.ProjectId;
            ClaimIds = operations.Select(c => c.ClaimId).Distinct().ToArray();
        }
    }

    public class FinOperationListItemViewModel
    {
        [Display(Name = "# операции")]
        public int FinanceOperationId { get; }

        [Display(Name = "Внесено денег"), Required]
        public int Money { get; }

        [Display(Name = "Изменение взноса"), Required]
        public int FeeChange { get; }

        [Display(Name = "Оплачено мастеру")]
        public User PaymentMaster { get; }

        [Display(Name = "Способ оплаты"), Required]
        public string PaymentTypeName { get; }

        [Display(Name = "Отметил"), Required]
        public User MarkingMaster { get; }

        [Display(Name = "Дата внесения"), Required, DateShouldBeInPast]
        public DateTime OperationDate { get; }

        [Display(Name = "Заявка"), Required]
        public string Claim { get; }

        [Url, Display(Name = "Ссылка на заявку")]
        public string ClaimLink { get; }

        [Display(Name = "Игрок"), Required]
        public User Player { get; }

        public FinOperationListItemViewModel(FinanceOperation fo, IUriService uriService)
        {
            PaymentTypeName = fo.PaymentType?.GetDisplayName();
            PaymentMaster = fo.PaymentType?.User;
            Claim = fo.Claim.Name;
            FeeChange = fo.FeeChange;
            Money = fo.MoneyAmount;
            OperationDate = fo.OperationDate;
            FinanceOperationId = fo.CommentId;
            MarkingMaster = fo.Comment.Author;
            Player = fo.Claim.Player;
            ClaimLink = uriService.Get(fo.Claim);

            if (fo.OperationType == FinanceOperationType.TransferFrom ||
                fo.OperationType == FinanceOperationType.TransferTo)
            {
                PaymentTypeName =
                    ((FinanceOperationTypeViewModel)fo.OperationType).GetDisplayName();
            }
        }
    }

    public class MoneyTransferListItemViewModel : MoneyTransferViewModelBase
    {
        [Display(Name = "Внес"), Required]
        public User MarkingMaster { get; }

        [Display(Name = "Статус")]
        public MoneyTransferStateViewModel State { get; }

        [Display(Name = "От")]
        public User Sender { get; }

        [Display(Name = "Кому")]
        public User Receiver { get; }

        public bool HasApproveAccess { get; }

        public int Id { get; }
        [Display(Name = "Комментарий")]
        public JoinHtmlString Comment { get; }

        public MoneyTransferListItemViewModel(MoneyTransfer fo, int currentUserId)
        {
            Id = fo.Id;
            ProjectId = fo.ProjectId;
            Sender = fo.Sender;
            Receiver = fo.Receiver;
            MarkingMaster = fo.CreatedBy;
            OperationDate = fo.OperationDate.UtcDateTime;
            State = (MoneyTransferStateViewModel)fo.ResultState;

            Money = fo.Amount;
            MarkingMaster = fo.CreatedBy;

            var isPendingSender = State == MoneyTransferStateViewModel.PendingForSender ||
                        State == MoneyTransferStateViewModel.PendingForBoth;

            var isPendingReceiver = State == MoneyTransferStateViewModel.PendingForReceiver ||
                              State == MoneyTransferStateViewModel.PendingForBoth;
            var isPendingAny = isPendingReceiver || isPendingSender;

            HasApproveAccess =
                (fo.Project.HasMasterAccess(currentUserId, acl => acl.CanManageMoney) &&
                 isPendingAny)
                || (currentUserId == Sender.UserId && isPendingSender)
                || (currentUserId == Receiver.UserId && isPendingReceiver);

            Comment = fo.TransferText.Text.ToHtmlString();
        }
    }


    public class PaymentTypeSummaryViewModel
    {
        public PaymentTypeSummaryViewModel(PaymentType pt, ICollection<FinanceOperation> projectFinanceOperations)
        {
            Name = pt.GetDisplayName();
            Master = pt.User;
            Total = projectFinanceOperations
                .Where(fo => fo.PaymentTypeId == pt.PaymentTypeId && fo.Approved)
                .Sum(fo => fo.MoneyAmount);

            Moderation = projectFinanceOperations
                .Where(fo => fo.PaymentTypeId == pt.PaymentTypeId && fo.RequireModeration)
                .Sum(fo => fo.MoneyAmount);
        }

        [Display(Name = "Способ приема оплаты")]
        public string Name { get; }
        [Display(Name = "Мастер")]
        public User Master { get; }
        [Display(Name = "Итого")]
        public int Total { get; }
        [Display(Name = "На модерации")]
        public int Moderation { get; }
    }

    public class MoneyInfoTotalViewModel
    {
        public int ProjectId { get; }

        public FinOperationListViewModel Operations { get; }

        public IReadOnlyCollection<MasterBalanceViewModel> Balance { get; }

        public IReadOnlyCollection<PaymentTypeSummaryViewModel> PaymentTypeSummary { get; }

        public IReadOnlyCollection<MoneyTransferListItemViewModel> Transfers { get; set; }

        public MoneyInfoTotalViewModel(Project project,
            IReadOnlyCollection<MoneyTransfer> transfers,
            IUriService urlHelper,
            IReadOnlyCollection<FinanceOperation> operations,
            PaymentTypeSummaryViewModel[] payments,
            int currentUserId)
        {

            var masters = operations.Select(fo => fo.PaymentType?.User)
                .Union(transfers.Select(mt => mt.Receiver))
                .Union(transfers.Select(mt => mt.Sender))
                .Distinct();

            ProjectId = project.ProjectId;

            Operations = new FinOperationListViewModel(project, urlHelper, operations);

            Balance = MasterBalanceBuilder.ToMasterBalanceViewModels(operations, transfers, project.ProjectId);

            Transfers = transfers.Select(transfer =>
                new MoneyTransferListItemViewModel(transfer, currentUserId)).ToArray();

            PaymentTypeSummary = payments;
        }

    }
}
