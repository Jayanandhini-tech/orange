namespace CMS.API.Services.Interfaces;

public interface IPendingMessageService
{
    Task SendPendingMessages(string MachineId, string ConnectionId);
}
