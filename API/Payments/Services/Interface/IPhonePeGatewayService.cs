using CMS.API.Payments.Dtos.Gateway.Phonepe;

namespace CMS.API.Payments.Services.Interface;

public interface IPhonePeGatewayService
{
    Task<PhonePePaymentStatus> CheckStatus(string orderId);
    Task<PhonePeInitQrResponse> InitQr(string orderId, string storeId, double amount);
    Task<PhonePeRefundAPIResponse> Refund(string orderId, string providerReferenceId, double refundAmount);
    string GetDisplayName();
}
