using CMS.Dto;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM.Components;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class UserHomePage : Page
{

    private readonly IServiceProvider serviceProvider;
    private readonly PaymentConfig paymentConfig;
    private readonly IUserService userService;

    private readonly ILogger<UserHomePage> logger;
    public AppUserDto user = new AppUserDto("", "", "", 0, "");
    public bool isRecharged = false;

    private int todaySpend = 0;
    private int monthSpend = 0;
    private bool spendCalculated = false;

    public UserHomePage(IServiceProvider serviceProvider, PaymentConfig paymentConfig, IUserService userService, ILogger<UserHomePage> logger)
    {
        InitializeComponent();

        this.serviceProvider = serviceProvider;
        this.paymentConfig = paymentConfig;
        this.userService = userService;
        this.logger = logger;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Id))
            {
                NavigationService.GoBack();
                return;
            }
            lblBalance.Text = "";
            lblTotal.Text = string.Format(new CultureInfo("en-IN"), "{0:C}", DataStore.selectedProducts.Sum(x => x.amount));
            lblName.Text = $"Welcome {user.Name}!";
            lblId.Text = $"Id : {user.Id}";
            //DisplayUserBalance();

            btnNext.Visibility = Visibility.Visible;
            btnRecharge.Visibility = Visibility.Hidden;
            dgRecharge.Visibility = Visibility.Hidden;

            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID)
            {
                btnRecharge.Visibility = Visibility.Visible;
                dgRecharge.Visibility = Visibility.Visible;
            }

            if (DataStore.selectedProducts.Count == 0)
            {
                lblTotal.Text = "";
                btnNext.Visibility = Visibility.Collapsed;
            }
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
            if (!string.IsNullOrEmpty(user.ImagePath))
            {
                if (File.Exists(user.ImagePath))
                {
                    ImageBrush imgBrush = new ImageBrush();
                    imgBrush.ImageSource = new BitmapImage(new Uri(user.ImagePath, UriKind.Relative));
                    brPhoto.Background = imgBrush;
                }
            }

            if (isRecharged)
                isRecharged = false;

            _ = Task.Run(async () =>
            {
                await GetUserBalance();
                await DisplayTransactions();
                if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.POSTPAID &&
                     (paymentConfig.AccountConfig.DailyLimit > 0 || paymentConfig.AccountConfig.MonthlyLimit > 0))
                {
                    await CalculateSpendAsync();
                }
            });



            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.POSTPAID && paymentConfig.AccountConfig.AuthType == StrDir.AuthPage.FACE)
            {
                btnNext.IsEnabled = false;
                await Task.Delay(2000);
                MoveNext();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task DisplayTransactions()
    {
        try
        {
            var resp = await userService.GetTransactionsAsync(user.Id);
            if (resp.Success)
            {
                Dispatcher.Invoke(() =>
                {
                    dgRecharge.ItemsSource = resp.Recharges.OrderByDescending(x => x.RechargeDate).ToList();
                    dgTransactions.ItemsSource = resp.Orders.OrderByDescending(x => x.OrderDate).ToList();
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private async Task GetUserBalance()
    {
        try
        {
            double balance = await userService.GetBalanceAsync(user.Id);
            user = new AppUserDto(user.Id, user.Name, user.IdCardNo, balance, user.ImagePath);
            DisplayUserBalance();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void DisplayUserBalance()
    {
        try
        {
            string msg = string.Empty;

            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.POSTPAID)
                msg = $"Spend : {user.Balance}";

            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID)
                msg = $"Balance : {user.Balance}";


            Dispatcher.Invoke(() => { lblBalance.Text = msg; });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private async Task CalculateSpendAsync()
    {
        try
        {
            var spend = await userService.GetLocalSpendAsync(user.Id);
            todaySpend = (int)spend.today;
            monthSpend = (int)spend.month;
            spendCalculated = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            spendCalculated = true;
        }
    }

    private void btnRecharge_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            isRecharged = true;
            RechargeAmountPage rechargeAmount = serviceProvider.GetRequiredService<RechargeAmountPage>();
            rechargeAmount.UserId = user.Id;
            NavigationService.Navigate(rechargeAmount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void btnNext_Click(object sender, RoutedEventArgs e)
    {
        MoveNext();
    }

    private async void MoveNext()
    {
        try
        {
            btnNext.IsEnabled = false;

            if (DataStore.selectedProducts is null)
                return;

            double amount = DataStore.selectedProducts.Sum(x => x.amount);

            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID && user.Balance < amount)
            {
                DisplayMsg("Sorry, you do not have enough balance to process this order.");
                return;
            }


            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.PREPAID)
            {
                var accountPayment = new AppUserOrderPayment(user.Id, DataStore.orderNumber, amount);
                var result = await userService.ConfirmOrderPayment(user.Id, amount);
                if (result is null || result.Success == false)
                {
                    DisplayMsg(result?.Message ?? "Payment failed");
                    return;
                }
            }


            if (paymentConfig.AccountConfig.Plan == StrDir.AccountPlan.POSTPAID)
            {
                if ((paymentConfig.AccountConfig.DailyLimit > 0 || paymentConfig.AccountConfig.MonthlyLimit > 0) && spendCalculated == false)
                {
                    DisplayProcessDialog();
                    int wait_count = 0;
                    while (spendCalculated == false && wait_count < 20)
                    {
                        await Task.Delay(500);
                        wait_count++;
                    }
                    HideDialog();
                }

                if (paymentConfig.AccountConfig.DailyLimit > 0)
                {
                    if (todaySpend >= paymentConfig.AccountConfig.DailyLimit)
                    {
                        DisplayMsg("Sorry, you consumed your today's limit.");
                        return;
                    }

                    if (amount > paymentConfig.AccountConfig.DailyLimit)
                    {
                        DisplayMsg($"Please order less than {paymentConfig.AccountConfig.DailyLimit}");
                        return;
                    }

                    if ((todaySpend + amount) > paymentConfig.AccountConfig.DailyLimit)
                    {
                        int allowed = (int)paymentConfig.AccountConfig.DailyLimit - todaySpend;
                        DisplayMsg($"Please order less than {allowed}");
                        return;
                    }
                }

                if (paymentConfig.AccountConfig.MonthlyLimit > 0)
                {
                    if (monthSpend >= paymentConfig.AccountConfig.MonthlyLimit)
                    {
                        DisplayMsg("Sorry, you finished your monthly limit.");
                        return;
                    }

                    if ((monthSpend + amount) > paymentConfig.AccountConfig.MonthlyLimit)
                    {
                        int allowed = (int)paymentConfig.AccountConfig.MonthlyLimit - monthSpend;
                        DisplayMsg($"Please order less than {allowed}");
                        return;
                    }
                }
            }

            DataStore.PaymentDto = new OrderPaymentDto()
            {
                PaymentType = StrDir.PaymentType.ACCOUNT,
                Amount = amount,
                IsPaid = true,
                Reference = $"{paymentConfig.AccountConfig.Plan} - {paymentConfig.AccountConfig.AuthType}",
                UserId = user.Id,
            };

            if (DataStore.AppType == StrDir.AppType.VM)
            {
                NavigationService.Navigate(serviceProvider.GetRequiredService<VendingPage>());
            }
            else if (DataStore.AppType == StrDir.AppType.KIOSK)
            {
                NavigationService.Navigate(serviceProvider.GetRequiredService<KioskVendingPage>());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
        finally
        {
            btnNext.IsEnabled = true;
        }
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        if (DataStore.selectedProducts.Count == 0)
        {
            NavigationService.Navigate(serviceProvider.GetRequiredService<HomePage>());
        }
        else
        {
            NavigationService.Navigate(serviceProvider.GetRequiredService<PaymentOptionPage>());
        }
    }

    private async void DisplayMsg(string msg)
    {
        try
        {
            HideDialog();
            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgUserHomePageHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private async void DisplayProcessDialog()
    {
        try
        {
            HideDialog();
            await DialogHost.Show(new ProcessingDialog(), "pgUserHomePageHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void HideDialog()
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgUserHomePageHost"))
                DialogHost.Close("pgUserHomePageHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}
