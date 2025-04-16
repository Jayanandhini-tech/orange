using CMS.API.Domains;
using CMS.API.Dtos.Dashboard;
using CMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CMS.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<NotificationHub> logger;

    public NotificationHub(IServiceProvider serviceProvider, ILogger<NotificationHub> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }


    public override async Task OnConnectedAsync()
    {

        await base.OnConnectedAsync();
        try
        {
            var notificationService = serviceProvider.GetRequiredService<INotificationService>();
            var tenant = serviceProvider.GetRequiredService<ITenant>();

            await notificationService.AddToGroup(tenant.VendorId, tenant.AppType, Context.ConnectionId);

            if (!string.IsNullOrEmpty(tenant.MachineId))
            {
                logger.LogInformation($"Connected. MachineId :  {tenant.MachineId}, Name : {tenant.MachineName}");

                await UpdateMachineStatus(tenant.MachineId, true, Context.ConnectionId);
                await SendPendingMessages(tenant.MachineId, Context.ConnectionId);

                await notificationService.MachineStatusToDashboard(tenant.VendorId, new ConnectionStatusDto(tenant.MachineId, true, "Online", DateTime.Now));
            }
            else
            {
                logger.LogInformation($"Connected. User : {Context.User?.Identity?.Name ?? "Unknown"}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        try
        {
            var notificationService = serviceProvider.GetRequiredService<INotificationService>();
            var tenant = serviceProvider.GetRequiredService<ITenant>();

            await notificationService.RemoveFromGroup(tenant.VendorId, tenant.AppType, Context.ConnectionId);

            if (!string.IsNullOrEmpty(tenant.MachineId))
            {
                logger.LogInformation($"Disconnected. MachineId :  {tenant.MachineId}, Name : {tenant.MachineName}");
                await UpdateMachineStatus(tenant.MachineId, false, Context.ConnectionId);
                await notificationService.MachineStatusToDashboard(tenant.VendorId, new ConnectionStatusDto(tenant.MachineId, false, "Offline", DateTime.Now));
            }
            else
            {
                logger.LogInformation($"Disconnected. User : {Context.User?.Identity?.Name ?? "Unknown"}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }


    private async Task UpdateMachineStatus(string machineId, bool isConnected, string connectionId)
    {
        try
        {
            var dBContext = serviceProvider.GetRequiredService<AppDBContext>();

            var machine = await dBContext.Machines.FindAsync(machineId);
            if (machine is not null)
            {
                machine.Status = isConnected ? "Online" : "Offline";
                if (isConnected)
                {
                    machine.ConnectionId = Context.ConnectionId;
                }
                else
                {
                    if (machine.ConnectionId == connectionId)
                        machine.ConnectionId = "";
                }
                await dBContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }


    private async Task SendPendingMessages(string MachineId, string ConnectionId)
    {
        try
        {
            var pendingMessageService = serviceProvider.GetRequiredService<IPendingMessageService>();
            await pendingMessageService.SendPendingMessages(MachineId, ConnectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
}
