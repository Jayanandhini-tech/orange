namespace CMS.Dto;

public record LogRequestDto(string MachineId, DateTime LogDate);
public record LogRequestNotificationDto(string RequestId, DateTime LogDate);
public record LogRequestDisplayDto(string Id, DateTime LogDate, bool Sent, bool Received, bool Success, string Message);
public record LogReceivedAlertDto(string RequestId, string Message);
