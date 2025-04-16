using Microsoft.EntityFrameworkCore;

namespace VM.Domains;

public class MotorSetting
{
    public int Id { get; set; }
    public int CabinId { get; set; }
    public int MotorNumber { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Stock { get; set; }
    public bool SoldOut { get; set; }

    [Precision(0)]   
    public DateTime UpdatedOn { get; set; }

    public bool IsViewed { get; set; }
    public bool IsActive { get; set; }
}
