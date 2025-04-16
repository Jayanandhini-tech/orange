

namespace CMS.API.Dtos.VM.Request;


public record SalesAddDto(string SalesCode, DateTime SalesDate, string Type, double Total, string RefNo, string CustomerCode, string CustomerName, List<SalesItemDto> Items);
public record SalesItemDto(string ProductName, double Price, int Quantity, double Amount);