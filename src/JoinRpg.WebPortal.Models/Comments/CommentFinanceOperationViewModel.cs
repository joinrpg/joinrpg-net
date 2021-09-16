using System;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models
{
    /// <summary>
    /// Model used for comment finance operation output
    /// </summary>
    public class CommentFinanceOperationViewModel
    {
        /// <summary>
        /// Reply form Id
        /// </summary>
        public int ReplyFormIndex { get; }

        /// <summary>
        /// true if moderation controls must be shown
        /// </summary>
        public bool ShowModerationControls { get; }

        /// <summary>
        /// true if link to claim has to be shown for transfer payments
        /// (if false, link to user will be shown)
        /// </summary>
        public bool ShowLinkToClaimIfTransfer { get; }

        /// <summary>
        /// Finance operation
        /// </summary>
        public FinanceOperation FinanceOperation { get; }

        /// <summary>
        /// true if Status row has to be rendered
        /// </summary>
        public bool ShowStatus { get; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommentFinanceOperationViewModel(CommentViewModel source, int replyFormIndex)
        {
            ShowLinkToClaimIfTransfer = source.HasMasterAccess;
            FinanceOperation = source.Finance;
            ShowModerationControls = source.ShowFinanceModeration;
            ReplyFormIndex = replyFormIndex;

            switch (source.Finance.OperationType)
            {
                case FinanceOperationType.FeeChange:
                case FinanceOperationType.TransferTo:
                case FinanceOperationType.TransferFrom:
                    ShowStatus = false;
                    break;
                case FinanceOperationType.PreferentialFeeRequest:
                case FinanceOperationType.Submit:
                case FinanceOperationType.Online:
                case FinanceOperationType.Refund:
                    ShowStatus = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

