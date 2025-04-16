namespace CMS.Dto;

public record SalesDetailsDto(double Total, double Upi, double Account, double Cash, double Card, double Today);

public record MonthWiseSales(string Month, double Total);

public record DailySales(string Day, double Total);

public record MachineStatusDto(string Id, string AppType, int Number, string Name, string Location, string Status, DateTime UpdatedOn);

public record SalesTotalDto(double Total, double Upi, double Account, double Cash, double Card);

public record SalesLegendsDto(SalesTotalDto Month, SalesTotalDto Today); 

public record ProdcutSalesSumDto(string Name, int Quantity);