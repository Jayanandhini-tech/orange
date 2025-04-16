using CMS.API.Domains;
using CMS.API.Payments.Services;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    private readonly AppDBContext db;

    public PaymentRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
    }

    public async Task<Payment?> GetPaymentUsingOrderNumber(string orderNumber)
    {
        return await db.Payments.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
    }

    public async Task<TransactionRequesterDto?> UpdatePaymentStatus(string orderNumber, string status, string transactionId, string msg, string bankRRN)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);

        if (payment is null)
            return null;


        payment.Status = status;
        payment.PaymentOn = DateTime.Now;

        if (!string.IsNullOrEmpty(transactionId))
            payment.TransactionId = transactionId;

        if (!string.IsNullOrEmpty(bankRRN))
            payment.BankRRN = bankRRN;

        if (!string.IsNullOrEmpty(msg))
            payment.Reference = msg.Substring(0, msg.Length > 38 ? 38 : msg.Length);

        if (status == "SUCCESS")
        {
            payment.IsPaid = true;

            var order = await db.Orders.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
            if (order is not null)
            {
                order.IsPaid = payment.IsPaid;
                order.PaidAmount = payment.Amount;
                order.PaymentId = payment.Id;
                order.Status = payment.IsPaid ? StrDir.OrderStatus.PAID : StrDir.OrderStatus.FAILED;
            }
        }
        await db.SaveChangesAsync();

        return new TransactionRequesterDto(payment.VendorId, payment.MachineId, payment.Id);
    }


    public async Task UpdateRefundStatus(string orderNumber, string refundId, double refundAmount, string transactionId)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);

        if (payment is not null)
        {
            payment.IsRefunded = true;
            payment.RefundId = refundId;
            payment.RefundAmount = refundAmount;
            payment.RefundTransactionId = transactionId ?? "";


            var order = await db.Orders.FirstOrDefaultAsync(x => x.OrderNumber == orderNumber);
            if (order is not null)
            {
                order.IsRefunded = true;
                order.RefundedAmount = refundAmount;
            }

            await db.SaveChangesAsync();
        }
    }
}
