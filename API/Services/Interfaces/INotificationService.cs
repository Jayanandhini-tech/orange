using CMS.API.Dtos.Dashboard;
using CMS.Dto.Payments.QR;

namespace CMS.API.Services.Interfaces;

public interface INotificationService
{
    Task AddToGroup(int vendorId, string appType, string connectionId);
    Task MachineStatusToDashboard(int vendorId, ConnectionStatusDto statusDto);
    Task PaymentStatusToAllMachineAsync(int vendorId, QrStatusDto qrStatus);
    Task PaymentStatusToMachineAsync(QrStatusDto qrStatus, string connectionId);
    Task RemoveFromGroup(int vendorId, string appType, string connectionId);    
    Task SendToAllMachineAsync(int vendorId, string method, object message);
    Task SendToDashboard(int vendorId, string method, object message);
    Task SendToMachineAsync(string connectionId, string method, object message);
    Task TestMessageToAllMachine(int vendorId, string message);
}