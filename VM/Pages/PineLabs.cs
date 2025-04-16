//using System;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using System.Windows;
//using System.Windows.Interop;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//namespace VM.Pages;

//// TODO - move these to DB and access them via accessor patten -> DTOs -> objects.
//public partial class CardPaymentPage
//{
//    ///<summary> See relevant docs <see href="https://developer.pinelabs.com/in/instore/cloud-integration">here</see> before making changes. </summary>
//    private JObject _makeTxnRequestBaseJson = new()
//    {
//        { "MerchantID", "29610" },
//        { "SecurityToken", "a4c9741b-2889-47b8-be2f-ba42081a246e" },
//        { "ClientId","1012631" },
//        { "StoreId", "1221258" },
//        { "UserID", "bvcTester" },
//        { "SequenceNumber", "1"},
//        { "AutoCancelDurationInMinutes", "3" },
//        { "AllowedPaymentMode", "1" }
//    };

//    private JObject _txnBaseJson = new()
//    {
//        { "MerchantID", "29610" },
//        { "SecurityToken", "a4c9741b-2889-47b8-be2f-ba42081a246e" },
//        { "ClientId","1012631" },
//        { "StoreId", "1221258" }
//    };

//    private string? _transactionId;
//    private string? _plTxnRefId = string.Empty;
//    private int _plPaymentStatus = -1;
//    private string _pineLabsUrl = "https://www.plutuscloudserviceuat.in:8201/API/CloudBasedIntegration/V1";
//    private JArray? _transactionData;


//    private async void DialogHost_Loaded(object sender, RoutedEventArgs e)
//    {
//        try
//        {
         
//            var scope = serviceProvider.CreateScope();
//            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

//            if (!environment.IsProduction()) return;

//            // Setup only iff PROD.
//            _pineLabsUrl = "https://www.plutuscloudservice.in:8201/API/CloudBasedIntegration/V1";

//            _makeTxnRequestBaseJson = new JObject
//            {
//                { "MerchantID", "803024" },
//                { "SecurityToken", "015946c5-9e82-416c-825c-abc1e2fa2fcc" },
//                { "ClientId", "3673294" },
//                { "StoreId", "1366881" },
//                { "UserID", "bvcCustomer" },
//                { "SequenceNumber", "1"},
//                { "AutoCancelDurationInMinutes", "3" },
//                { "AllowedPaymentMode", "1" }
//            };

//            _txnBaseJson = new JObject
//            {
//                { "MerchantID", "803024" },
//                { "SecurityToken", "015946c5-9e82-416c-825c-abc1e2fa2fcc" },
//                { "ClientId","3673294" },
//                { "StoreId", "1366881" }
//            };
//        }
//        catch (Exception exception)
//        {
//            logger.LogError(exception, "dialogHost failed");
//        }
//    }



//    private async Task<JObject> PostRequest(string url, JObject json)
//    {
//        try
//        {
//            HttpContent content =
//                new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8);
//            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");


//            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
//            request.Headers.Add("User-Agent", "BvcVmApp");

//            var httpResponseMessage = await httpClient.SendAsync(request);
//            var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
//            logger.LogInformation("RESP content :  {rC}", responseContent);


//            if (!httpResponseMessage.IsSuccessStatusCode)
//            {
//                logger.LogWarning("Received non-success status code: {statusCode}", httpResponseMessage.StatusCode);
//                return new JObject { ["status"] = "ISE", ["HTTPStatusCode"] = $"{httpResponseMessage.StatusCode}" };
//            }
//            if (string.IsNullOrEmpty(responseContent))
//            {
//                logger.LogError("RECV Empty response from pine labs - sending empty json");
//                return new JObject { ["status"] = "EmptyResponse" };
//            }

//            var jsonResponse = JObject.Parse(responseContent);
//            return jsonResponse;

//        }
//        catch (HttpRequestException exception)
//        {
//            logger.LogError(exception, "HTTP Request error - Returning Service Unavailable response.");
//            return new JObject { ["status"] = "ServiceUnavailable" };
//        }
//        catch (Exception exception)
//        {
//            logger.LogError("Internal Server Error during transaction due to {e} and {st} - returning empty JObject",
//                exception.Message, exception.StackTrace);
//            return new JObject { ["status"] = "ISE" };
//        }
//    }

