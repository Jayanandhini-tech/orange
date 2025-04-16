using CMS.API.Payments.Dtos.ICICI;

namespace CMS.API.Payments.Services.Interface;

public interface IiciciGatewayService
{
    Task<IciciQRDto> InitateQRAsync(string orderNumber, decimal amount);
    string GetEncryptedPayload(string plainText);
    string DecryptData(string encryptedText);
    Task<IciciStatusDto> CheckStatusAsync(string orderNumber);
    Task<RefundDto> RefundAsync(string orderNumber, string bankRRN, decimal refundAmount);
}
