namespace CMS.Dto;

public class CategoryIncludeProductWithStockDto
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string ImgPath { get; set; } = string.Empty;
    public List<ProductDisplayDto> Products { get; set; } = new List<ProductDisplayDto>();
}

public class ProductDisplayDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public double Rate { get; set; }
    public int Gst { get; set; }
    public string ImgPath { get; set; } = string.Empty;
    public int Stock { get; set; }
}