//    // on clicking back button when payment is pending on pine machine is awaiting card / when timer expired
//   private async Task CancelTxn(string? txnRefId)
//{
//    try
//    {
//        var requestUrl = $"{_pineLabsUrl}/CancelTransaction";
//        _txnBaseJson["PlutusTransactionReferenceID"] = txnRefId;
//        _txnBaseJson["Amount"] = (orderTotal * 100).ToString();  // Convert amount to paisa (multiply by 100)
//        logger.LogInformation("Canceling transaction with request: {s}", _txnBaseJson.ToString());

//        var cancelTxnResponse = await PostRequest(requestUrl, _txnBaseJson);
//        logger.LogInformation("Received response for cancel transaction: {jsR}", cancelTxnResponse.ToString());

//        _plPaymentStatus = (int)(cancelTxnResponse["ResponseCode"] ?? -1); // Assign INVALID STATUS if not returned 
//        logger.LogInformation("Payment status after cancel: {pS} for transaction reference: {rf}", _plPaymentStatus, _plTxnRefId);
//    }     
//    catch (Exception exception)
//    {
//        logger.LogError(exception, "Error while canceling the transaction: {m}", exception.Message);
//    }
//}

//    private async Task<string?> ChargeTxn(string orderNumber)
//    {
//        try
//        {
//            lblDisplayName.Text = "Transaction Initiated";
//        _makeTxnRequestBaseJson["TransactionNumber"] = orderNumber;
//        _makeTxnRequestBaseJson["Amount"] = orderTotal .ToString(); // Convert to paisa (multiply by 100)
//        logger.LogInformation("Initiating charge for transaction: {tN} with amount: {a} paisa",
//            _makeTxnRequestBaseJson["TransactionNumber"]?.ToString(), _makeTxnRequestBaseJson["Amount"]?.ToString());

//        var requestUrl = $"{_pineLabsUrl}/UploadBilledTransaction";
//        var txnResponse = await PostRequest(requestUrl, _makeTxnRequestBaseJson);
//        logger.LogInformation("Received response for transaction charge: {jsR}", txnResponse.ToString());

//        if (txnResponse["ResponseCode"]?.ToString() == "0" && 
//            string.Equals(txnResponse["ResponseMessage"]?.ToString(), "APPROVED", StringComparison.OrdinalIgnoreCase))
//        {
//            _plTxnRefId = txnResponse["PlutusTransactionReferenceID"]?.ToString();
//            logger.LogInformation("Transaction Reference ID assigned: {rf}", _plTxnRefId);
//            return _plTxnRefId;
//        }

//        logger.LogWarning("Transaction not approved or response code is invalid. Response: {response}", txnResponse.ToString());
//        return string.Empty;
//    }
//        catch (Exception exception)
//        {
//            logger.LogError(exception, "Error while charging the transaction: {m}", exception.Message);
//            return string.Empty;
//        }
//        finally
//        {
//            logger.LogInformation("ChargeTxn completed. _plTxnRefId: {plTxnRefId}", _plTxnRefId);
//        }
//    }

//private async Task<int> GetTxnStatus(string? refId)
//{
//    try
//    {
//        var requestUrl = $"{_pineLabsUrl}/GetCloudBasedTxnStatus";
//        _txnBaseJson["PlutusTransactionReferenceID"] = refId;
//        logger.LogInformation("Getting transaction status for: {s}", _txnBaseJson.ToString());

//        var txnResponse = await PostRequest(requestUrl, _txnBaseJson);
//        logger.LogInformation("Received transaction status response: {jsR}", txnResponse.ToString());

//        _plPaymentStatus = (int)(txnResponse["ResponseCode"] ?? -1); // Set INVALID STATUS if nothing returned 
//        logger.LogInformation("Transaction status code: {pS} from response: {resp}", _plPaymentStatus, txnResponse.ToString());

