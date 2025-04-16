using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace VM.Domains;

public class OrderItem
{
    [Key]
    [MaxLength(35)]
    public required string Id { get; set; }

    public string OrderId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    [Precision(2)]
    public double Rate { get; set; }

    public int Gst { get; set; }

    [Precision(2)]
    public double Price { get; set; }

    public int Qty { get; set; }

    public int VendQty { get; set; }

    [Precision(0)]
    public DateTime UpdatedOn { get; set; }

    public bool IsViewed { get; set; } = false;
}
