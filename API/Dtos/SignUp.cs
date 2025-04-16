using System.ComponentModel.DataAnnotations;

namespace CMS.API.Dtos;

public class SignUp
{   
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;


    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public int VendorId { get; set; } 

    public string Role { get; set; } = string.Empty;
}
