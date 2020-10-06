using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;

namespace JoinRpg.Dal.Impl.Repositories
{
    [UsedImplicitly]
    internal class FinanceReportRepositoryImpl : GameRepositoryImplBase, IFinanceReportRepository
    {
        public FinanceReportRepositoryImpl(MyDbContext ctx) : base(ctx)
        {
        }

        public Task<List<MoneyTransfer>> GetMoneyTransfersForMaster(int projectId, int masterId)
            => Ctx.Set<MoneyTransfer>()
                .Include(mt => mt.TransferText)
                .Where(mt => mt.SenderId == masterId || mt.ReceiverId == masterId)
                .Where(mt => mt.ProjectId == projectId)
                .ToListAsync();

        public Task<List<MoneyTransfer>> GetAllMoneyTransfers(int projectId)
            => Ctx.Set<MoneyTransfer>()
                .Include(mt => mt.TransferText)
                .Where(mt => mt.ProjectId == projectId)
                .ToListAsync();

        public Task<List<PaymentTypeDto>> GetPaymentTypesForMaster(int projectId, int masterId)
        {
            var query =
                from pt in Ctx.Set<PaymentType>()
                where pt.ProjectId == projectId && pt.UserId == masterId
                select new PaymentTypeDto
                {
                    PaymentTypeId = pt.PaymentTypeId,
                    Name = pt.Name,
                    TypeKind = pt.TypeKind,
                };
            return query.ToListAsync();
        }


    }
}
