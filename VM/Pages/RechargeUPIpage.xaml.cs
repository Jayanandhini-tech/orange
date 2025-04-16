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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VM.Components;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class RechargeUPIpage : Page
{
    private readonly IServerClient httpClient;
    private readonly ISignalRClientService signalRClient;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<RechargeUPIpage> logger;

    BackgroundWorker bw_status = new BackgroundWorker();
    DispatcherTimer timer = new DispatcherTimer();

    public string UserId = string.Empty;
    public int Amount = 0;
    int timeout = 180;
    string rechargeId = "";

    public RechargeUPIpage(IServerClient httpClient, ISignalRClientService signalRClient, IServiceProvider serviceProvider, ILogger<RechargeUPIpage> logger)
    {
        InitializeComponent();
        this.httpClient = httpClient;
        this.signalRClient = signalRClient;
        this.serviceProvider = serviceProvider;
        this.logger = logger;

        bw_status.DoWork += Bw_status_DoWork;
        bw_status.RunWorkerCompleted += Bw_status_RunWorkerCompleted;

        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += Timer_Tick;
    }


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        signalRClient.OnPaymentStatusReceived += OnPaymentStatusReceived;
        btnBack.IsEnabled = true;

        lblPrice.Text = string.Format(new CultureInfo("en-IN"), "{0:C}", Amount);
        lblDisplayName.Text = "Generating QR Code, Please wait!";

        lblSecondsleft.Visibility = Visibility.Collapsed;
        lblSecondsleft2.Visibility = Visibility.Collapsed;
        img_QR.Visibility = Visibility.Collapsed;

        var qr = await GetRechargeQr();

        if (qr.IsSuccess)
        {
            byte[] binaryData = Convert.FromBase64String(qr.QRbase64png);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(binaryData);
            bi.EndInit();
            img_QR.Source = bi;

            rechargeId = qr.OrderNumber;

            lblDisplayName.Text = qr.DisplayName;
            img_QR.Visibility = Visibility.Visible;
            timeout = 180;
            lblSecondsleft.Text = $"{timeout} Seconds";
            lblSecondsleft.Visibility = Visibility.Visible;
            lblSecondsleft2.Visibility = Visibility.Visible;


            timer.Start();
        }
        else
        {

            lblDisplayName.Text = "The QR generated is failed. Please try after some times";
            btnBack.IsEnabled = false;
            await Task.Delay(3000);
            GoBack();
        }
    }


    private void Timer_Tick(object? sender, EventArgs e)
    {
        timeout--;
        lblSecondsleft.Text = $"{timeout} Seconds";

        if (timeout <= 0)
        {
            timer.Stop();
            CheckStatus();
        }
    }

    private async Task<QrImageDto> GetRechargeQr()
    {
        RechargeUpiDto qrCreate = new RechargeUpiDto(UserId, Amount);

        var response = await httpClient.PostAsync($"api/recharge/upi/create", qrCreate);
        string respText = await response.Content.ReadAsStringAsync();
        logger.LogInformation(respText);

        if (response.StatusCode == HttpStatusCode.OK)
            return JsonConvert.DeserializeObject<QrImageDto>(respText)!;

        return new QrImageDto("");
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
            if (status.OrderNumber == rechargeId)
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
            if (qrStatus.OrderNumber == rechargeId)
            {
                switch (qrStatus.Status)
                {
                    case PaymentStatusCode.PENDING:            // Pending                       
                        break;
                    case PaymentStatusCode.SUCCESS:            // Success                        
                        {
                            Success();
                        }
                        break;
                    case PaymentStatusCode.FAILED:             // Failed
                    default:
                        {
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
            var response = httpClient.GetAsync($"api/recharge/upi/status/gateway?rechargeId={rechargeId}").GetAwaiter().GetResult();
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

    private void GoBack()
    {
        var userHomePage = serviceProvider.GetRequiredService<UserHomePage>();
        NavigationService.Navigate(userHomePage);
    }

    private async void Success()
    {
        try
        {
            timer.Stop();
            DisplayMsg("Recharged Successfull");
            await Task.Delay(3000);

            if (DialogHost.IsDialogOpen("pgRechargeUPIHost"))
                DialogHost.Close("pgRechargeUPIHost");

            GoBack();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        GoBack();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        timer.Stop();
        signalRClient.OnPaymentStatusReceived -= OnPaymentStatusReceived;
    }

    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgRechargeUPIHost"))
                DialogHost.Close("pgRechargeUPIHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgRechargeUPIHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


}
