namespace VM.Domains;

public class Refill
{
    public required string Id { get; set; }
    public DateTime RefilledOn { get; set; }
    public int MotorNumber { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsViewed { get; set; }
}
