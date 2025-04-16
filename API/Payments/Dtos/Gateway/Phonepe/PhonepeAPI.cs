using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace CMS.API.Payments.Dtos.Gateway.Phonepe;

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record PhonepeAPIRequest(string Request);

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public record PhonepeAPIResponse(string Response);