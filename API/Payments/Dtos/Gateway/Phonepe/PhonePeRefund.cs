using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace CMS.API.Payments.Dtos.Gateway.Phonepe;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record PhonePeRefundAPIRequest(
    string MerchantId,
    string TransactionId,
    string OriginalTransactionId,
    string MerchantOrderId,
    string ProviderReferenceId,
    int Amount,
    string Message
   );

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record PhonePeRefundAPIResponse(
     bool Success,
     string Code,
     string Message,
    RefundDate Data
    );


[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record RefundDate(
     string MerchantId,
     string Status,
     int Amount,
     string ProviderReferenceId,
     string TransactionId);