using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class PgSetting : VendorEntity
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string GatewayName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ProviderName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string MerchantId { get; set; } = string.Empty;

    [MaxLength(70)]
    public string MerchantName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string MerchantVPA { get; set; } = string.Empty;

    [MaxLength(50)]
    public string MerchantKey { get; set; } = string.Empty;

    public int Mcc { get; set; }

    public int KeyIndex { get; set; }

    public string? BaseURL { get; set; }
    public string? PublicKeyPath { get; set; }
    public string? PrivateKeyPath { get; set; }
}
