using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Domains;

[Index(nameof(AppType))]
public class Product : VendorEntity, IAppTypeEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;

    [MaxLength(35)]
    public string CategoryId { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string ImgPath { get; set; } = string.Empty;


    [Precision(2)]
    public double BaseRate { get; set; }

    public int Gst { get; set; }

    [Precision(2)]
    public double Price { get; set; }

    public bool IsStocked { get; set; }

    public int Stock { get; set; }


    [Precision(0)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedOn { get; set; }


    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }
}
