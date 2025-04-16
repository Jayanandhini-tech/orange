using CMS.API.Domains;
using CMS.API.Payments.Services;

namespace CMS.API.Repository.IRepository;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetPaymentUsingOrderNumber(string orderNumber);
    Task<TransactionRequesterDto?> UpdatePaymentStatus(string orderNumber, string status, string transactionId, string msg, string bankRRN);
    Task UpdateRefundStatus(string orderNumber, string refundId, double refundAmount, string transactionId);
}
