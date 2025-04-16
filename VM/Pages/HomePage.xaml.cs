using CMS.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class HomePage : Page
{

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly PaymentConfig paymentConfig;
    private readonly IModbus modbus;
    private readonly SpeechSynthesizer speech;
    private readonly ISyncService syncService;
    private readonly ILogger<HomePage> logger;


    DispatcherTimer tmrAdmin = new DispatcherTimer();
    int adminTmrCount = 0;

    public HomePage(IServiceScopeFactory serviceScopeFactory, PaymentConfig paymentConfig, IModbus modbus, SpeechSynthesizer speech, ILogger<HomePage> logger, ISyncService syncService)
    {
        InitializeComponent();
        this.serviceScopeFactory = serviceScopeFactory;
        this.paymentConfig = paymentConfig;
        this.syncService = syncService;
        this.modbus = modbus;
        this.speech = speech;
        this.logger = logger;

        tmrAdmin.Interval = TimeSpan.FromSeconds(1);
        tmrAdmin.Tick += Tmr_admin_Tick;



        btnLeft.Visibility = Visibility.Collapsed;
    }
    
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {

        if (!string.IsNullOrEmpty(DataStore.orderNumber))
        {
            var scope = serviceScopeFactory.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var juiceService = scope.ServiceProvider.GetRequiredService<IJuiceService>();
            await juiceService.UpdateOrderStatus(DataStore.orderNumber);
            await orderService.UpdateOrderStatus(DataStore.orderNumber);
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
            eventService.RaiseOrderCompleteEvent(DataStore.orderNumber);
            await syncService.StockUpdateAsync();
            await syncService.PushStockClearedAsync();
            await syncService.PushStockRefillAsync();
        }
       
        DataStore.selectedProducts = [];
        DataStore.orderNumber = "";
        DataStore.deliveryType = "";
        DataStore.PaymentDto = new OrderPaymentDto();

        if (DataStore.AppType == StrDir.AppType.VM)
        {
            lblRight.Text = "ORDER\r\nJUICE";
            btnRight.Tag = "VEND";
            adminTmrCount = 0;
            bool isOpen = modbus.Open();
            if (isOpen)
                tmrAdmin.Start();
        }

        if (DataStore.AppType == StrDir.AppType.KIOSK)
        {
            btnRight.Tag = "DINE-IN";
        }


        if (paymentConfig.Account == true && paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID)
        {
            btnLeft.Visibility = Visibility.Visible;

            lblLeft.Text = "ACCOUNT";
            btnLeft.Tag = "";
        }


        RemoveAll();
        PlayNextVideo();
    }
    
    private void btnLeft_Click(object sender, RoutedEventArgs e)
    {
        DataStore.deliveryType = "";
        MovePage(GetAccountAuthPage());
    }

    private void btnNext_Click(object sender, RoutedEventArgs e)
    {
        DataStore.deliveryType = ((Button)sender).Tag.ToString() ?? "";
        MovePage(typeof(JuicePage));
    }

    private void MovePage(Type type)
    {
        try
        {
            tmrAdmin.Stop();
            modbus.Close();
            speech.SpeakAsync("Welcome");
            var scope = serviceScopeFactory.CreateScope();
            NavigationService?.Navigate(scope.ServiceProvider.GetRequiredService(type));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    private Type GetAccountAuthPage()
    {
        if (paymentConfig.AccountConfig.AuthType == StrDir.AuthPage.FACE)
            return typeof(FaceLoginPage);

        if (paymentConfig.AccountConfig.AuthType == StrDir.AuthPage.IDCARD)
            return typeof(IdCardLoginPage);

        return typeof(HomePage);
    }

    private void meVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        PlayNextVideo();
    }

    private void meVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        PlayNextVideo();
    }

    private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Tab))
            {
                //if (DataStore.AppType == StrDir.AppType.VM)
                //    MovePage(typeof(OperatorHomePage));
                MovePage(typeof(OperatorHomePage));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void PlayNextVideo()
    {
        try
        {
            if (DataStore.videos.Count > 0)
            {
                DataStore.playIndex++;

                DataStore.playIndex = DataStore.playIndex >= DataStore.videos.Count ? 0 : DataStore.playIndex;

                meVideo.Source = new Uri(DataStore.videos[DataStore.playIndex]);
            }
        }
        catch
        {

        }
    }

    private void RemoveAll()
    {
        while (NavigationService.CanGoBack)
        {
            try
            {
                NavigationService.RemoveBackEntry();
            }
            catch
            {
                break;
            }
        }
    }

    private void Tmr_admin_Tick(object? sender, EventArgs e)
    {
        try
        {
            adminTmrCount++;
            bool adminRequested = modbus.AdminButtonPressed();

            //logger.LogInformation($"Button Pressed : {adminRequested}, Count : {adminTmrCount}, MODBUS Status : {modbus.modbusStatus}");


            if (adminRequested)
                MovePage(typeof(OperatorHomePage));

            if (adminTmrCount > 60)
            {
                tmrAdmin.Stop();
                modbus.Close();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        tmrAdmin.Stop();
        modbus.Close();
    }

}
