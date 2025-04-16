using CMS.Dto.Payments;

namespace CMS.API.Payments.Services.Interface;

public interface IPaymentCallbackService
{
    Task PaymentStatusUpdateAsync(string orderNumber, PaymentStatusCode status, string transactionId, string message, string bankRRN = "");
}