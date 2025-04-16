using System.ComponentModel.DataAnnotations;

namespace CMS.API.Dtos;

public class ProductAdd
{
    [Required]
    [MinLength(3)]
    public required string Name { get; set; }

    [Range(1, 99999)]
    public double Price { get; set; }

    [Range(0, 100)]
    public int GST { get; set; }

    public string CategoryId { get; set; } = string.Empty;

    public IFormFile? ProductImage { get; set; }
}
