using CMS.Dto;
using CMS.Dto.Payments.QR;

namespace VM.Services.Interfaces;

public interface ISignalRClientService
{
    event Action<QrStatusDto>? OnPaymentStatusReceived;
    event Action<string>? OnTestMessageReceived;
    event Action<PowerOptionDto>? OnPowerOptionReceived;
    event Action<ReportRequestDto>? OnReportRequestReceived;
    event Action<LogRequestNotificationDto>? OnLogRequestReceived;

    Task ConnectAsync();
    Task DisconnectAsync();
    bool IsConnected();
    void StartConnectionMonitoring();
    void StopConnectionMonitoring();
}