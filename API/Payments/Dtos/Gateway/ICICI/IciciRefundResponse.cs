namespace CMS.API.Payments.Dtos.Gateway.ICICI;

public class IciciRefundResponse
{
    public string merchantId { get; set; } = string.Empty;
    public string subMerchantId { get; set; } = string.Empty;
    public string terminalId { get; set; } = string.Empty;
    public string success { get; set; } = string.Empty;
    public string response { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public string originalBankRRN { get; set; } = string.Empty;
    public string merchantTranId { get; set; } = string.Empty;
}
