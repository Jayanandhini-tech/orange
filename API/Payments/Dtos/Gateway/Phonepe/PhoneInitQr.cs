using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace CMS.API.Payments.Dtos.Gateway.Phonepe;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class PhonePeInitQrRequest
{
    public string MerchantId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public int ExpiresIn { get; set; }
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record PhonePeInitQrResponse(
     bool Success,
     string Code,
     string Message,
     Data Data
);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record Data(
     string MerchantId,
     string TransactionId,
     int Amount,
     string QrString);