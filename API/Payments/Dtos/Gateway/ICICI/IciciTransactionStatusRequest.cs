namespace CMS.API.Payments.Dtos.Gateway.ICICI;

public class IciciTransactionStatusRequest
{
    public string merchantId { get; set; } = string.Empty;
    public string subMerchantId { get; set; } = string.Empty;
    public string terminalId { get; set; } = string.Empty;
    public string merchantTranId { get; set; } = string.Empty;
}
