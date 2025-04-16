namespace CMS.API.Payments.Dtos.Gateway.ICICI;

public class IciciPaymentCallBackDto
{
    public string merchantId { get; set; } = string.Empty;
    public string subMerchantId { get; set; } = string.Empty;
    public string terminalId { get; set; } = string.Empty;
    public string BankRRN { get; set; } = string.Empty;
    public string merchantTranId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerMobile { get; set; } = string.Empty;
    public string PayerVA { get; set; } = string.Empty;
    public string PayerAmount { get; set; } = string.Empty;
    public string TxnStatus { get; set; } = string.Empty;
    public string TxnInitDate { get; set; } = string.Empty;
    public string TxnCompletionDate { get; set; } = string.Empty;
}
