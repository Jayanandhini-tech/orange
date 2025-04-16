namespace CMS.Dto;

public record SalesByUserDto(string UserId, string UserName, double Amount, string Items);

public record SalesByProductDto(string Category, string ProductId, string ProductName, double Price, int Quantity, double Amount);

public record CurrentStockByProductDto(string ProductName, double Price, int Capacity, int Stock, double Amount);

public record RefillReportDto(DateTime RefilledOn, int MotorNumber, string ProductName, double Price, int RefillCount, double Amount);

public record StockClearedReportDto(DateTime ClearedOn, int MotorNumber, string ProductName, double Price, int RefillCount, double Amount);