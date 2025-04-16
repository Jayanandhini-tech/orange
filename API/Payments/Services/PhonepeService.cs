using CMS.API.Payments.Dtos;
using CMS.API.Payments.Dtos.Gateway;
using CMS.API.Payments.Dtos.Gateway.Phonepe;
using CMS.API.Payments.Services.Interface;
using CMS.Dto.Payments;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CMS.API.Payments.Services;

public class PhonepeService : IPaymentService
{
    private readonly PaymentGatewayConfigDto config;
    private readonly HttpClient httpClient;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<PhonepeService> logger;

    public string Gateway { get { return "Phonepe"; } }

    public PhonepeService(PaymentGatewayConfigDto config, HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<PhonepeService> logger)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public async Task<UPICreateResponse> CreateQRAsync(string orderNumber, double amount, string machineId)
    {
        PhonePeInitQrRequest initQr = new PhonePeInitQrRequest()
        {
            MerchantId = config.MerchantId,
            TransactionId = orderNumber,
            StoreId = machineId,
            Amount = Convert.ToInt32(amount * 100),
            ExpiresIn = 178
        };


        string urlEndpoint = "/v3/qr/init";

        string jsonStr = JsonConvert.SerializeObject(initQr);
        string base64Json = ConvertStringToBase64(jsonStr);
        string jsonSuffixString = $"{urlEndpoint}{config.MerchantKey}";
        string checksum = GenerateSha256ChecksumFromBase64Json(base64Json, jsonSuffixString);
        checksum = $"{checksum}###{config.KeyIndex}";

        PhonepeAPIRequest initRequest = new PhonepeAPIRequest(base64Json);

        var post_data = new StringContent(JsonConvert.SerializeObject(initRequest), Encoding.UTF8, "application/json");
        string url = $"{config.BaseURL}{urlEndpoint}";
        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.Absolute));
        httpRequest.Headers.Add("X-VERIFY", checksum);
        httpRequest.Headers.Add("X-PROVIDER-ID", config.ProviderId);
        httpRequest.Headers.Add("X-CALLBACK-URL", GetCallBackURL());
        httpRequest.Headers.Add("X-CALL-MODE", "POST");

        httpRequest.Content = post_data;
        var response = await httpClient.SendAsync(httpRequest);
        string responseText = await response.Content.ReadAsStringAsync();

        logger.LogTrace($"URL : {url}");
        logger.LogTrace($"Body : {jsonStr}");
        logger.LogTrace($"X-VERIFY : {checksum}");

        logger.LogInformation(responseText);

        if (response.StatusCode != HttpStatusCode.OK)
            return new UPICreateResponse() { Success = false, Message = "Unable to generate QR" };

        PhonePeInitQrResponse initResponse = JsonConvert.DeserializeObject<PhonePeInitQrResponse>(responseText)!;

        if (!(initResponse.Success && initResponse.Code == "SUCCESS"))
            return new UPICreateResponse() { Success = false, Message = "Unable to generate QR" };

        return new UPICreateResponse() { Success = true, UpiString = initResponse.Data.QrString, TransactionId = initResponse.Data.TransactionId, DisplayName = config.MerchantName };

    }


    public async Task<UPIStatusResponse> StatusAsync(string orderNumber)
    {
        string urlEndpoint = $"/v3/transaction/{config.MerchantId}/{orderNumber}/status";
        string x_verify_string = $"{urlEndpoint}{config.MerchantKey}";
        string checksum = GenerateSha256ChecksumFromBase64Json("", x_verify_string);
        checksum = $"{checksum}###{config.KeyIndex}";

        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri($"{config.BaseURL}{urlEndpoint}", UriKind.Absolute));
        httpRequest.Headers.Add("X-VERIFY", checksum);
        httpRequest.Headers.Add("X-PROVIDER-ID", config.ProviderId);

        var response = await httpClient.SendAsync(httpRequest);
        string responseText = await response.Content.ReadAsStringAsync();
        logger.LogInformation(responseText);

        if (response.StatusCode != HttpStatusCode.OK)
            return new UPIStatusResponse() { Success = false, Status = PaymentStatusCode.PENDING, Message = "Unable check the status", TransactionId = "" };

        var paymentStatus = JsonConvert.DeserializeObject<PhonePePaymentStatus>(responseText)!;

        var status = paymentStatus.Code switch
        {
            "PAYMENT_PENDING" => PaymentStatusCode.PENDING,
            "PAYMENT_SUCCESS" => PaymentStatusCode.SUCCESS,
            _ => PaymentStatusCode.FAILED,
        };

        return new UPIStatusResponse() { Success = true, Status = status, TransactionId = paymentStatus.Data.ProviderReferenceId, Message = "" };
    }


    public async Task<UPIRefundResponse> RefundAsync(string orderNumber, string transactionIdorRefId, double refundAmount, string msg = "", string bankRRN = "")
    {

        string refundId = $"R-{orderNumber}";
        var refundAPIRequest = new PhonePeRefundAPIRequest(config.MerchantId, refundId, orderNumber, orderNumber, transactionIdorRefId, Convert.ToInt32(refundAmount * 100), msg);

        string urlEndpoint = "/v3/credit/backToSource";
        string jsonStr = JsonConvert.SerializeObject(refundAPIRequest);
        string base64Json = ConvertStringToBase64(jsonStr);
        string jsonSuffixString = $"{urlEndpoint}{config.MerchantKey}";
        string checksum = GenerateSha256ChecksumFromBase64Json(base64Json, jsonSuffixString);
        checksum = $"{checksum}###{config.KeyIndex}";

        var phonepeRequest = new PhonepeAPIRequest(base64Json);
        var post_data = new StringContent(JsonConvert.SerializeObject(phonepeRequest), Encoding.UTF8, "application/json");

        HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri($"{config.BaseURL}{urlEndpoint}", UriKind.Absolute));
        httpRequest.Headers.Add("X-VERIFY", checksum);
        httpRequest.Headers.Add("X-PROVIDER-ID", config.ProviderId);
        httpRequest.Content = post_data;

        var response = await httpClient.SendAsync(httpRequest);
        string responseText = await response.Content.ReadAsStringAsync();
        logger.LogInformation(responseText);


        if (response.StatusCode != HttpStatusCode.OK)
            return new UPIRefundResponse() { Success = false, RefundId = refundId };

        var refundResponse = JsonConvert.DeserializeObject<PhonePeRefundAPIResponse>(responseText)!;

        if (!refundResponse.Success)
            return new UPIRefundResponse() { Success = refundResponse.Success, RefundId = refundId, Status = "", RefundRefId = "" };


        return new UPIRefundResponse() { Success = refundResponse.Success, RefundId = refundId, Status = refundResponse.Data.Status, RefundRefId = refundResponse.Data.ProviderReferenceId };

    }



    private string GetCallBackURL()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        string callbackURL = $"https://{request!.Host}/api/payments/gateway/phonepe";
        //logger.LogInformation("Callbackurl : " + callbackURL);
        return callbackURL;
    }

    private string ConvertStringToBase64(string inputString)
    {
        byte[] requestBytes = Encoding.UTF8.GetBytes(inputString);
        string base64Json = Convert.ToBase64String(requestBytes);
        return base64Json;
    }

    private string GenerateSha256ChecksumFromBase64Json(string base64JsonString, string jsonSuffixString)
    {
        string checksum = string.Empty;
        SHA256 sha256 = SHA256.Create();
        string checksumString = base64JsonString + jsonSuffixString;
        byte[] checksumBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(checksumString));
        foreach (byte b in checksumBytes)
        {
            checksum += $"{b:x2}";
        }
        return checksum;
    }


}
