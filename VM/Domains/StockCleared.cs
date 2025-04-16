namespace VM.Domains;

public class StockCleared
{
    public required string Id { get; set; }
    public DateTime ClearedOn { get; set; }
    public int MotorNumber { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsViewed { get; set; }
}
