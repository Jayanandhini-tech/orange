using CMS.API.Payments.Dtos;
using CMS.API.Payments.Dtos.Gateway;
using CMS.API.Payments.Dtos.Gateway.HDFC;
using CMS.API.Payments.Services.Interface;
using CMS.Dto.Payments;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using UPIKit;

namespace CMS.API.Payments.Services;

public class HDFCService : IPaymentService
{
    public string Gateway { get { return "HDFC"; } }

    private readonly PaymentGatewayConfigDto config;
    private readonly HttpClient httpClient;
    private readonly ILogger<HDFCService> logger;

    public HDFCService(PaymentGatewayConfigDto config, HttpClient httpClient, ILogger<HDFCService> logger)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.logger = logger;
    }



    public async Task<UPICreateResponse> CreateQRAsync(string orderNumber, double amount, string machineId)
    {
        string qrUri = $"upi://pay?pa={config.MerchantVPA}&pn={config.MerchantName}&am={amount.ToString("0.00")}&tr={orderNumber}&tn={orderNumber}&mc={config.Mcc}&mode=23&cu=INR";
        await Task.CompletedTask;

        return new UPICreateResponse() { Success = true, UpiString = qrUri, TransactionId = "", DisplayName = config.MerchantName };
    }

    public async Task<UPIStatusResponse> StatusAsync(string orderNumber)
    {
        try
        {
            string parameters = $"{config.MerchantId}|{orderNumber}|||||||||||NA|NA";
            string encyptMsg = UPISecurity.Encrypt(parameters, config.MerchantKey);
            var statusReq = new HdfcTransactionRequest()
            {
                pgMerchantId = config.MerchantId,
                requestMsg = encyptMsg
            };

            string url = $"{config.BaseURL}/transactionStatusQuery";
            var post_data = new StringContent(JsonConvert.SerializeObject(statusReq), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, post_data);
            var responsetext = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError($"Status Response Code : {response.StatusCode}, body : {responsetext}");
                return new UPIStatusResponse() { Success = true, Status = PaymentStatusCode.PENDING, Message = "Unable check the status", TransactionId = "" };
            }

            var respMsg = UPISecurity.Decrypt(responsetext, config.MerchantKey);

            HdfcTransactionResponse? statusResp = ReadStatusResponse(respMsg);

            if (statusResp is null)
                return new UPIStatusResponse() { Success = true, Status = PaymentStatusCode.PENDING, Message = "", TransactionId = "" };

            var status = statusResp.Status switch
            {
                "SUCCESS" => PaymentStatusCode.SUCCESS,
                "FAILURE" => PaymentStatusCode.FAILED,
                "PENDING" => PaymentStatusCode.PENDING,
                _ => PaymentStatusCode.FAILED
            };

            return new UPIStatusResponse() { Success = true, Status = status, TransactionId = statusResp.TxnId, BankRRR = statusResp.CustomerReferenceNo, Message = $"{statusResp.PayerVPA}.{statusResp.PayerBankDetails}.{statusResp.Description}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return new UPIStatusResponse() { Success = true, Status = PaymentStatusCode.PENDING, Message = "Error in the request", TransactionId = "" };
        }
    }

    public async Task<UPIRefundResponse> RefundAsync(string orderNumber, string transactionIdorRefId, double refundAmount, string msg = "", string bankRRN = "")
    {
        string refundId = $"R-{orderNumber}";
        string parameters = $"{config.MerchantId}|{refundId}|{orderNumber}|{transactionIdorRefId}|{bankRRN}|orderrefund|{refundAmount}|INR|P2P|PAY|||||||||NA|NA";
        string encyptMsg = UPISecurity.Encrypt(parameters, config.MerchantKey);
        var refundReq = new HdfcTransactionRequest()
        {
            pgMerchantId = config.MerchantId,
            requestMsg = encyptMsg
        };

        string url = $"{config.BaseURL}/refundReqSvc";
        var post_data = new StringContent(JsonConvert.SerializeObject(refundReq), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, post_data);
        var responsetext = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogError($"Refund Response Code : {response.StatusCode}, body : {responsetext}");
            return new UPIRefundResponse() { Success = false, RefundId = refundId };
        }

        var respMsg = UPISecurity.Decrypt(responsetext, config.MerchantKey);
        HdfcRefundResponse? refundResponse = ReadRefundResponse(respMsg);

        if (refundResponse is null)
            return new UPIRefundResponse() { Success = false, RefundId = refundId };

        if (refundResponse.Status == "S" || refundResponse.Status == "SUCCESS")
            return new UPIRefundResponse() { Success = true, RefundId = refundId, Status = "SUCCESS", RefundRefId = refundResponse.TxnId };

        return new UPIRefundResponse() { Success = false, RefundId = refundId, Status = "", RefundRefId = "" };


    }



    private HdfcTransactionResponse? ReadStatusResponse(string response)
    {
        logger.LogInformation(response);
        string[] resp = response.Trim().Split('|');
        if (resp.Length != 21)
        {
            logger.LogError($"Issue with reading response : {response}");
            return null;
        }

        return new HdfcTransactionResponse(resp[0], resp[1], Convert.ToDecimal(resp[2]), resp[3], resp[4], resp[5], resp[6], resp[7], resp[8], resp[9], resp[10], resp[16], resp[17]);
    }


    private HdfcRefundResponse? ReadRefundResponse(string response)
    {
        logger.LogInformation(response);

        string[] resp = response.Trim().Split('|');
        if (resp.Length != 21)
            return null;

        var refundresponse = new HdfcRefundResponse()
        {
            TxnId = resp[0],
            RefundId = resp[1],
            Amount = Convert.ToDecimal(resp[2]),
            AuthDate = resp[3],
            Status = resp[4],
            Description = resp[5],
            ResponseCode = resp[6],
            ApprovalNumber = resp[7],
            PayerVPA = resp[8],
            CustomerReferenceNo = resp[9],
            ReferenceId = resp[10]
        };
        return refundresponse;
    }
}
