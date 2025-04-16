namespace VM.Domains;

public class CashRefund
{
    public int Id { get; set; }
    public string RefId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Denomination { get; set; } = string.Empty;
    public DateTime CancelOn { get; set; }
    public bool IsViewed { get; set; }
}
