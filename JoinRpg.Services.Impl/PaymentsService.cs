using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl
{
    /// <inheritdoc cref="IPaymentsService" />
    public class PaymentsService : IPaymentsService
    {
        /// <inheritdoc />
        public Task<ClaimPaymentContext> InitiateClaimPayment(ClaimPaymentRequest request)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<ClaimPaymentResultContext> SetClaimPaymentResult(int paymentId, bool succeeded)
            => throw new NotImplementedException();
    }
}
