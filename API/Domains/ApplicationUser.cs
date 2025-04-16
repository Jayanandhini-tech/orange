using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMS.API.Domains;

public class ApplicationUser : IdentityUser
{
    public int? VendorId { get; set; }

    [ForeignKey("VendorId")]
    public Vendor? Vendor { get; set; }
}
