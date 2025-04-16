using CMS.Dto;
using CMS.Dto.Payments;
using CMS.Dto.Payments.QR;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using VM.Components;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class UpiPage : Page
{

    private readonly IServerClient httpClient;
    private readonly ISignalRClientService signalRClient;
    private readonly IOrderService orderService;
    private readonly IServiceProvider serviceProvider;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<UpiPage> logger;


    BackgroundWorker bw_status = new BackgroundWorker();
    DispatcherTimer timer = new DispatcherTimer();

    double orderTotal = 0;
    string upiNumber = string.Empty;
    int timeout = 20;
    QrImageDto qr = new QrImageDto("");



    public UpiPage(IServerClient httpClient, ISignalRClientService signalRClient, IOrderService orderService,
        IServiceProvider serviceProvider, SpeechSynthesizer speech, ILogger<UpiPage> logger, IJuiceService juiceService)
    {
        InitializeComponent();
        this.httpClient = httpClient;
        this.signalRClient = signalRClient;
        this.orderService = orderService;

        this.serviceProvider = serviceProvider;
        this.speech = speech;
        this.logger = logger;
      
        bw_status.DoWork += Bw_status_DoWork;
        bw_status.RunWorkerCompleted += Bw_status_RunWorkerCompleted;

    }


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            lblPrice.Text = "";
            img_QR.Source = null;

            if (!signalRClient.IsConnected())
                await signalRClient.ConnectAsync();

            signalRClient.OnPaymentStatusReceived += OnPaymentStatusReceived;

            btnBack.IsEnabled = true;
            orderTotal = DataStore.selectedProducts.Sum(x => x.amount);

            lblPrice.Text = string.Format(new CultureInfo("en-IN"), "{0:C}", orderTotal);
            lblDisplayName.Text = "Generating QR Code, Please wait!";

            lblSecondsleft.Visibility = Visibility.Collapsed;
            lblSecondsleft2.Visibility = Visibility.Collapsed;
            img_QR.Visibility = Visibility.Collapsed;


            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timeout = 20;

            upiNumber = await orderService.GetNextOrderNumberforUPI();
          
            qr = await GetQr(upiNumber, orderTotal);

            if (qr.IsSuccess)
            {
                byte[] binaryData = Convert.FromBase64String(qr.QRbase64png);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();
                img_QR.Source = bi;


                lblDisplayName.Text = qr.DisplayName;
                img_QR.Visibility = Visibility.Visible;
                lblSecondsleft.Text = $"{timeout} Seconds";
                lblSecondsleft.Visibility = Visibility.Visible;
                lblSecondsleft2.Visibility = Visibility.Visible;


                timer.Start();
                speech.SpeakAsync($"Please scan and pay");
            }
            else
            {

                lblDisplayName.Text = "The QR generated is failed. Please try other payments";
                speech.SpeakAsync($"The QR generated is failed");
                btnBack.IsEnabled = false;
                await Task.Delay(3000);
                GoBack();

            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }


    private async Task<QrImageDto> GetQr(string orderNumber, double amount)
    {
        try
        {
            QrCreateDto qrCreate = new QrCreateDto(orderNumber, amount);
            var response = await httpClient.PostAsync($"api/payments/upi/create", qrCreate);
            string respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation(respText);

            if (response.StatusCode == HttpStatusCode.OK)
                return JsonConvert.DeserializeObject<QrImageDto>(respText)!;

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        return new QrImageDto(orderNumber, false);
    }

   
    private void Timer_Tick(object? sender, EventArgs e)
    {
        timeout--;

        if (timeout <= 0)
        {
            timer.Stop();
            CheckStatus();
        }
        lblSecondsleft.Text = $"{timeout} Seconds";
    }

    private void CheckStatus()
    {
        signalRClient.OnPaymentStatusReceived -= OnPaymentStatusReceived;
        if (!bw_status.IsBusy)
            bw_status.RunWorkerAsync();
    }

  
    private void OnPaymentStatusReceived(QrStatusDto status)
    {
        logger.LogInformation(JsonConvert.SerializeObject(status));

        if (status is not null)
        {
            if (status.OrderNumber == upiNumber)
            {
                timer.Stop();
                Dispatcher.Invoke((Action)(() =>
                {
                    ProcessStatus(status);
                }));
            }
        }
    }

    private async void ProcessStatus(QrStatusDto qrStatus)
    {
        try
        {
            if (qrStatus.OrderNumber == upiNumber)
            {
                switch (qrStatus.Status)
                {
                    case PaymentStatusCode.PENDING:            // Pending                       
                        break;
                    case PaymentStatusCode.SUCCESS:            // Success                        
                        {
                            DataStore.PaymentDto = new OrderPaymentDto()
                            {
                                PaymentType = StrDir.PaymentType.UPI,
                                Amount = orderTotal,
                                IsPaid = true,
                                Reference = upiNumber,
                                PaymentId = qrStatus.PaymentId
                            };
                            MoveNextAsync();
                        }
                        break;
                    case PaymentStatusCode.FAILED:             // Failed
                    default:
                        {
                            logger.LogInformation(JsonConvert.SerializeObject(qrStatus));
                            lblDisplayName.Text = "Transaction failed";
                            btnBack.IsEnabled = false;
                            await Task.Delay(3000);
                            GoBack();
                        }
                        break;
                }

                if (timeout <= 0 && qrStatus.Status == PaymentStatusCode.PENDING)
                    GoBack();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void Bw_status_DoWork(object? sender, DoWorkEventArgs e)
    {
        try
        {
            var response = httpClient.GetAsync($"api/payments/upi/status/gateway?orderNumber={upiNumber}").GetAwaiter().GetResult();
            string respText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (response.StatusCode != HttpStatusCode.OK)
                logger.LogInformation(respText);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var qrStatus = JsonConvert.DeserializeObject<QrStatusDto>(respText)!;
                e.Result = qrStatus;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void Bw_status_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        if (e.Result != null)
        {
            QrStatusDto qrStatus = (QrStatusDto)e.Result;
            ProcessStatus(qrStatus);
        }

    }



    private void MoveNextAsync()
    {
        speech.SpeakAsync($"Rupees {DataStore.PaymentDto.Amount} received on UPI");

        if (DataStore.AppType == StrDir.AppType.VM)
        {
            NavigationService.Navigate(serviceProvider.GetRequiredService<VendingPage>());
        }
        else if (DataStore.AppType == StrDir.AppType.KIOSK)
        {
            NavigationService.Navigate(serviceProvider.GetRequiredService<KioskVendingPage>());
        }
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        GoBack();
    }

    private void GoBack()
    {
        NavigationService.GoBack();
    }

    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgUPIHost"))
                DialogHost.Close("pgUPIHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgUPIHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        timer.Stop();
        timer.Tick -= Timer_Tick;
        signalRClient.OnPaymentStatusReceived -= OnPaymentStatusReceived;
    }
}
