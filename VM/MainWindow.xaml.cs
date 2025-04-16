using CMS.Dto;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VM.Domains;
using VM.Pages;
using VM.Services.Interfaces;

namespace VM;


public partial class MainWindow : Window
{

    private readonly ISignalRClientService signalRClient;
    private readonly IEventService eventService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MainWindow> logger;
    private Timer? _timer;
    private bool restartRequired = false;
    private bool shutdownRequired = false;

    public MainWindow(ISignalRClientService signalRClient, IEventService eventService, IServiceProvider serviceProvider, ILogger<MainWindow> logger)
    {
        InitializeComponent();

        this.signalRClient = signalRClient;
        this.eventService = eventService;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    protected override void OnManipulationBoundaryFeedback(ManipulationBoundaryFeedbackEventArgs e)
    {
        e.Handled = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            restartRequired = false;
            shutdownRequired = false;

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            signalRClient.OnTestMessageReceived += SignalRClient_OnTestMessageReceived;
            signalRClient.OnPowerOptionReceived += SignalRClient_OnPowerOptionReceived;
            signalRClient.OnReportRequestReceived += SignalRClient_OnReportRequestReceived;
            signalRClient.OnLogRequestReceived += SignalRClient_OnLogRequestReceived;
            signalRClient.ConnectAsync();
            signalRClient.StartConnectionMonitoring();

            eventService.OnOrderCompleted += EventService_OnOrderCompleted;
            eventService.OnCashPaymentCanceled += EventService_OnCashPaymentCanceled;
            eventService.OnUserCreated += EventService_OnUserCreated;
            eventService.OnNetworkMessage += EventService_OnNetworkMessage;
            eventService.OnBackToOnline += EventService_OnBackToOnline;

            var scope = serviceProvider.CreateScope();
            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (environment.IsProduction())
            {
                Process[] oskProcessArray = Process.GetProcessesByName("explorer");
                foreach (Process onscreenProcess in oskProcessArray)
                {
                    onscreenProcess.Kill();
                }
                this.Topmost = true;
            }

            TimeSpan delay = CalculateDelayUntil3AM();
            _timer = new Timer(DoWork, null, delay, TimeSpan.FromHours(24));

            mainFrame.NavigationService.Navigated += NavigationService_Navigated;
            mainFrame.NavigationService.Navigate(scope.ServiceProvider.GetRequiredService<AppInitPage>());

        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    public async Task SyncDb()
    {
        try
        {
            if (eventService.IsNetworkAvailable)
            {
                var syncService = serviceProvider.GetRequiredService<ISyncService>();
                await syncService.OrderUpdateAsync();
                await syncService.StockUpdateAsync();
                await syncService.PushStockClearedAsync();
                await syncService.PushStockRefillAsync();
                await syncService.CashRefundUpdateAsync();
                await syncService.AppUserUpdateAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void EventService_OnBackToOnline()
    {
        try
        {
            string pageTitle = GetCurrentPageName();
            if (pageTitle != "AppInit")
                await SyncDb();
           
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void EventService_OnNetworkMessage(string message)
    {
        try
        {
            HideAlertMessage();
            ShowAlertMessage(message, 10);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private async void NetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        try
        {
            if (e.IsAvailable)
            {
                logger.LogInformation("Network connected");
                HideAlertMessage();
                var serverClient = serviceProvider.GetRequiredService<IServerClient>();
                bool online = await serverClient.IsServerConnectable();
                eventService.RaiseNetworkStatusChanged(online);
            }
            else
            {
                logger.LogInformation("Network disconnected");
                ShowAlertMessage("Network has become unavailable", 10);
                eventService.RaiseNetworkStatusChanged(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void EventService_OnUserCreated(string userId)
    {
        try
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            await userService.PostNewUserAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void EventService_OnOrderCompleted(string orderNumber)
    {
        try
        {
            if (!eventService.IsNetworkAvailable)
            {
                await signalRClient.ConnectAsync();
            }

            if (eventService.IsNetworkAvailable)
            {
                if (!string.IsNullOrEmpty(orderNumber.Trim()))
                {
                    var orderService = serviceProvider.GetRequiredService<IOrderService>();
                    await orderService.SendOrderToServer(orderNumber);
                    var juiceService = serviceProvider.GetRequiredService<IJuiceService>();
                    await juiceService.SendOrderToServer(orderNumber);
                }

                await SyncDb();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void EventService_OnCashPaymentCanceled(int refundId)
    {
        try
        {
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            var serverClient = serviceProvider.GetRequiredService<IServerClient>();

            var cashRefund = await dbContext.CashRefunds.FirstOrDefaultAsync(x => x.Id == refundId);
            if (cashRefund is not null)
            {
                var cashRefundDto = cashRefund.Adapt<CashRefundDto>();
                var response = await serverClient.PostAsync("api/cashrefund", cashRefundDto);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    cashRefund.IsViewed = true;
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    string respText = await response.Content.ReadAsStringAsync();
                    logger.LogInformation($"Cash refund send failed. Status Code : {response.StatusCode}, Response : {respText}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void SignalRClient_OnTestMessageReceived(string message)
    {
        MessageBox.Show(message);
    }

    private void SignalRClient_OnPowerOptionReceived(PowerOptionDto powerOption)
    {
        string currentPage = GetCurrentPageName();
        if (powerOption.option == PowerState.Restart)
        {
            if (currentPage == "Home")
            {
                RestartApp();
            }
            else
            {
                restartRequired = true;
            }
        }
        else if (powerOption.option == PowerState.Shutdown)
        {

            if (currentPage == "Home")
            {
                ShutdownApp();
            }
            else
            {
                shutdownRequired = true;
            }
        }
    }

    private async void SignalRClient_OnReportRequestReceived(ReportRequestDto reportRequestDto)
    {
        try
        {
            var reportService = serviceProvider.GetRequiredService<IReportService>();
            await reportService.SendReportToServer(reportRequestDto.RequestId, reportRequestDto.From, reportRequestDto.To);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void SignalRClient_OnLogRequestReceived(LogRequestNotificationDto dto)
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"{dto.LogDate.ToString("yyyy-MM-dd")}.log");
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(dto.RequestId), "RequestId");

            if (File.Exists(path))
            {
                byte[] bytes;

                using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    using (var binaryReader = new BinaryReader(reader.BaseStream))
                    {
                        bytes = binaryReader.ReadBytes((int)reader.BaseStream.Length);
                    }
                }

                //var bytes = await File.ReadAllBytesAsync(path);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                form.Add(fileContent, "File", Path.GetFileName(path));
                form.Add(new StringContent($"{true}"), "Success");
            }
            else
            {
                form.Add(new StringContent($"{false}"), "Success");
                form.Add(new StringContent("File not found"), "Message");
            }

            var httpClient = serviceProvider.GetRequiredService<IServerClient>();
            var response = await httpClient.PostAsync("api/machinelogs/response", form);
            var respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation($"Log Send. Status Code : {response.StatusCode}, Response : {respText}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }


    public async Task SendLogFileToServerAsync(string requestId, DateTime logDate)
    {
        try
        {

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"{logDate.ToString("yyyy-MM-dd")}.log");

            if (File.Exists(path))
            {
                using var form = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(path));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                form.Add(fileContent, "File", Path.GetFileName(path));
                form.Add(new StringContent(requestId), "RequestId");

                var httpClient = serviceProvider.GetRequiredService<IServerClient>();

                var response = await httpClient.PostAsync("api/reports/live/machine/response", form);
                var respText = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Report Send. Status Code : {response.StatusCode}, Response : {respText}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }


    private TimeSpan CalculateDelayUntil3AM()
    {
        int hour = 3;
        int minute = 0;
        DateTime now = DateTime.Now;
        DateTime targetTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        if (now.Hour >= hour && now.Minute >= minute)
            targetTime = targetTime.AddDays(1);

        return targetTime - now;
    }

    private void DoWork(object? state)
    {
        try
        {
            string currentPage = GetCurrentPageName();
            logger.LogInformation(currentPage);

            if (currentPage == "Home")
            {
                RestartApp();
            }
            else
            {
                restartRequired = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void NavigationService_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        try
        {
            var currentpage = e.Content as Page;
            if (currentpage?.Title == "Home")
            {
                if (restartRequired)
                    RestartApp();

                if (shutdownRequired)
                    ShutdownApp();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void ShowAlertMessage(string Message, int TimeoutSeconds = 5)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                sbAlert.MessageQueue?.Enqueue(Message, null, null, null, false, true, TimeSpan.FromSeconds(TimeoutSeconds));
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void HideAlertMessage()
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                sbAlert.IsActive = false;
                sbAlert.MessageQueue?.Clear();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private string GetCurrentPageName()
    {
        string pageName = string.Empty;
        Dispatcher.Invoke((Action)(() =>
        {
            var currentPage = mainFrame.Content as Page;
            pageName = currentPage?.Title ?? string.Empty;
        }));

        return pageName;
    }

    private void RestartApp()
    {
        logger.LogInformation("Application going to Auto Restart");
        Process.Start("shutdown", "/r /t 2");
        Application.Current.Shutdown();
    }

    private void ShutdownApp()
    {
        logger.LogInformation("Application going to Auto Shutdown");
        Process.Start("shutdown", "/s /t 2");
        Application.Current.Shutdown();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _timer?.Dispose();
        signalRClient.DisconnectAsync();
        signalRClient.StopConnectionMonitoring();
    }
}