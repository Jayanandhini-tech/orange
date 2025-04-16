using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace CMS.API.Payments.Dtos.Gateway.Phonepe;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record PhonePePaymentStatus(
    bool Success,
     string Code,
     string Message,
     PhonePePaymentStatusData Data
     );

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record PhonePePaymentStatusData(
     string MerchantId,
     string TransactionId,
     string ProviderReferenceId,
     int Amount,
     string PaymentState,
     string PayResponseCode
    );