using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace JoinRpg.DataModel.Finances
{
    public class MoneyTransfer
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; }
        public FinanceOperationState SenderState { get; set; }

        public int ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }
        public FinanceOperationState ReceiverState { get; set; }

        public int Amount { get; set; }

        public FinanceOperationState ResultState { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Changed { get; set; }

        public int CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public User CreatedBy { get; set; }

        public int ChangedById { get; set; }
        [ForeignKey("ChangedById")]
        public User ChangedBy { get; set; }

        public DateTimeOffset OperationDate { get; set; }
    }

    public class MoneyTransferText
    {
        public int MoneyTransferId { get; set; }
        [NotNull]
        public MarkdownString Text { get; set; } = new MarkdownString();
    }
}
