using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Models {

    public static class FinanceViewModelsExtensions
    {

        /// <summary>
        /// Returns title of operation state
        /// </summary>
        public static string ToTitleString(this FinanceOperationState self)
            => ((FinanceOperationStateViewModel)self).GetDisplayName();

        /// <summary>
        /// Returns row class for detailed payments view
        /// </summary>
        public static string ToRowClass(this FinanceOperationState self)
        {
            switch (self)
            {
                case FinanceOperationState.Proposed:
                    return "unapprovedPayment";
                case FinanceOperationState.Declined:
                    return "unapprovedPayment danger";
                case FinanceOperationState.Approved:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self));
            }
        }

        /// <summary>
        /// Excludes payment types that could not be selected by a user during payment submit
        /// independently of user permissions and role
        /// </summary>
        public static IEnumerable<PaymentTypeViewModel> GetUserSelectablePaymentTypes(
            this IEnumerable<PaymentTypeViewModel> source)
            => source.Where(pt => pt.Kind != PaymentTypeKindViewModel.Online);

        /// <summary>
        /// Converts payment types to select box items
        /// </summary>
        public static IEnumerable<SelectListItem> PaymentTypesToSelectBoxItems(
            this IEnumerable<PaymentTypeViewModel> source)
            => source.Select(
                pt => new SelectListItem
                {
                    Text = pt.Name,
                    Value = pt.PaymentTypeId.ToString()
                });

        /// <summary>
        /// Returns true if online payment is enabled
        /// </summary>
        public static bool OnlinePaymentsEnabled(
            this IEnumerable<PaymentTypeViewModel> paymentTypes)
            => paymentTypes.FirstOrDefault(pt => pt.Kind == PaymentTypeKindViewModel.Online) != null;
    }
}
