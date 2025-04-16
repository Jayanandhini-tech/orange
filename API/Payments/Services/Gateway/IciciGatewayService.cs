using CMS.API.Payments.Dtos.Gateway.ICICI;
using CMS.API.Payments.Dtos.ICICI;
using CMS.API.Payments.Services.Interface;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CMS.API.Payments.Services.Gateway;

public class IciciGatewayService : IiciciGatewayService
{
    private readonly HttpClient httpClient;
    private readonly IDynamicQRCodeGenerator qrCodeGenerator;
    private readonly ILogger<IciciGatewayService> logger;
    private IciciConfig iciciConfig = new();

    public IciciGatewayService(HttpClient httpClient, IDynamicQRCodeGenerator qrCodeGenerator, IConfiguration configuration, ILogger<IciciGatewayService> logger)
    {
        this.httpClient = httpClient;
        this.qrCodeGenerator = qrCodeGenerator;
        this.logger = logger;
        iciciConfig = configuration.GetSection("Icici").Get<IciciConfig>() ?? new();
    }


    public async Task<IciciQRDto> InitateQRAsync(string orderNumber, decimal amount)
    {
        IciciInitTransaction init = new IciciInitTransaction()
        {
            merchantId = iciciConfig.MerchantId,
            terminalId = iciciConfig.MCC.ToString(),
            merchantTranId = orderNumber,
            billNumber = orderNumber,
            amount = amount.ToString("0.00")
        };

        string requestJson = JsonConvert.SerializeObject(init);
        logger.LogInformation(requestJson);

        string payload = GetEncryptedPayload(requestJson);
        //  string url = $"api/MerchantAPI/UPI/v0/QR3/{iciciConfig.MerchantId}"; For UAT 
        string url = $"api/MerchantAPI/UPI/v0/QR/{iciciConfig.MerchantId}";
        var content = new StringContent(payload, Encoding.UTF8, "text/plain");
        var response = await SendRequestAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogInformation($"Status Code : {response.StatusCode}, Response : {responseText}");
            return new IciciQRDto() { Success = false };
        }


        string responseJson = DecryptData(responseText);
        logger.LogInformation(responseJson);

        IciciInitResponse initResponse = JsonConvert.DeserializeObject<IciciInitResponse>(responseJson) ?? new();

        if (!Convert.ToBoolean(initResponse.success))
            return new IciciQRDto() { Success = false, Message = initResponse.message };

        string upiString = $"upi://pay?pa={iciciConfig.PayeeVPA}&pn={iciciConfig.MerchantName}&tr={initResponse.refId}&am={amount.ToString("0.00")}&cu=INR&mc={iciciConfig.MCC}";
        logger.LogInformation(upiString);
        string qrCode = qrCodeGenerator.CreateQRfromUPIstring(upiString);

