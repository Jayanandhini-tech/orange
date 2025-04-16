using CMS.Dto;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http;
using System.Speech.Synthesis;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;
using VM.Dtos;
using VM.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using VM.Domains;

namespace VM.Pages;

public partial class PinelabsUpiPage : Page
{
    private readonly AppDbContext dbContext;
    private readonly ISignalRClientService signalRClient;
    private readonly IOrderService orderService;
    private readonly IServiceProvider serviceProvider;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<PinelabsUpiPage> logger;
    private readonly DispatcherTimer timer = new DispatcherTimer();
    private readonly IPineLabsService pineLabsService;


    private string? _transactionId;
    private string? trId = string.Empty;
    public int _plPaymentStatus = -1;


    private double orderTotal = 0;
    private int timeout = 180;

    private string orderNumber = string.Empty;

    public PinelabsUpiPage(AppDbContext dbContext, ISignalRClientService signalRClient, IOrderService orderService,
        IServiceProvider serviceProvider, SpeechSynthesizer speech, ILogger<PinelabsUpiPage> logger, IPineLabsService pineLabsService)
    {
        InitializeComponent();

        this.dbContext = dbContext;
        this.signalRClient = signalRClient;
        this.orderService = orderService;
        this.serviceProvider = serviceProvider;
        this.speech = speech;
        this.logger = logger;
        this.pineLabsService = pineLabsService;
    }


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            timeout = 180;
            
            lblSecondsleft.Text = $"{timeout} Seconds";

            // UI Setup
            lblDisplayName.Text = "Transaction Initiating, please wait ..";
            lblPrice.Text = string.Empty;
            img_QR.Visibility = Visibility.Visible;

            if (!signalRClient.IsConnected())
                await signalRClient.ConnectAsync();

            btnBack.IsEnabled = false;
            orderTotal = DataStore.selectedProducts.Sum(x => x.amount);
            lblPrice.Text = string.Format(new CultureInfo("en-IN"), "{0:C}", orderTotal);

            // Start a fresh timer
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            // Get a new order number and start a new transaction
            orderNumber = await orderService.GetNextOrderNumberforUPI();
            //orderNumber = orderNumber.Replace("-", "");

            string paymentType = "UPI";
            await StartPaymentProcessing(paymentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Page_Loaded");
        }
    }

    private async Task StartPaymentProcessing(string paymentType)
    {
        //logger.LogInformation("Starting payment processing for Order Number: {OrderNumber}", orderNumber);
        var trId = await pineLabsService.ChargeTxn(orderNumber, orderTotal, paymentType);
        if (string.IsNullOrEmpty(trId))
        {
            logger.LogError("Failed to initiate transaction.");
            lblDisplayName.Text = "Transaction failed. Please try after time over";
            return;
        }

        logger.LogInformation($"Transaction initiated with Reference ID: {trId}");
        await CheckTransactionStatus(trId);
    }

    private async Task CheckTransactionStatus(string trId)
    {
        try
        {
            int retryCount = 0;
            const int maxRetries = 15; // Limit retries
            const int delay = 5000;    // Wait time between retries

            while (retryCount < maxRetries)
            {
                var response = await pineLabsService.GetTxnStatus(trId);
                logger.LogInformation("Payment Status: {Response}", response);

                if (response == 0)  // TXN APPROVED (Success)
                {
                    logger.LogInformation("Transaction approved. Reference: {TrId}", trId);

                    DataStore.PaymentDto = new OrderPaymentDto()
                    {
                        PaymentType = StrDir.PaymentType.UPI,
                        Amount = orderTotal,
                        IsPaid = true,
                        Reference = trId,
                        
                    };

                    lblDisplayName.Text = $"Transaction successful! Reference No: {trId}";
                    await Task.Delay(5000);
                    NavigationService?.Navigate(serviceProvider.GetRequiredService<VendingPage>());
                    return;  // **Exit on success**
                }
                else if (response == 1)  // TXN CANCELLED
                {
                    logger.LogWarning("Transaction was cancelled.");
                    lblDisplayName.Text = "Transaction failed. Please try after time over";
                    btnBack.IsEnabled = false;
                    return;  // **Exit on failure**
                }
                else if (response == 1001)  // TXN UPLOADED (Processing)
                {
                    if (retryCount % 5 == 0)  // Log every 5 retries
                        logger.LogInformation("Transaction is still in progress.");
                    lblDisplayName.Text = "Checking payment status.....";
                    await Task.Delay(5000);
                    lblDisplayName.Text = "Pay now";
                }
                else if (response == 1052)  // UPI Transaction Initiated
                {
                    logger.LogInformation("UPI transaction time over 120 sec");
                    lblDisplayName.Text = "Transaction Failed, please try after time overs";
                    btnBack.IsEnabled = false;
                }
                else  // Unexpected response
                {
                    logger.LogWarning("Unexpected status code: {Response}", response);
                    lblDisplayName.Text = "Checking payment status...";
                    btnBack.IsEnabled = false;
                    
                }

                retryCount++;
                await Task.Delay(delay);  
            }

            GoBack();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during transaction status check.");
        }
    }

 

    private async void Timer_Tick(object sender, EventArgs e)
    {
        timeout--;
        if (timeout <= 0)
        {
            timer.Stop();
            logger.LogInformation("Time expired.");
            await pineLabsService.CancelTxn(trId);
            await pineLabsService.ForcedCancelTxn(trId);
        }
        lblSecondsleft.Text = $"{timeout} Seconds";
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        GoBack();
    }

    private async void GoBack()
    {
        //logger.LogInformation("User pressed Back. Cancelling transaction (if any) and stopping timer.");

        //if (!string.IsNullOrEmpty(trId))
        //{
        //    logger.LogInformation("Cancelling transaction before navigating away. Transaction ID: {txnRefId }", trId);
        //    await pineLabsService.CancelTxn(trId);
        //        trId = string.Empty;
        //}

        timer.Stop(); // 🔹 Stop the timer
        NavigationService?.GoBack();
    }


    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {

        timer.Stop();
        timer.Tick -= Timer_Tick;  // Unsubscribe to avoid multiple event triggers
        trId = string.Empty;
        _plPaymentStatus = -1;
        _transactionId = string.Empty;
    }

}
