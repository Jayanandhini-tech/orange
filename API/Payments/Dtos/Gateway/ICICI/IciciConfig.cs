namespace CMS.API.Payments.Dtos.Gateway.ICICI;

public class IciciConfig
{
    public string BaseURL { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public int MCC { get; set; }
    public string PayeeVPA { get; set; } = string.Empty;
    public string ICICIPublicKeyPath { get; set; } = string.Empty;
    public string MerchantPrivateKeyPath { get; set; } = string.Empty;
}