//            _transactionData = txnResponse["TransactionData"] as JArray;
//            if (_transactionData != null)
//            {
//                foreach (var data in _transactionData)
//                {
//                    if (data["Tag"]?.ToString() != "TransactionLogId") continue;
//                    var transactionLogId = data["Value"]?.ToString();
//                    _transactionId = transactionLogId;
//                    break;
//                }
//            }
//            return _plPaymentStatus;
//    }
//    catch (Exception exception)
//    {
//        logger.LogError(exception, "Error while getting transaction status: {m}", exception.Message);
//            return -1;
//    }
//}


//    // UploadBilledTransaction - call void only on COMPLETE vending failure.
//    private async Task<bool> VoidTxn(string txnRefId)
//    {
//        var requestUrl = $"{_pineLabsUrl}/UploadBilledTransaction";
//        _makeTxnRequestBaseJson["TransactionNumber"] = orderNumber;
//        _makeTxnRequestBaseJson["Amount"] = orderTotal.ToString();
//        _makeTxnRequestBaseJson["OriginalPlutusTransactionReferenceID"] = txnRefId;
//        _makeTxnRequestBaseJson["TxnType"] = "1";

//        var jsonResponse = await PostRequest(requestUrl, _makeTxnRequestBaseJson);
//        logger.LogInformation("RECV VOID JSON RESP {jsR}", jsonResponse.ToString());

//        var voidRefId = jsonResponse["PlutusTransactionReferenceID"]?.ToString();
//        logger.LogInformation("txnRefId: {tr} had been voided voidRefId: {rf}", txnRefId, voidRefId);

//        return jsonResponse["ResponseCode"]?.ToString() == "0";
//    }

//    // UploadBilledTransaction - call refund ONLY on PARTIAL vending COMPLETION and PARTIAL vending FAILURE.
//    public async Task<int> RefundCardTxn(double balance, string txnRefId,string refundMessage)
//    {
//        var requestUrl = $"{_pineLabsUrl}/UploadBilledTransaction";
//        logger.LogInformation("Requesting refund from URL {url}", requestUrl);

//        _makeTxnRequestBaseJson["TransactionNumber"] = orderNumber + "R"; // Append 'R' to indicate refund transaction
//        _makeTxnRequestBaseJson["Amount"] = balance.ToString();
//        _makeTxnRequestBaseJson["TransactionId"] = _transactionId;
//        _makeTxnRequestBaseJson["TxnType"] = "3"; // Assuming '3' indicates a refund transaction

//        try
//        {
//            var jsonResponse = await PostRequest(requestUrl, _makeTxnRequestBaseJson);
//            logger.LogInformation("Received refund response JSON: {jsR}", jsonResponse.ToString());

//            return (int)(jsonResponse["ResponseCode"] ?? -1);
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "Error processing refund for order {orderNumber}", orderNumber);
//            return -1;
//        }
//    }

//}


///*
 
//for prod / live device: 
 
//curl --request POST --url https://www.plutuscloudservice.in:8201/API/CloudBasedIntegration/V1/UploadBilledTransaction \
//  --header 'content-type: application/json' \
//    --data '{"TransactionNumber":"060125","SequenceNumber": "6125","AllowedPaymentMode": "1", \
//    "Amount": "100","UserID": "testUser21252","MerchantID":"803024", \
//    "SecurityToken": "015946c5-9e82-416c-825c-abc1e2fa2fcc","ClientId":"3673294", \
//    "StoreId":"1366881","AutoCancelDurationInMinutes":"2"}'

//Prod/live device
//{
//    "TransactionNumber":"060125",
//    "SequenceNumber": "6125",
//    "AllowedPaymentMode": "1",
//    "Amount": "100",
//    "UserID": "testUser21252",
//    "MerchantID":"803024",
//    "SecurityToken": "015946c5-9e82-416c-825c-abc1e2fa2fcc",
//    "ClientId":"3673294",
//    "StoreId":"1366881",
//    "AutoCancelDurationInMinutes":"2"
//}

//curl --request POST --url https://www.plutuscloudservice.in:8201/API/CloudBasedIntegration/V1/GetCloudBasedTxnStatus \
// --header 'content-type: application/json'  --data  \
//'{ 
//    "MerchantID": "803024", 
//    "SecurityToken": "015946c5-9e82-416c-825c-abc1e2fa2fcc", 
//    "ClientId": "3673294", 
//    "StoreId": "1366881",
//    "PlutusTransactionReferenceID":"391988779"
//}'


