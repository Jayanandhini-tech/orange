namespace CMS.API.Payments.Dtos.Gateway.ICICI;

public class IciciRefundRequest
{
    public string merchantId { get; set; } = string.Empty;
    public string subMerchantId { get; set; } = string.Empty;
    public string terminalId { get; set; } = string.Empty;
    public string originalBankRRN { get; set; } = string.Empty;
    public string merchantTranId { get; set; } = string.Empty;
    public string originalmerchantTranId { get; set; } = string.Empty;
    public string payeeVA { get; set; } = string.Empty;
    public string refundAmount { get; set; } = string.Empty;
    public string note { get; set; } = string.Empty;
    public string onlineRefund { get; set; } = string.Empty;
}
