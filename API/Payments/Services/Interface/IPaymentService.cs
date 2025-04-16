using CMS.API.Payments.Dtos;

namespace CMS.API.Payments.Services.Interface;

public interface IPaymentService
{
    string Gateway { get; }
    Task<UPICreateResponse> CreateQRAsync(string orderNumber, double amount, string machineId);
    Task<UPIStatusResponse> StatusAsync(string orderNumber);
    Task<UPIRefundResponse> RefundAsync(string orderNumber, string transactionIdorRefId, double refundAmount, string msg = "", string bankRRN = "");
}
