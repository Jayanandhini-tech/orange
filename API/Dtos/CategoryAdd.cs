namespace CMS.API.Dtos.Categories;

public class CategoryAdd
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public required IFormFile Image { get; set; }
}
