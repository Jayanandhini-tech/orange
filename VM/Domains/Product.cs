using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VM.Domains;

public class Product
{
    [Key]
    public required string Id { get; set; }

    public required string CategoryId { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string ImgPath { get; set; } = string.Empty;

    [Precision(2)]
    public double BaseRate { get; set; }

    public int Gst { get; set; }

    [Precision(2)]
    public double Price { get; set; }

    [Precision(0)]
    public DateTime UpdatedOn { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }

    internal Product? ToListAsync()
    {
        throw new NotImplementedException();
    }
}
