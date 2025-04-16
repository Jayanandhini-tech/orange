using CMS.API.Payments.Dtos;
using CMS.API.Payments.Dtos.Gateway;
using CMS.API.Payments.Dtos.Gateway.ICICI;
using CMS.API.Payments.Services.Interface;
using CMS.Dto.Payments;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CMS.API.Payments.Services;

public class IciciService : IPaymentService
{
    private readonly PaymentGatewayConfigDto config;
    private readonly HttpClient httpClient;  
    private readonly ILogger<IciciService> logger;

    public string Gateway { get { return "ICICI"; } }

    public IciciService(PaymentGatewayConfigDto config, HttpClient httpClient,  ILogger<IciciService> logger)
    {
        this.config = config;
        this.httpClient = httpClient;       
        this.logger = logger;
    }

    public async Task<UPICreateResponse> CreateQRAsync(string orderNumber, double amount, string machineId)
    {
        IciciInitTransaction init = new IciciInitTransaction()
        {
            merchantId = config.MerchantId,
            terminalId = config.Mcc.ToString(),
            merchantTranId = orderNumber,
            billNumber = orderNumber,
            amount = amount.ToString("0.00")
        };

        string requestJson = JsonConvert.SerializeObject(init);
        logger.LogInformation(requestJson);

        string payload = GetEncryptedPayload(requestJson);
        //  string url = $"api/MerchantAPI/UPI/v0/QR3/{iciciConfig.MerchantId}"; For UAT 
        string url = $"api/MerchantAPI/UPI/v0/QR/{config.MerchantId}";
        var content = new StringContent(payload, Encoding.UTF8, "text/plain");
        var response = await SendRequestAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogInformation($"Status Code : {response.StatusCode}, Response : {responseText}");
            return new UPICreateResponse() { Success = false, Message = "Unable to generate QR" };
        }


        string responseJson = DecryptData(responseText);
        logger.LogInformation(responseJson);

        IciciInitResponse initResponse = JsonConvert.DeserializeObject<IciciInitResponse>(responseJson) ?? new();

        if (!Convert.ToBoolean(initResponse.success))
            return new UPICreateResponse() { Success = false, Message = "Unable to generate QR" };

        string upiString = $"upi://pay?pa={config.MerchantVPA}&pn={config.MerchantName}&tr={initResponse.refId}&am={amount.ToString("0.00")}&cu=INR&mc={config.Mcc}&tn={orderNumber}";

        return new UPICreateResponse() { Success = true, UpiString = upiString, TransactionId = initResponse.refId, DisplayName = config.MerchantName };
    }

    public async Task<UPIStatusResponse> StatusAsync(string orderNumber)
    {
        IciciTransactionStatusRequest statusRequest = new IciciTransactionStatusRequest
        {
            merchantId = config.MerchantId,
            subMerchantId = config.MerchantId,
            terminalId = config.Mcc.ToString(),
            merchantTranId = orderNumber
        };

        string requestJson = JsonConvert.SerializeObject(statusRequest);
        logger.LogInformation(requestJson);

        string payload = GetEncryptedPayload(requestJson);
        string url = $"api/MerchantAPI/UPI/v0/TransactionStatus3/{config.MerchantId}";
        var content = new StringContent(payload, Encoding.UTF8, "text/plain");
        var response = await SendRequestAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogInformation($"Status Code : {response.StatusCode}, Response : {responseText}");
            return new UPIStatusResponse() { Success = false, Status = PaymentStatusCode.PENDING, Message = "Unable check the status" };
        }

        string responseJson = DecryptData(responseText);
        logger.LogInformation(responseJson);

        IciciTransactionStatusResponse statusResponse = JsonConvert.DeserializeObject<IciciTransactionStatusResponse>(responseJson) ?? new();

        var status = statusResponse.status switch
        {
            "PENDING" => PaymentStatusCode.PENDING,
            "SUCCESS" => PaymentStatusCode.SUCCESS,
            _ => PaymentStatusCode.FAILED,
        };

        return new UPIStatusResponse() { Success = Convert.ToBoolean(statusResponse.success), Status = status, BankRRR = statusResponse.OriginalBankRRN, Message = statusResponse.message };

    }

    public async Task<UPIRefundResponse> RefundAsync(string orderNumber, string transactionIdorRefId, double refundAmount, string msg, string bankRRN = "")
    {
        string refundId = $"R{orderNumber}";

        IciciRefundRequest refundRequest = new()
        {
            merchantId = config.MerchantId,
            subMerchantId = config.MerchantId,
            terminalId = config.Mcc.ToString(),
            originalBankRRN = bankRRN,
            merchantTranId = refundId,
            originalmerchantTranId = orderNumber,
            refundAmount = refundAmount.ToString("0.00"),
            payeeVA = config.MerchantVPA,
            onlineRefund = "Y",
            note = msg
        };

        string requestJson = JsonConvert.SerializeObject(refundRequest);
        logger.LogInformation(requestJson);

        string payload = GetEncryptedPayload(requestJson);
        string url = $"api/MerchantAPI/UPI/v0/Refund/{config.MerchantId}";
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

            return new UPIRefundResponse() { Success = false, RefundId = refundId };
        }



        string responseJson = DecryptData(responseText);
        logger.LogInformation(responseJson);

        IciciRefundResponse refundResponse = JsonConvert.DeserializeObject<IciciRefundResponse>(responseJson) ?? new();

        return new UPIRefundResponse()
        {
            Success = Convert.ToBoolean(refundResponse.success),
            RefundId = refundId,
            Status = refundResponse.status,
            RefundRefId = refundResponse.originalBankRRN
        };
    }



    private async Task<HttpResponseMessage> SendRequestAsync(string relativeURL, HttpContent? content = null, HttpMethod? method = null)
    {
        if (method is null)
            method = HttpMethod.Post;

        HttpRequestMessage httpRequest = new HttpRequestMessage(method, new Uri($"{config.BaseURL}/{relativeURL}", UriKind.Absolute));
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
        string iciciPublicKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.PublicKeyPath!);
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

        string merchantPrivateKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.PrivateKeyPath!);
        var str = File.ReadAllText(merchantPrivateKeyPath);
        var rsa = RSA.Create();
        rsa.ImportFromPem(str.ToCharArray());
        var decryptedbytes = rsa.Decrypt(encryptedbytes, RSAEncryptionPadding.Pkcs1);
        string decyptedText = Encoding.UTF8.GetString(decryptedbytes);
        return decyptedText;
    }

}
