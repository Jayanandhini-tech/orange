namespace CMS.API.Payments.Dtos.Gateway.ICICI;

public class IciciInitTransaction
{
    public string merchantId { get; set; } = string.Empty;
    public string terminalId { get; set; } = string.Empty;
    public string amount { get; set; } = string.Empty;
    public string merchantTranId { get; set; } = string.Empty;
    public string billNumber { get; set; } = string.Empty;
}
