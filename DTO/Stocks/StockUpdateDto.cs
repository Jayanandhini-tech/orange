namespace CMS.Dto.Stocks;

public class StockUpdateDto
{
    public string ProductId { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int Stock { get; set; }
}
