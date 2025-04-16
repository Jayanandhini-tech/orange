using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class RefreshToken
{
    [Key]
    public Guid Token { get; set; }

    [Required]
    [StringLength(50)]
    public string? UserName { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}
