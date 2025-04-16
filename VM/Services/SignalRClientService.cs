using CMS.Dto;
using CMS.Dto.Payments.QR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VM.Services.Interfaces;

namespace VM.Services;


public class SignalRClientService : ISignalRClientService
{
    private readonly IEventService eventService;
    private readonly ILogger<SignalRClientService> logger;
    private Timer? _timer;

    private HubConnection hubConnection;
    public event Action<QrStatusDto>? OnPaymentStatusReceived;
    public event Action<string>? OnTestMessageReceived;
    public event Action<PowerOptionDto>? OnPowerOptionReceived;
    public event Action<ReportRequestDto>? OnReportRequestReceived;
    public event Action<LogRequestNotificationDto>? OnLogRequestReceived;

    public SignalRClientService(IConfiguration configuration, IServerClient serverClient, IEventService eventService, ILogger<SignalRClientService> logger)
    {
        this.eventService = eventService;
        this.logger = logger;

        string url = $"{configuration["BaseURL"]!.TrimEnd('/')}/hubs/notification";

        hubConnection = new HubConnectionBuilder()
                            .WithUrl(url, option =>
                            {
                                option.AccessTokenProvider = () => serverClient.GetToken()!;
                            })
                            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(10) })
                            .Build();


        hubConnection.On<QrStatusDto>(HubMethods.PaymentStatusMachine, (qrStatus) =>
        {
            OnPaymentStatusReceived?.Invoke(qrStatus);
        });

        hubConnection.On<PowerOptionDto>(HubMethods.PowerOption, (powerOption) =>
        {
            OnPowerOptionReceived?.Invoke(powerOption);
        });

        hubConnection.On<ReportRequestDto>(HubMethods.ReportRequest, (message) =>
        {
            OnReportRequestReceived?.Invoke(message);
        });

        hubConnection.On<LogRequestNotificationDto>(HubMethods.LogRequest, (message) =>
        {
            OnLogRequestReceived?.Invoke(message);
        });

        hubConnection.On<string>(HubMethods.Machine_TestMessage, (message) =>
        {
            logger.LogInformation(message);
            OnTestMessageReceived?.Invoke(message);
        });

        hubConnection.Reconnected += HubConnection_Reconnected;
        hubConnection.Closed += HubConnection_Closed;
    }

    public async Task ConnectAsync()
    {

        try
        {
            await hubConnection.StartAsync();
            logger.LogInformation($"Connection state: {hubConnection.State}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        eventService.RaiseNetworkStatusChanged(hubConnection.State == HubConnectionState.Connected);
    }

    public async Task DisconnectAsync()
    {
        if (hubConnection != null && hubConnection.State == HubConnectionState.Connected)
        {
            await hubConnection.StopAsync();
            eventService.RaiseNetworkStatusChanged(hubConnection.State == HubConnectionState.Connected);
        }
    }

    private Task HubConnection_Closed(Exception? ex)
    {
        logger.LogInformation(ex?.Message);
        eventService.RaiseNetworkStatusChanged(hubConnection.State == HubConnectionState.Connected);
        return Task.CompletedTask;
    }

    private Task HubConnection_Reconnected(string? arg)
    {
        eventService.RaiseNetworkStatusChanged(hubConnection.State == HubConnectionState.Connected);
        return Task.CompletedTask;
    }


    public bool IsConnected()
    {
        return hubConnection.State == HubConnectionState.Connected;
    }

    public void StartConnectionMonitoring()
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private async void DoWork(object? state)
    {
        if (hubConnection != null && hubConnection.State == HubConnectionState.Disconnected)
        {
            await ConnectAsync();
        }
    }

    public void StopConnectionMonitoring()
    {
        _timer?.Dispose();
    }

}
