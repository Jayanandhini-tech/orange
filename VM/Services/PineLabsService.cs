using CMS.Dto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Services;

public class PineLabsService : IPineLabsService
{
    private readonly ILogger<PineLabsService> logger;
    private readonly HttpClient httpClient;

    public PineLabsService(ILogger<PineLabsService> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    ///<summary> See relevant docs <see href="https://developer.pinelabs.com/in/instore/cloud-integration">here</see> before making changes. </summary>

    private JObject _makeTxnRequestBaseJson(string paymentType)
    {
        logger.LogInformation("Payment Type in _makeTxnRequestBaseJson: {PaymentType}", paymentType);
        string allowedPaymentMode = paymentType == "UPI" ? "10" : "1";

        var txnRequest = new JObject
        {
            //{ "MerchantID", "803024" },
            //{ "SecurityToken", "015946c5-9e82-416c-825c-abc1e2fa2fcc" },
            //{ "ClientId", "3673294" },
            //{ "StoreId", "1366881" },
            //{ "UserID", "bvcCustomer" },
            //{ "SequenceNumber", "1"},
            //{ "AutoCancelDurationInMinutes", "3" },
            //{ "AllowedPaymentMode", allowedPaymentMode }

            { "MerchantID", "29610" },
            { "SecurityToken", "a4c9741b-2889-47b8-be2f-ba42081a246e" },
            { "ClientId","1012631" },
            { "StoreId", "1221258" },
            { "UserID", "bvcTester" },
            { "SequenceNumber", "1"},
            { "AutoCancelDurationInMinutes", "3" },
            { "AllowedPaymentMode", allowedPaymentMode }

        //{ "MerchantID", "827228" },
        //{ "SecurityToken", "f46cba4b-7bcd-4597-9bc5-cf2e92532dd4" },
        //{ "ClientId","3710101" },
        //{ "StoreId", "1376989" },
        //{ "UserID", "Lilli - A Unit of HariBhavanam" },
        //{ "SequenceNumber", "1"},
        //{ "AutoCancelDurationInMinutes", "3" },
        //{ "AllowedPaymentMode",  allowedPaymentMode }


        //{ "MerchantID", "827228" },
        //{ "SecurityToken", "f46cba4b-7bcd-4597-9bc5-cf2e92532dd4" },
        //{ "ClientId","3673294" },
        //{ "StoreId", "1366881" },
        //{ "UserID", "Bvc24" },
        //{ "SequenceNumber", "1"},
        //{ "AutoCancelDurationInMinutes", "3" },
        //{ "AllowedPaymentMode",  allowedPaymentMode }


            };


        if (paymentType == "UPI")
        {
            txnRequest["BankCode"] = "2";  // ✅ Extra field only for UPI transactions
        }

        return txnRequest;
    }

    private JObject _txnBaseJson = new()
    {
        //{ "MerchantID", "803024" },
        //{ "SecurityToken", "015946c5-9e82-416c-825c-abc1e2fa2fcc" },
        //{ "ClientId", "3673294" },
        //{ "StoreId", "1366881" },

        { "MerchantID", "29610" },
        { "SecurityToken", "a4c9741b-2889-47b8-be2f-ba42081a246e" },
        { "ClientId","1012631" },
        { "StoreId", "1221258" }

        //{ "MerchantID", "827228" },
        //{ "SecurityToken", "f46cba4b-7bcd-4597-9bc5-cf2e92532dd4" },
        //{ "ClientId","3710101" },
        //{ "StoreId", "1376989" },

    };

    private string? _transactionId;
    private string? _plTxnRefId;
    private int _plPaymentStatus = -1;
    private JArray? _transactionData;

    private string _pineLabsUrl = "https://www.plutuscloudserviceuat.in:8201/API/CloudBasedIntegration/V1";

    //private string _pineLabsUrl = "https://www.plutuscloudservice.in:8201/API/CloudBasedIntegration/V1";

    private string _pinecancelled = "https://www.plutuscloudserviceUAT.in:8201/API/CloudBasedIntegration/V1";

    private double orderTotal = 0;
    private string orderNumber = string.Empty;

    public async Task<JObject> PostRequest(string url, JObject json)
    {
        try
        {
            HttpContent content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            request.Headers.Add("User-Agent", "BvcVmApp");

            var httpResponseMessage = await httpClient.SendAsync(request);
            var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
            //logger.LogInformation("RESP content : {rC}", responseContent);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                logger.LogWarning("Received non-success status code: {statusCode}", httpResponseMessage.StatusCode);
                return new JObject { ["status"] = "ISE", ["HTTPStatusCode"] = $"{httpResponseMessage.StatusCode}" };
            }
            if (string.IsNullOrEmpty(responseContent))
            {
                logger.LogError("RECV Empty response from pine labs - sending empty json");
                return new JObject { ["status"] = "EmptyResponse" };
            }

            var jsonResponse = JObject.Parse(responseContent);
            return JObject.Parse(responseContent);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "HTTP Request error - Returning Service Unavailable response.");
            return new JObject { ["status"] = "ServiceUnavailable" };
        }

