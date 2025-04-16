using System.ComponentModel.DataAnnotations;

namespace VM.Domains;
public class PaymentAccountSetting
{
    [Key]
    [MaxLength(35)]
    public required string MachineId { get; set; }

    [MaxLength(15)]
    public string AccountPlan { get; set; } = string.Empty;

    [MaxLength(15)]
    public string AuthType { get; set; } = string.Empty;

    public double MonthlyLimit { get; set; }

    public double DailyLimit { get; set; }
}
