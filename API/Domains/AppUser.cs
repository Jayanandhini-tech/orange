using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class AppUser : VendorEntity
{
    [MaxLength(20)]
    public required string Id { get; set; }

    [MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(15)]
    public string IdCardNo { get; set; } = string.Empty;

    [Precision(2)]
    public double Balance { get; set; }

    [Precision(2)]
    public double CreditLimit { get; set; }

    public string ImagePath { get; set; } = string.Empty;

    [Precision(0)]
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}
