namespace CMS.Dto;


public record RefillSummaryItemDto(string ProductName, double Price, int Filled, int Cleared, int Quantity, double Amount);
public record RefillSummaryDto(int Attempt, DateTime Start, DateTime End, List<RefillSummaryItemDto> Items);


public record RefillItemDto(DateTime Date, int Motor, string Product, double Price, int Quantity, double Amount);