using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class PaymentAccountSetting : VendorEntity
{
    public int Id { get; set; }

    [MaxLength(35)]
    public required string MachineId { get; set; }

    [MaxLength(15)]
    public string AccountPlan { get; set; } = string.Empty;

    [MaxLength(15)]
    public string AuthType { get; set; } = string.Empty;

    public double MonthlyLimit { get; set; }

    public double DailyLimit { get; set; }
}
