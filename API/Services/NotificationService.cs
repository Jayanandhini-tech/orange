using CMS.API.Dtos.Dashboard;
using CMS.API.Hubs;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using CMS.Dto.Payments.QR;
using Microsoft.AspNetCore.SignalR;

namespace CMS.API.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> hubContext;


    public NotificationService(IHubContext<NotificationHub> hubContext)
    {
        this.hubContext = hubContext;
    }

    public async Task AddToGroup(int vendorId, string appType, string connectionId)
    {
        if ((appType == StrDir.AppType.VM || appType == StrDir.AppType.KIOSK))
        {
            await hubContext.Groups.AddToGroupAsync(connectionId, $"V{vendorId}-machines");
        }
        else
        {
            await hubContext.Groups.AddToGroupAsync(connectionId, $"V{vendorId}-dashboard");
        }
    }


    public async Task RemoveFromGroup(int vendorId, string appType, string connectionId)
    {
        if ((appType == StrDir.AppType.VM || appType == StrDir.AppType.KIOSK))
        {
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, $"V{vendorId}-machines");
        }
        else
        {
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, $"V{vendorId}-dashboard");
        }
    }

    public async Task MachineStatusToDashboard(int vendorId, ConnectionStatusDto statusDto)
    {
        await hubContext.Clients.Group($"V{vendorId}-dashboard").SendAsync(HubMethods.VendingMachineStatus, statusDto);
    }

    public async Task PaymentStatusToMachineAsync(QrStatusDto qrStatus, string connectionId)
    {
        await hubContext.Clients.Client(connectionId).SendAsync(HubMethods.PaymentStatusMachine, qrStatus);
    }

    public async Task PaymentStatusToAllMachineAsync(int vendorId, QrStatusDto qrStatus)
    {
        await hubContext.Clients.Group($"V{vendorId}-machines").SendAsync(HubMethods.PaymentStatusMachine, qrStatus);
    }

    public async Task TestMessageToAllMachine(int vendorId, string message)
    {
        await hubContext.Clients.Group($"V{vendorId}-machines").SendAsync(HubMethods.Machine_TestMessage, message);
    }

    public async Task SendToMachineAsync(string connectionId, string method, object message)
    {
        await hubContext.Clients.Client(connectionId).SendAsync(method, message);
    }

    public async Task SendToAllMachineAsync(int vendorId, string method, object message)
    {
        await hubContext.Clients.Group($"V{vendorId}-machines").SendAsync(method, message);
    }

    public async Task SendToDashboard(int vendorId, string method, object message)
    {
        await hubContext.Clients.Group($"V{vendorId}-dashboard").SendAsync(method, message);
    }

}
