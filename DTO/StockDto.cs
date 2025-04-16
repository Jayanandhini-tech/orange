namespace CMS.Dto;

public record StockRefillDto(string Id, DateTime RefilledOn, int MotorNumber, string ProductId, string ProductName, int Quantity);
public record StockClearedDto(string Id, DateTime ClearedOn, int MotorNumber, string ProductId, string ProductName, int Quantity);
 