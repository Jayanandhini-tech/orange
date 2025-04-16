namespace CMS.Dto;

public record ReportRequestDto(string RequestId, DateTime From, DateTime To);
public record ReportRangeDto(string MachineId, DateTime From, DateTime To);
public record ReportRequestDisplayDto(string Id, DateTime From, DateTime To, bool Sent, bool Received);

public record ReportReceivedAlertDto(string RequestId, string Message);