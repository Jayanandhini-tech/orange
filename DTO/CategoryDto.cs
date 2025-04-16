namespace CMS.Dto;

public record struct CategoryDto(string Id, string Name, string ImgPath, int DisplayOrder, DateTime UpdatedOn);