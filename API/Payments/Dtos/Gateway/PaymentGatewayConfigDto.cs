namespace CMS.API.Payments.Dtos.Gateway;

public class PaymentGatewayConfigDto
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantVPA { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public int Mcc { get; set; }
    public int KeyIndex { get; set; }
    public string? BaseURL { get; set; }
    public string? PublicKeyPath { get; set; }
    public string? PrivateKeyPath { get; set; }
}
