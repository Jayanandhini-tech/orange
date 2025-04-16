namespace CMS.API.Dtos;

public class LogResponseDto
{
    public IFormFile? File { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
}
