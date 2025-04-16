namespace VM.Services.Interfaces;

public interface IPineLabsService
{
    Task CancelTxn(string? txnRefId);

    Task<string?> ChargeTxn(string orderNumber, double orderTotal,string paymentType);

    Task<int> GetTxnStatus(string? refId);

    Task<int> VoidCardTxn( string orderNumber, double balance, string reference, string msg, string paymentType);

    Task<int> RefundCardTxn(string orderNumber, double balance, string msg, string paymentType);

   

    Task ForcedCancelTxn(string? txnRefId);

}
