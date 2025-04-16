using CMS.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Threading;
using VM.Components;
using VM.Domains;
using VM.Dtos;
using VM.Pages;
using VM.Services;
using VM.Services.Interfaces;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace VM;

public partial class App : Application
{
    private readonly IHost host;
    private ILogger<App> logger;
    private readonly string AppId = "567862f0-124a-43b5-89ec-53bad3845b13";
    private static Mutex? _mutex = null;

    public App()
    {
        Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

        host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((builderContext, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration
                    .SetBasePath(Directory.GetCurrentDirectory())

                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{builderContext.HostingEnvironment.EnvironmentName}.json", true, true);
                })
                .ConfigureLogging(logBuilder =>
                {
                    logBuilder.AddLog4Net("log4net.config");
                })
                .UseDefaultServiceProvider(options => options.ValidateScopes = false)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<AppDbContext>(opt =>
                    {
                        opt.UseSqlite("Data Source=bvcvm.db");
                    });

                    services.AddHttpClient<IServerClient, ServerClient>();
                    services.AddSingleton<ISignalRClientService, SignalRClientService>();
                    services.AddSingleton<IModbus, Modbus>();
                    services.AddSingleton<ISensorService, SensorService>();
                    services.AddSingleton<IBillValidatorService, BillValidatorService>();
                    services.AddSingleton<IDailyReportService, DailyReportService>();
                    services.AddSingleton<IEventService, EventService>();
                    services.AddSingleton<PaymentConfig>(serviceProvider =>
                    {
                        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
                        string machineId = hostContext.Configuration["MachineKey"] ?? "";
                        return GetConfiguration(machineId, dbContext);
                    });

                    services.AddSingleton<SpeechSynthesizer>();
                    services.AddSingleton<MainWindow>();
                    services.AddScoped<IFaceDeviceService, FaceDeviceService>();
                    services.AddScoped<ISyncService, SyncService>();
                    services.AddScoped<IOrderService, OrderService>();
                    services.AddScoped<IReportService, ReportService>();
                    services.AddScoped<IUserService, UserService>();
                    services.AddScoped<ProductDialog>();
                    services.AddScoped<AppDbInitializer>();
                    services.AddScoped<AppInitPage>();
                    services.AddScoped<HomePage>();
                    services.AddScoped<OrderPage>();
                    //services.AddScoped<CartPage>();
                    services.AddScoped<PaymentOptionPage>();
                    services.AddScoped<UpiPage>();
                    services.AddScoped<CashPage>();
                    services.AddScoped<VendingPage>();
                    services.AddScoped<KioskVendingPage>();
                    services.AddScoped<PrintBillPage>();
                    services.AddScoped<RefillPage>();
                    services.AddScoped<SpiralSettingPage>();
                    services.AddScoped<OperatorHomePage>();
                    services.AddScoped<IdCardLoginPage>();
                    services.AddScoped<IdCardRegisterPage>();
                    services.AddScoped<FaceLoginPage>();
                    services.AddScoped<UserHomePage>();
                    services.AddScoped<RechargeAmountPage>();
                    services.AddScoped<RechargeUPIpage>();
                    services.AddScoped<ReportsPage>();
                    services.AddScoped<StockRequiredPage>();
                    services.AddScoped<MotorTestingPage>();
                    services.AddScoped<OrangeHomePage>();
                    services.AddScoped<JuicePage>();
                    services.AddScoped<IJuiceService,JuiceService>();
                    services.AddScoped<PinelabsUpiPage>();
                    services.AddScoped<IPineLabsService, PineLabsService>();
                    services.AddScoped<CardPaymentPage>();
                }).Build();
        logger = host.Services.GetRequiredService<ILogger<App>>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {

        _mutex = new Mutex(true, AppId, out bool createdNew);
        if (!createdNew)
        {
            logger.LogInformation("Application Already opened");
            _mutex = null;
            Application.Current.Shutdown();
            return;
        }


        await host.StartAsync();

        using (var scope = host.Services.CreateScope())
        {
            var dbInitialzer = scope.ServiceProvider.GetRequiredService<AppDbInitializer>();
            await dbInitialzer.Initialize();
        }

        string videoroot = AppDomain.CurrentDomain.BaseDirectory + @"Videos\Home\";
        if (!Directory.Exists(videoroot))
            Directory.CreateDirectory(videoroot);

        DataStore.videos = Directory.GetFiles(videoroot, "*.mp4").ToList();
             
        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var dailyReportService = host.Services.GetRequiredService<IDailyReportService>();
        dailyReportService.Stop();

        await host.StopAsync();
        if (_mutex != null)
            _mutex.ReleaseMutex();
        base.OnExit(e);
    }


    private void Application_Startup(object sender, StartupEventArgs e)
    {

        logger.LogInformation("=========================================== Vending Application Start =============================================");
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        logger.LogInformation("=========================================== Vending Application Exit ==============================================\n");
    }

    public void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        logger.LogError(e.Exception, e.Exception.Message);
        e.Handled = true;
    }



    public PaymentConfig GetConfiguration(string machineId, AppDbContext dbContext)
    {
        PaymentConfig config = new PaymentConfig() { Upi = true };
        try
        {
            var paymentSetting = dbContext.PaymentSettings.Find(machineId);
            if (paymentSetting is not null)
            {
                config.Upi = paymentSetting.Upi;
                config.Cash = paymentSetting.Cash;
                config.Card = paymentSetting.Card;
                config.Account = paymentSetting.Account;
                config.Counter = paymentSetting.Counter;

                if (paymentSetting.Account)
                {
                    var accSettings = dbContext.PaymentAccountSettings.Find(machineId);
                    if (accSettings is not null)
                    {
                        AccountConfig accountConfig = new AccountConfig()
                        {
                            Plan = accSettings.AccountPlan,
                            AuthType = accSettings.AuthType,
                            DailyLimit = accSettings.DailyLimit,
                            MonthlyLimit = accSettings.MonthlyLimit
                        };
                        config.AccountConfig = accountConfig;

                        if (accSettings.AuthType == StrDir.AuthPage.FACE)
                        {
                            var faceSetting = dbContext.FaceDeviceSettings.Find(machineId);
                            if (faceSetting is not null)
                            {
                                config.FaceDeviceConfig = new FaceDeviceConfig() { IpAddress = faceSetting.IpAddress };
                            }
                        }
                    }
                }
            }
            return config;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
        return config;
    }

}
