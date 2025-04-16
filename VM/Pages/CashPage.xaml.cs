using CMS.Dto;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VM.Components;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class CashPage : Page
{
    private readonly IServiceProvider serviceProvider;
    private readonly IBillValidatorService billValidator;
    private readonly AppDbContext dbContext;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<CashPage> logger;

    DispatcherTimer tmrIdeal = new DispatcherTimer();

    int orderValue = 0;
    int total = 0;
    string denomination = string.Empty;

    int timeOut = 180;

    public CashPage(IServiceProvider serviceProvider, IBillValidatorService billValidator, AppDbContext dbContext, SpeechSynthesizer speech, ILogger<CashPage> logger)
    {
        InitializeComponent();

        this.serviceProvider = serviceProvider;
        this.billValidator = billValidator;
        this.dbContext = dbContext;
        this.speech = speech;
        this.logger = logger;

        billValidator.OnMessageRaised += BillValidator_OnMessageRaised;
        billValidator.OnAmountReceived += BillValidator_OnAmountReceived;
        tmrIdeal.Tick += TmrIdeal_Tick;
        tmrIdeal.Interval = TimeSpan.FromSeconds(1);
    }


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            btnBack.IsEnabled = true;
            timeOut = 20;

            orderValue = (int)DataStore.selectedProducts.Sum(x => x.amount);
            total = 0;
            denomination = "";

            lblPrice.Text = $"Order Total : ₹{orderValue}";

            sp200.Visibility = Visibility.Collapsed;
            sp100.Visibility = Visibility.Collapsed;
            sp50.Visibility = Visibility.Collapsed;
            sp20.Visibility = Visibility.Collapsed;
            sp10.Visibility = Visibility.Collapsed;
            spTotal.Visibility = Visibility.Collapsed;

            tmrIdeal.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void DialogHost_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            bool isReady = billValidator.InitializeAmountRequest(orderValue);
            if (!isReady)
            {
                speech.SpeakAsync("The Cash payment is not ready at the movement");
                lblMessage.Text = "The Cash payment is not ready at the movement. Sorry for the inconvenience. Please try other payments";
                btnBack.IsEnabled = false;
                await Task.Delay(3000);
                NavigationService.GoBack();
            }
            else
            {
                speech.SpeakAsync("Please insert the note one by one");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void BillValidator_OnAmountReceived(Dictionary<int, int> notes)
    {
        Dispatcher.Invoke((Action)(() =>
        {
            AmountReceived(notes);
        }));
    }

    private void BillValidator_OnMessageRaised(string message)
    {
        try
        {
            Dispatcher.Invoke((Action)(() =>
            {
                DisplayMsg(message);
            }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void AmountReceived(Dictionary<int, int> notes)
    {
        try
        {
            timeOut = 20;
            total = 0;
            denomination = "";

            if (notes[10] > 0)
            {
                sp10.Visibility = Visibility.Visible;
                txt10Count.Text = notes[10].ToString();
                txt10Value.Text = (notes[10] * 10).ToString();
                total = total + (notes[10] * 10);

                denomination = $"10 x {notes[10]}";
            }

            if (notes[20] > 0)
            {
                sp20.Visibility = Visibility.Visible;
                txt20Count.Text = notes[20].ToString();
                txt20Value.Text = (notes[20] * 20).ToString();
                total = total + (notes[20] * 20);

                denomination = denomination + (denomination.Length > 0 ? " \n" : "");
                denomination = denomination + $"20 x {notes[20]}";
            }


            if (notes[50] > 0)
            {
                sp50.Visibility = Visibility.Visible;
                txt50Count.Text = notes[50].ToString();
                txt50Value.Text = (notes[50] * 50).ToString();
                total = total + (notes[50] * 50);

                denomination = denomination + (denomination.Length > 0 ? " \n" : "");
                denomination = denomination + $"50 x {notes[50]}";
            }

            if (notes[100] > 0)
            {
                sp100.Visibility = Visibility.Visible;
                txt100Count.Text = notes[100].ToString();
                txt100Value.Text = (notes[100] * 100).ToString();
                total = total + (notes[100] * 100);

                denomination = denomination + (denomination.Length > 0 ? " \n" : "");
                denomination = denomination + $"100 x {notes[100]}";
            }

            if (notes[200] > 0)
            {
                sp200.Visibility = Visibility.Visible;
                txt200Count.Text = notes[200].ToString();
                txt200Value.Text = (notes[200] * 200).ToString();
                total = total + (notes[200] * 200);

                denomination = denomination + (denomination.Length > 0 ? " \n" : "");
                denomination = denomination + $"200 x {notes[200]}";
            }

            if (total > 0)
            {
                spTotal.Visibility = Visibility.Visible;
                txtTotal.Text = total.ToString();
            }

            if (total >= orderValue)
            {
                logger.LogInformation($"Total Amount Received : {total}. Denomination : {denomination.Replace('\n', '\t')}");

                DataStore.PaymentDto = new OrderPaymentDto()
                {
                    PaymentType = StrDir.PaymentType.CASH,
                    Amount = total,
                    IsPaid = true,
                    Reference = denomination
                };

                speech.SpeakAsync($"Rupees {total} received on cash");

                if (DataStore.AppType == StrDir.AppType.VM)
                {
                    NavigationService.Navigate(serviceProvider.GetRequiredService<VendingPage>());
                }
                else if (DataStore.AppType == StrDir.AppType.KIOSK)
                {
                    NavigationService.Navigate(serviceProvider.GetRequiredService<KioskVendingPage>());
                }
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void CancelTransaction(string mobileNumber = "")
    {
        try
        {
            btnBack.IsEnabled = false;

            if (string.IsNullOrEmpty(mobileNumber))
                mobileNumber = await GetMobileNumber();

            if (mobileNumber.Length != 10)
            {
                DisplayMsg("Please enter vaild mobile number");
                return;
            }

            logger.LogInformation($"Cash order cancelled. Order : {DataStore.orderNumber}, Mobile : {mobileNumber}, Amount : {total}. Denomination : {denomination.Replace('\n', '\t')}");

            CashRefund cashRefund = new CashRefund()
            {
                RefId = Guid.NewGuid().ToString("N").ToUpper(),
                OrderNumber = DataStore.orderNumber,
                MobileNumber = mobileNumber,
                Denomination = denomination,
                Amount = total,
                CancelOn = DateTime.Now,
                IsViewed = false
            };

            dbContext.CashRefunds.Add(cashRefund);
            await dbContext.SaveChangesAsync();

            var eventService = serviceProvider.GetRequiredService<IEventService>();
            eventService.RaiseCashPaymentCanceled(cashRefund.Id);

            NavigationService.Navigate(serviceProvider.GetRequiredService<HomePage>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        finally
        {
            btnBack.IsEnabled = true;
        }
    }

    private async Task<string> GetMobileNumber()
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgCashPageHost"))
                DialogHost.Close("pgCashPageHost");

            var sampleMessageDialog = new MobileNumberDialog { Message = { Text = "To cancel the order and get the cash back \n Please enter your mobile number" } };
            var result = await DialogHost.Show(sampleMessageDialog, "pgCashPageHost");
            var isEntered = Convert.ToBoolean(result);
            if (isEntered)
                return sampleMessageDialog.txtInput.Text.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        return string.Empty;
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        if (total > 0)
        {
            CancelTransaction();
        }
        else
        {
            NavigationService.GoBack();
        }
    }

    private void TmrIdeal_Tick(object? sender, EventArgs e)
    {
        try
        {
            timeOut--;

            if (timeOut <= 0)
            {
                tmrIdeal.Stop();
                if (total > 0)
                {
                    CancelTransaction("0000000000");
                }
                else
                {
                    NavigationService.Navigate(serviceProvider.GetRequiredService<HomePage>());
                }
            }

            lblSecondsleft.Text = $"{timeOut} Seconds";

        }
        catch (Exception ex)
        {
            logger.LogInformation(ex.Message, ex);
        }
    }


    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        tmrIdeal.Stop();
        billValidator.DisableValidator();
    }

    private async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgCashPageHost"))
                DialogHost.Close("pgCashPageHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgCashPageHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

}