        return new IciciQRDto() { Success = true, DisplayName = iciciConfig.MerchantName, RefId = initResponse.refId, QRbase64png = qrCode };
    }

    public async Task<IciciStatusDto> CheckStatusAsync(string orderNumber)
    {
        IciciTransactionStatusRequest statusRequest = new IciciTransactionStatusRequest
        {
            merchantId = iciciConfig.MerchantId,
            subMerchantId = iciciConfig.MerchantId,
            terminalId = iciciConfig.MCC.ToString(),
            merchantTranId = orderNumber
        };

        string requestJson = JsonConvert.SerializeObject(statusRequest);
        logger.LogInformation(requestJson);

        string payload = GetEncryptedPayload(requestJson);
        string url = $"api/MerchantAPI/UPI/v0/TransactionStatus3/{iciciConfig.MerchantId}";
        var content = new StringContent(payload, Encoding.UTF8, "text/plain");
        var response = await SendRequestAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogInformation($"Status Code : {response.StatusCode}, Response : {responseText}");
            return new IciciStatusDto() { Success = false, Status = "PENDING" };
        }

        string responseJson = DecryptData(responseText);
        logger.LogInformation(responseJson);

        IciciTransactionStatusResponse statusResponse = JsonConvert.DeserializeObject<IciciTransactionStatusResponse>(responseJson) ?? new();
        return new IciciStatusDto() { Success = Convert.ToBoolean(statusResponse.success), Message = statusResponse.message, Status = statusResponse.status, OriginalBankRRN = statusResponse.OriginalBankRRN };
    }

    public async Task<RefundDto> RefundAsync(string orderNumber, string bankRRN, decimal refundAmount)
    {
        string refundId = $"R{orderNumber}";

        IciciRefundRequest refundRequest = new()
        {
            merchantId = iciciConfig.MerchantId,
            subMerchantId = iciciConfig.MerchantId,
            terminalId = iciciConfig.MCC.ToString(),
            originalBankRRN = bankRRN,
            merchantTranId = refundId,
            originalmerchantTranId = orderNumber,
            refundAmount = refundAmount.ToString("0.00"),
            payeeVA = iciciConfig.PayeeVPA,
            onlineRefund = "Y",
            note = "Refund for failed order"
        };

        string requestJson = JsonConvert.SerializeObject(refundRequest);
        logger.LogInformation(requestJson);

        string payload = GetEncryptedPayload(requestJson);
        string url = $"api/MerchantAPI/UPI/v0/Refund/{iciciConfig.MerchantId}";
        var content = new StringContent(payload, Encoding.UTF8, "text/plain");
        var response = await SendRequestAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();



        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogInformation($"Status Code : {response.StatusCode}, Response : {responseText}");

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                logger.LogInformation(DecryptData(responseText));
            }

            return new RefundDto() { Success = false };
        }



        string responseJson = DecryptData(responseText);
        logger.LogInformation(responseJson);

        IciciRefundResponse refundResponse = JsonConvert.DeserializeObject<IciciRefundResponse>(responseJson) ?? new();
        return new RefundDto()
        {
            Success = Convert.ToBoolean(refundResponse.success),
            Message = refundResponse.message,
            OriginalBankRRN = refundRequest.originalBankRRN,
            Status = refundResponse.success
        };
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string relativeURL, HttpContent? content = null, HttpMethod? method = null)
    {
        if (method is null)
            method = HttpMethod.Post;

        HttpRequestMessage httpRequest = new HttpRequestMessage(method, new Uri($"{iciciConfig.BaseURL}/{relativeURL}", UriKind.Absolute));
        httpRequest.Headers.Add("accept", "*/*");
        httpRequest.Headers.Add("accept-encoding", "*");
        httpRequest.Headers.Add("accept-language", "en-US,en;q=0.8,hi;q=0.6");
        httpRequest.Headers.Add("cache-control", "no-cache");

        if (content is not null)
            httpRequest.Content = content;

        return await httpClient.SendAsync(httpRequest);
    }

    public string GetEncryptedPayload(string plainText)
    {
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        string iciciPublicKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iciciConfig.ICICIPublicKeyPath);
        X509Certificate2 cert = new X509Certificate2(iciciPublicKeyPath);
        var rsa = cert.GetRSAPublicKey();

        if (rsa == null)
            return string.Empty;

        byte[] encryptedBytes = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string DecryptData(string encryptedText)
    {
        var encryptedbytes = Convert.FromBase64String(encryptedText);

        string merchantPrivateKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iciciConfig.MerchantPrivateKeyPath);
        var str = File.ReadAllText(merchantPrivateKeyPath);
        var rsa = RSA.Create();
        rsa.ImportFromPem(str.ToCharArray());
        var decryptedbytes = rsa.Decrypt(encryptedbytes, RSAEncryptionPadding.Pkcs1);
        string decyptedText = Encoding.UTF8.GetString(decryptedbytes);
        return decyptedText;
    }
}
