namespace CMS.Dto;

public class LoginDto
{
    public string Key { get; set; } = string.Empty;
    public List<string> Mac { get; set; } = new List<string>();
}
