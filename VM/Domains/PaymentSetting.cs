using System.ComponentModel.DataAnnotations;

namespace VM.Domains;
public class PaymentSetting
{
    [Key]
    [MaxLength(35)]
    public required string MachineId { get; set; }
    public bool Cash { get; set; }
    public bool Upi { get; set; }
    public bool Account { get; set; }
    public bool Card { get; set; }
    public bool Counter { get; set; }
}
