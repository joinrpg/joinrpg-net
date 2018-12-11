using System;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace JoinRpg.DataModel.Finances
{
    public class MoneyTransfer : IProjectEntity
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }
        

        public int ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public virtual User Receiver { get; set; }

        public int Amount { get; set; }

        public MoneyTransferState ResultState { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Changed { get; set; }

        public int CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; }

        public int ChangedById { get; set; }
        [ForeignKey("ChangedById")]
        public virtual User ChangedBy { get; set; }

        public DateTimeOffset OperationDate { get; set; }


        [NotNull]
        public virtual TransferText TransferText { get; set; }
    }

    public enum MoneyTransferState
    {
        Approved,
        Declined,
        PendingForReceiver,
        PendingForSender,
        PendingForBoth,
    }

    public class TransferText
    {
        public int MoneyTransferId { get; set; }
        [NotNull]
        public MarkdownString Text { get; set; } = new MarkdownString();
    }
}
