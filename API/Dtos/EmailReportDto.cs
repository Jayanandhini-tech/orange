namespace CMS.API.Dtos;

public class EmailReportDto
{
    public required IFormFile File { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ReportType { get; set; } = string.Empty;
}

public class LiveReportResponseDto
{
    public required IFormFile File { get; set; }
    public string RequestId { get; set; } = string.Empty;
}
