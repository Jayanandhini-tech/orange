using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Domains;

[Index(nameof(AppType))]
public class Category : VendorEntity, IAppTypeEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [MaxLength(300)]
    public string ImgPath { get; set; } = string.Empty;

    [Precision(0)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedOn { get; set; }

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;

    public virtual List<Product>? Products { get; }

}
