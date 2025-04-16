using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class Vendor
{
    public int Id { get; set; }
    public string? Name { get; set; }

    [MaxLength(3)]
    public string? ShortName { get; set; }

    [Precision(0)]
    public DateTime CreatedOn { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
