using System.ComponentModel.DataAnnotations;

namespace CMS.API.Dtos;

public class Login
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
