using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Domains;

public class OrderItem : VendorEntity, IAppTypeEntity
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    [MaxLength(80)]
    public string ProductName { get; set; } = string.Empty;

    [Precision(2)]
    public double Rate { get; set; }
    public int Gst { get; set; }

    [Precision(2)]
    public double Price { get; set; }

    public int Qty { get; set; }

    public int VendQty { get; set; }

    [Precision(0)]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedOn { get; set; }


    [MaxLength(10)]
    public string AppType { get; set; } = string.Empty;


    [ForeignKey(nameof(OrderId))]
    public virtual Order? Order { get; set; }
}
