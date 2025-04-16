namespace CMS.Dto;

public record StockMachineDto(int CabinId, int MotorNumber, string ProductId, int Capacity, int Stock, bool SoldOut, DateTime UpdatedOn, bool IsActive);

public record StockUpdateResultDto(List<int> MotorNumbers);


public record StockItemDto(int MotorNumber, string Product, double Price, string ImgPath, int Capacity, int Stock, bool SoldOut);

public record CabinRowDto(List<StockItemDto> Items);

public record StockDisplayDto(List<CabinRowDto> Cabin1, List<CabinRowDto> Cabin2);

public record StockReportDto(string Product, double Price, int Capacity, int Stock, double Amount);
public record StockRequirementDto(string Product, double Price, int Required, double Amount);