        catch (Exception exception)
        {
            logger.LogError("Internal Server Error during transaction due to {e} and {st} - returning empty JObject",
                exception.Message, exception.StackTrace);
            return new JObject { ["status"] = "ISE" };
        }

    }

    public async Task<int> GetTxnStatus(string? refId)
    {
        try
        {
            var requestUrl = $"{_pineLabsUrl}/GetCloudBasedTxnStatus";
            _txnBaseJson["PlutusTransactionReferenceID"] = refId;
            //logger.LogInformation("Getting transaction status for: {s}", _txnBaseJson.ToString());

            var txnResponse = await PostRequest(requestUrl, _txnBaseJson);
            //logger.LogInformation("Received transaction status response: {jsR}", txnResponse.ToString());

            _plPaymentStatus = (int)(txnResponse["ResponseCode"] ?? -1);
            //logger.LogInformation("Transaction status code: {pS} from response: {resp}", _plPaymentStatus, txnResponse.ToString());

            _transactionData = txnResponse["TransactionData"] as JArray;
            if (_transactionData != null)
            {
                foreach (var data in _transactionData)
                {
                    if (data["Tag"]?.ToString() != "TransactionLogId") continue;
                    var transactionLogId = data["Value"]?.ToString();
                    _transactionId = transactionLogId;
                    break;
                }
            }

            return _plPaymentStatus;
        }
        catch (Exception exception) 
        {
            logger.LogError(exception, "Error while getting transaction status: {m}", exception.Message);
            return -1;
        }
    }


    public async Task<string?> ChargeTxn(string orderNumber, double orderTotal, string paymentType)
    {
        try
        {
            var txnRequest = _makeTxnRequestBaseJson(paymentType);
            txnRequest["TransactionNumber"] = orderNumber;
            //logger.LogInformation($"TransactionNumber:{orderNumber}");
            txnRequest["Amount"] = orderTotal * 100;

            logger.LogInformation("Initiating charge for transaction: {tN} with amount: {a} paisa",
                txnRequest["TransactionNumber"]?.ToString(), txnRequest["Amount"]?.ToString());

            var requestUrl = $"{_pineLabsUrl}/UploadBilledTransaction";
            var txnResponse = await PostRequest(requestUrl, txnRequest);
            //logger.LogInformation("Received response for transaction charge: {jsR}", txnResponse.ToString());

            if (txnResponse["ResponseCode"]?.ToString() == "0" &&
                string.Equals(txnResponse["ResponseMessage"]?.ToString(), "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                _plTxnRefId = txnResponse["PlutusTransactionReferenceID"]?.ToString();
                logger.LogInformation("Transaction Reference ID assigned: {rf}", _plTxnRefId);
                return _plTxnRefId;
            }

            logger.LogWarning("Transaction not approved or response code is invalid. Response: {response}", txnResponse.ToString());
            return string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while charging the transaction: {m}", exception.Message);
            return string.Empty;
        }
       }
   
    
    public async Task CancelTxn(string? txnRefId)
    {
        try
        {
            var requestUrl = $"{_pineLabsUrl}/CancelTransaction";
            _txnBaseJson["PlutusTransactionReferenceID"] = txnRefId;
            _txnBaseJson["Amount"] = (orderTotal*100).ToString();
            //logger.LogInformation("get txn status for cancel: {s}", _txnBaseJson.ToString());

            var cancelTxnResponse = await PostRequest(requestUrl, _txnBaseJson);
            //logger.LogInformation("RECV RESP for Cancel JSON {jsR}", cancelTxnResponse.ToString());

            _plPaymentStatus = (int)(cancelTxnResponse["ResponseCode"] ?? -1); // set INVALID STATUS -1 on nothing returned 
            logger.LogInformation("_plPaymentStatus assigned: {pS} for {rf}", _plPaymentStatus, _plPaymentStatus);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while getting the txn status due to {m}", exception.Message);
        }
    }

    public async Task ForcedCancelTxn(string? txnRefId)
    {
        try
        {
            var requestUrl = $"{_pinecancelled}/CancelTransactionForced";
            _txnBaseJson["PlutusTransactionReferenceID"] = txnRefId;
            _txnBaseJson["Amount"] = (orderTotal * 100).ToString();
            //logger.LogInformation("get txn status for cancel: {s}", _txnBaseJson.ToString());

            var cancelTxnResponse = await PostRequest(requestUrl, _txnBaseJson);
            //logger.LogInformation("RECV RESP for Cancel JSON {jsR}", cancelTxnResponse.ToString());

            _plPaymentStatus = (int)(cancelTxnResponse["ResponseCode"] ?? -1); // set INVALID STATUS -1 on nothing returned 
            logger.LogInformation("_pl Forced cancelled: {pS} for {rf}", _plPaymentStatus, _plPaymentStatus);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error while getting the txn status failed due to {m}", exception.Message);
        }
    }


    public async Task<int> VoidCardTxn(string orderNumber, double balance, string reference, string msg, string paymentType)
    {
        var requestUrl = $"{_pineLabsUrl}/UploadBilledTransaction";
        var txnRequest = _makeTxnRequestBaseJson(paymentType);
        txnRequest["TransactionNumber"] = orderNumber;
        txnRequest["Amount"] = (balance * 100).ToString();
        txnRequest["OriginalPlutusTransactionReferenceID"] = reference;
        txnRequest["TxnType"] = "1";  // Void transaction

        //logger.LogInformation($"Void Transaction Request: {JsonConvert.SerializeObject(txnRequest)}");
        var jsonResponse = await PostRequest(requestUrl, txnRequest);
        //logger.LogInformation("RECV VOID JSON RESP {jsR}", jsonResponse.ToString());

        var responseCode = jsonResponse["ResponseCode"]?.ToString();
        var responseMessage = jsonResponse["ResponseMessage"]?.ToString();
        var voidRefId = jsonResponse["PlutusTransactionReferenceID"]?.ToString();

        logger.LogInformation("PaymentId: {tr} had been voided. VoidRefId: {rf}", reference, voidRefId);
        return responseCode == "0" ? 0 : -1;

    }

    public async Task<int> RefundCardTxn(string orderNumber, double balance, string msg, string paymentType)
    {
        var requestUrl = $"{_pineLabsUrl}/UploadBilledTransaction";
        var txnRequest = _makeTxnRequestBaseJson(paymentType);
        txnRequest["TransactionNumber"] = orderNumber;        
        txnRequest["Amount"] = (balance * 100).ToString();
        txnRequest["TransactionId"] = _transactionId;
        txnRequest["TxnType"] = "3"; // Refund transaction

        //logger.LogInformation($"Refund Transaction Request: {JsonConvert.SerializeObject(txnRequest)}");

        try
        {
            var jsonResponse = await PostRequest(requestUrl, txnRequest);
            logger.LogInformation("Received refund response JSON: {jsR}", jsonResponse.ToString());

            return (int)(jsonResponse["ResponseCode"] ?? -1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing refund for order {orderNumber}", orderNumber);
            return -1;
        }

    }
}
