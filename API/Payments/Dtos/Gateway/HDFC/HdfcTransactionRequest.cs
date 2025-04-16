namespace CMS.API.Payments.Dtos.Gateway.HDFC;

public class HdfcTransactionRequest
{
    public string pgMerchantId { get; set; } = string.Empty;
    public string requestMsg { get; set; } = string.Empty;
}


public record HdfcTransactionResponse(string TxnId, string OrderNumber, decimal Amount, string AuthDate,
    string Status, string Description, string ResponseCode, string ApprovalNumber,
    string PayerVPA, string CustomerReferenceNo, string ReferenceId, string PayerBankDetails, string PayType);


public class HdfcRefundResponse
{
    public string TxnId { get; set; } = string.Empty;
    public string RefundId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AuthDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string ApprovalNumber { get; set; } = string.Empty;
    public string PayerVPA { get; set; } = string.Empty;
    public string CustomerReferenceNo { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
}
