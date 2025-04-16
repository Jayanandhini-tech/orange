using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace VM.Domains;

public class Category
{
    [Key]
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string ImgPath { get; set; } = string.Empty;
    [Precision(0)]  
    public DateTime UpdatedOn { get; set; }
    public virtual List<Product>? Products { get; }
}
