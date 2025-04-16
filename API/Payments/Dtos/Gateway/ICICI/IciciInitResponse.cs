namespace CMS.API.Payments.Dtos.Gateway.ICICI;

public class IciciInitResponse
{
    public string response { get; set; } = string.Empty;
    public string merchantId { get; set; } = string.Empty;
    public string terminalId { get; set; } = string.Empty;
    public string success { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public string merchantTranId { get; set; } = string.Empty;
    public string refId { get; set; } = string.Empty;
}
