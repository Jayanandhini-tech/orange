namespace CMS.Dto;

public class Token
{
    public string JwtToken { get; set; } = string.Empty;
    public Guid RefreshToken { get; set; }
    public DateTime Expired { get; set; }
}
