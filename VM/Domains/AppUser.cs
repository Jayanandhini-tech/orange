using Microsoft.EntityFrameworkCore;

namespace VM.Domains;

public class AppUser
{
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CreditLimit { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    [Precision(0)]
    public DateTime CreatedOn { get; set; }
    public bool IsViewed { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
