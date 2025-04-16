using CMS.Dto;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Numerics;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using VM.Components;
using VM.Dtos;

namespace VM.Pages;


public partial class PaymentOptionPage : Page
{
    private readonly IServiceProvider serviceProvider;
    private readonly PaymentConfig paymentConfig;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<PaymentOptionPage> logger;

    public PaymentOptionPage(IServiceProvider serviceProvider, PaymentConfig paymentConfig,
         SpeechSynthesizer speech, ILogger<PaymentOptionPage> logger)
    {
        InitializeComponent();
        this.serviceProvider = serviceProvider;
        this.paymentConfig = paymentConfig;

        this.speech = speech;

        this.logger = logger;
    }


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        cdAccount.Visibility = Visibility.Collapsed;
        cdCard.Visibility = Visibility.Collapsed;
        cdCash.Visibility = Visibility.Collapsed;
        cdUPI.Visibility = Visibility.Collapsed;
        cdCounter.Visibility = Visibility.Collapsed;

        if (paymentConfig.Account)
            cdAccount.Visibility = Visibility.Visible;

        if (paymentConfig.Cash)
            cdCash.Visibility = Visibility.Visible;

        if (paymentConfig.Upi)
            cdUPI.Visibility = Visibility.Visible;

        if (paymentConfig.Card)
            cdCard.Visibility = Visibility.Visible;

        if (DataStore.AppType == StrDir.AppType.KIOSK && paymentConfig.Counter)
            cdCounter.Visibility = Visibility.Visible;

        lblTotal.Text = string.Format(new CultureInfo("en-IN"), "{0:C}", DataStore.selectedProducts.Sum(x => x.amount));


        if (!NavigationService.CanGoForward)
        {
            CheckAutoPayemntOption();
        }

    }

    private void Cash_MouseDown(object sender, MouseButtonEventArgs e)
    {

        if (DataStore.selectedProducts.Sum(x => x.amount) % 10 != 0)
        {
            DisplayMsg("Rounded cash only(multiples of 10), please use other payment options.");
            return;
        }

        MoveNext("CASH");
    }

    private void UPI_MouseDown(object sender, MouseButtonEventArgs e)
    {
        MoveNext("UPI");
    }

    private void Account_MouseDown(object sender, MouseButtonEventArgs e)
    {
        MoveNext("ACCOUNT");
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        MoveNext("CARD");
    }


    private void Counter_MouseDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            logger.LogInformation($"Payment Type : {StrDir.PaymentType.COUNTER}");

            DataStore.PaymentDto = new OrderPaymentDto()
            {
                PaymentType = StrDir.PaymentType.COUNTER,
                Amount = DataStore.selectedProducts.Sum(x => x.amount),
                IsPaid = true,
                Reference = "Counter",
                PaymentId = ""
            };

            NavigationService.Navigate(serviceProvider.GetRequiredService<KioskVendingPage>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public void MoveNext(string paymentOption)
    {
        try
        {
            if (DataStore.selectedProducts.Sum(x => x.qty) < 1)
            {
                DisplayMsg("No item in the cart, Please add one or more items into cart");
                return;
            }

            logger.LogInformation($"Payment Type : {paymentOption}");
            speech.SpeakAsync($"{paymentOption}");

            Page nextPage = paymentOption switch
            {
                "UPI" => serviceProvider.GetRequiredService<UpiPage>(),
                "CASH" => serviceProvider.GetRequiredService<CashPage>(),
                "CARD" => serviceProvider.GetRequiredService<HomePage>(),
                "ACCOUNT" => GetAccountAuthPage(),
                _ => serviceProvider.GetRequiredService<HomePage>(),
            };

            NavigationService.Navigate(nextPage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private Page GetAccountAuthPage()
    {
        if (paymentConfig.AccountConfig.AuthType == StrDir.AuthPage.FACE)
            return serviceProvider.GetRequiredService<FaceLoginPage>();

        if (paymentConfig.AccountConfig.AuthType == StrDir.AuthPage.IDCARD)
            return serviceProvider.GetRequiredService<IdCardLoginPage>();

        return serviceProvider.GetRequiredService<HomePage>();
    }

    public void CheckAutoPayemntOption()
    {
        int totalPaymentOptions = Convert.ToInt16(paymentConfig.Account) +
                                    Convert.ToInt16(paymentConfig.Cash) +
                                    Convert.ToInt16(paymentConfig.Card) +
                                    Convert.ToInt16(paymentConfig.Upi) +
                                    Convert.ToInt16(paymentConfig.Counter);

        if (totalPaymentOptions == 1)
        {
            if (paymentConfig.Account)
                MoveNext("ACCOUNT");

            if (paymentConfig.Upi)
                MoveNext("UPI");

            if (paymentConfig.Cash)
            {
                if (DataStore.selectedProducts.Sum(x => x.amount) % 10 == 0)
                    MoveNext("CASH");
            }

            if (paymentConfig.Card)
                MoveNext("CARD");
        }
    }


    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(DataStore.orderNumber))
            NavigationService.Navigate(serviceProvider.GetRequiredService<CartPage>());
        else
            NavigationService.Navigate(serviceProvider.GetRequiredService<HomePage>());

    }


    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgPaymentOptionsHost"))
                DialogHost.Close("pgPaymentOptionsHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgPaymentOptionsHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

}
