namespace CMS.Dto;

public class OrderDespatchDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string AppUserId { get; set; } = string.Empty;
    public required List<DespatchItemDto> DespatchItems { get; set; }
}

public record DespatchItemDto(string ProductId, int VendQty, DateTime UpdatedOn);