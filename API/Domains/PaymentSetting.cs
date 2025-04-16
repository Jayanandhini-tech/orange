using System.ComponentModel.DataAnnotations;

namespace CMS.API.Domains;

public class PaymentSetting : VendorEntity
{
    public int Id { get; set; }

    [MaxLength(35)]
    public required string MachineId { get; set; }

    public bool Cash { get; set; }
    public bool Upi { get; set; }
    public bool Account { get; set; }
    public bool Card { get; set; }
    public bool Counter { get; set; }
}
