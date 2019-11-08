using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models {

    public static class FinanceViewModelsExtensions
    {

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
            => source.Where(pt => pt.TypeKind != PaymentTypeKindViewModel.Online);

        /// <summary>
        /// Converts payment types to select box items
        /// </summary>
        public static IEnumerable<JoinSelectListItem> PaymentTypesToSelectBoxItems(
            this IEnumerable<PaymentTypeViewModel> source)
            => source.Select(
                pt => new JoinSelectListItem
                {
                    Text = pt.Name,
                    Value = pt.PaymentTypeId
                });

        /// <summary>
        /// Returns true if online payment is enabled
        /// </summary>
        public static bool OnlinePaymentsEnabled(
            this IEnumerable<PaymentTypeViewModel> paymentTypes)
            => paymentTypes.FirstOrDefault(pt => pt.TypeKind == PaymentTypeKindViewModel.Online) != null;
    }
}
