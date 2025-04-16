using CMS.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class AppInitPage : Page
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ISyncService syncService;
    private readonly IEventService eventService;
    private readonly ISensorService sensorService;
    private readonly IModbus modbus;
    private readonly IDailyReportService dailyReportService;
    private readonly ILogger<AppInitPage> logger;

    public AppInitPage(IServiceScopeFactory serviceScopeFactory, ISyncService syncService, IEventService eventService, ISensorService sensorService, IModbus modbus, IDailyReportService dailyReportService, ILogger<AppInitPage> logger)
    {
        InitializeComponent();
        this.serviceScopeFactory = serviceScopeFactory;
        this.syncService = syncService;
        this.eventService = eventService;
        this.sensorService = sensorService;
        this.modbus = modbus;
        this.dailyReportService = dailyReportService;
        this.logger = logger;
    }

    private async void DialogHost_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            string reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            if (!Directory.Exists(reportDir))
                Directory.CreateDirectory(reportDir);


            await LoadAllRecords();
            await CheckFaceDevice();


            if (DataStore.AppType == StrDir.AppType.VM)
            {
                modbus.Init("COM4");
                await syncService.InitMotorSettings();

                lblMessage.Text = "Calibrating Sensors";
                sensorService.CalibrateAllSensorPermanantly();

                dailyReportService.Start();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        finally
        {
            var scope = serviceScopeFactory.CreateScope();
            //NavigationService.Navigate(scope.ServiceProvider.GetRequiredService<JuicePage>());
            NavigationService.Navigate(scope.ServiceProvider.GetRequiredService<HomePage>());
        }
    }


    public async Task LoadAllRecords()
    {
        await syncService.SetDefaultPaymentSettings();

        lblMessage.Text = "Testing Internet Connection";
        bool isOnline = await syncService.CheckInternetStatus();

        eventService.RaiseNetworkStatusChanged(isOnline);

        if (isOnline)
        {
            lblMessage.Text = "Getting Machine Data";
            await syncService.GetMachineInfo();
            await syncService.GetPaymentSettings();
        }

        lblMessage.Text = "Loading Machine Data";
        await syncService.LoadMachineData();

        if (isOnline)
        {
            lblMessage.Text = "Getting Company details";
            await syncService.GetCompany();

            lblMessage.Text = "Loading Categories";
            await syncService.GetCategories();

            lblMessage.Text = "Loading Products";
            await syncService.GetProducts();

            lblMessage.Text = "Updating Sales records";
            await syncService.OrderUpdateAsync();

            lblMessage.Text = "Refunds Sync";
            await syncService.CashRefundUpdateAsync();

            lblMessage.Text = "Users Sync";
            await syncService.AppUserUpdateAsync();

            if (DataStore.AppType == StrDir.AppType.VM)
            {
                lblMessage.Text = "Sync Stocks";
                await syncService.StockUpdateAsync();

                lblMessage.Text = "Sending Stock return details";
                await syncService.PushStockClearedAsync();

                lblMessage.Text = "Pushing Refill datas";
                await syncService.PushStockRefillAsync();
            }
        }

        UpdateDispalyName();

    }


    public async Task CheckFaceDevice()
    {
        try
        {
            var scope = serviceScopeFactory.CreateScope();
            var paymentConfig = scope.ServiceProvider.GetRequiredService<PaymentConfig>();

            if (paymentConfig.Account && paymentConfig.AccountConfig.AuthType == StrDir.AuthPage.FACE)
            {
                lblMessage.Text = "Trying to connect with a face device";

                var faceDevice = scope.ServiceProvider.GetRequiredService<IFaceDeviceService>();

                bool isConnected = await faceDevice.ConnectDevice();
                if (isConnected)
                {
                    faceDevice.DeleteAllRecords();
                    faceDevice.SetDateTime();
                    faceDevice.DisconnectDevice();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public void UpdateDispalyName()
    {
        try
        {
            var scope = serviceScopeFactory.CreateScope();

            var mainWindow = scope.ServiceProvider.GetRequiredService<MainWindow>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = dbContext.Company.OrderBy(x => x.Id).FirstOrDefault();
            if (company != null)
            {
                mainWindow.lblDisplayName.Text = company.Name;
                if (string.IsNullOrEmpty(company.Phone.Trim()))
                {
                    mainWindow.lblSupport.Text = "www.bvc24.com";
                }
                else
                {
                    mainWindow.lblSupport.Text = $"www.bvc24.com | Support : +91 {company.Phone}";
                }

            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
}
