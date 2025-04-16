using CMS.API.Domains;
using CMS.API.Services.Interfaces;
using CMS.Dto;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Services;

public class PendingMessageService : IPendingMessageService
{
    private readonly AppDBContext dBContext;
    private readonly ITenant tenant;
    private readonly INotificationService notificationService;
    private readonly ILogger<PendingMessageService> logger;

    public PendingMessageService(AppDBContext dBContext, ITenant tenant, INotificationService notificationService, ILogger<PendingMessageService> logger)
    {
        this.dBContext = dBContext;
        this.tenant = tenant;
        this.notificationService = notificationService;
        this.logger = logger;
    }

    public async Task SendPendingMessages(string MachineId, string ConnectionId)
    {
        try
        {
            await SendReportReqestMessages(MachineId, ConnectionId);
            await SendLogRequests(MachineId, ConnectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task SendReportReqestMessages(string MachineId, string ConnectionId)
    {
        try
        {
            var messages = await dBContext.ReportRequests
                                        .Where(x => x.MachineId == MachineId && x.Sent == false)
                                        .Select(s => new ReportRequestDto(s.Id, s.From, s.To))
                                        .ToListAsync();
            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    await notificationService.SendToMachineAsync(ConnectionId, HubMethods.ReportRequest, message);
                }
                var updatedIds = messages.Select(x => x.RequestId).ToList();

                await dBContext.ReportRequests.Where(x => updatedIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(p => p.Sent, true));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task SendLogRequests(string MachineId, string ConnectionId)
    {
        try
        {
            var messages = await dBContext.LogRequests
                                        .Where(x => x.MachineId == MachineId && x.Sent == false)
                                        .Select(s => new LogRequestNotificationDto(s.Id, s.LogDate))
                                        .ToListAsync();
            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    await notificationService.SendToMachineAsync(ConnectionId, HubMethods.LogRequest, message);
                }
                var updatedIds = messages.Select(x => x.RequestId).ToList();

                await dBContext.LogRequests.Where(x => updatedIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(p => p.Sent, true));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
}

