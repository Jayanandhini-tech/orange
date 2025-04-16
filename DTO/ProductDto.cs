namespace CMS.Dto.Products;

public record ProductDto(string Id, string Name, double Price, double BaseRate, int gst, string ImgPath, DateTime UpdatedOn, string CategoryId);
