namespace CMS.Dto.Stocks;

public class CategoryWiseStock
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string ImgPath { get; set; } = string.Empty;
    public List<StockProduct> Products { get; set; } = [];
}


public class StockProduct
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public string ImgPath { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int Stock { get; set; }
}
