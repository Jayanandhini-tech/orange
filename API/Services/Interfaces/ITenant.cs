using System.Security.Claims;

namespace CMS.API.Services.Interfaces;

public interface ITenant
{
    string AppType { get; }
    int VendorId { get; }
    string MachineId { get; }
    string MachineName { get; }
    int MachineNumber { get; }
}