//---- TEST / UAT ---- 

//curl --request POST --url https://www.plutuscloudserviceuat.in:8201/API/CloudBasedIntegration/V1/UploadBilledTransaction --header 'content-type: application/json'  --data '{"TransactionNumber":"UP1234203151231357","SequenceNumber": "1","AllowedPaymentMode": "1","Amount": "1000","UserID": "cool","MerchantID":"29610","SecurityToken": "a4c9741b-2889-47b8-be2f-ba42081a246e","ClientId":"1012631","StoreId":"1221258","AutoCancelDurationInMinutes":"3"}'

//{"ResponseCode":0,"ResponseMessage":"APPROVED","PlutusTransactionReferenceID":658845,"AdditionalInfo":null}09:42:29 sdk@sdkUbu ~ → 
//09:42:58 sdk@sdkUbu ~ → 
//09:42:58 sdk@sdkUbu ~ → curl --request POST --url https://www.plutuscloudserviceuat.in:8201/API/CloudBasedIntegration/V1/GetCloudBasedTxnStatus --header 'content-type: application/json'  --data  '{"MerchantID": "29610", "SecurityToken": "a4c9741b-2889-47b8-be2f-ba42081a246e", "ClientId": "1012631", "StoreId": "1221258","PlutusTransactionReferenceID": "658746" }'
//{"ResponseCode":1,"ResponseMessage":"INVALID PLUTUS TXN REF ID","PlutusTransactionReferenceID":658746,"TransactionData":null}09:43:24 sdk@sdkUbu ~ → 
//09:43:25 sdk@sdkUbu ~ → 
//09:43:25 sdk@sdkUbu ~ → curl --request POST --url https://www.plutuscloudserviceuat.in:8201/API/CloudBasedIntegration/V1/GetCloudBasedTxnStatus --header 'content-type: application/json'  --data  '{"MerchantID": "29610", "SecurityToken": "a4c9741b-2889-47b8-be2f-ba42081a246e", "ClientId": "1012631", "StoreId": "1221258","PlutusTransactionReferenceID": "658845" }'
//{"ResponseCode":1001,"ResponseMessage":"TXN UPLOADED","PlutusTransactionReferenceID":658845,"TransactionData":null}09:43:37 sdk@sdkUbu ~ → 
//09:43:43 sdk@sdkUbu ~ → 

//01:06:52 sdk@sdkUbu Downloads → curl --request POST --url https://www.plutuscloudserviceuat.in:8201/API/CloudBasedIntegration/V1/GetCloudBasedTxnStatus --header 'content-type: application/json'  --data  '{"MerchantID": "29610", "SecurityToken": "a4c9741b-2889-47b8-be2f-ba42081a246e", "ClientId": "1012631", "StoreId": "1221258","PlutusTransactionReferenceID": "663352" }'
//{"ResponseCode":0,"ResponseMessage":"TXN APPROVED","PlutusTransactionReferenceID":663352,"TransactionData":[{"Tag":"TID","Value":"10126317"},{"Tag":"MID","Value":"               "},{"Tag":"PaymentMode","Value":"CARD"},{"Tag":"Amount","Value":"90.00"},{"Tag":"BatchNumber","Value":"7"},{"Tag":"RRN","Value":"000020"},{"Tag":"ApprovalCode","Value":"00"},{"Tag":"Invoice Number","Value":"20"},{"Tag":"Card Number","Value":"************0717"},{"Tag":"Expiry Date","Value":"XXXX"},{"Tag":"Card Type","Value":"VISA"},{"Tag":"Acquirer Id","Value":"02"},{"Tag":"Acquirer Name","Value":"ICICI"},{"Tag":"Transaction Date","Value":"10012025"},{"Tag":"Transaction Time","Value":"130445"},{"Tag":"AmountInPaisa","Value":"9000"},{"Tag":"OriginalAmount","Value":"9000"},{"Tag":"FinalAmount","Value":"9000"},{"Tag":"IsPartialPayByPointsTxn","Value":"False"},{"Tag":"TransactionLogId","Value":"4295483177"},{"Tag":"Currency Type","Value":"INR"}]}
//01:07:16 sdk@sdkUbu Downloads → 
//*/