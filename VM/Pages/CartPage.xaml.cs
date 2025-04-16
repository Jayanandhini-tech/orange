using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VM.Components;
using VM.Dtos;
namespace VM.Pages;


public partial class CartPage : Page
{

    private readonly IServiceProvider serviceProvider;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<CartPage> logger;


    int qty = 0;
    double amount = 0;

    public CartPage(IServiceProvider serviceProvider, SpeechSynthesizer speech, ILogger<CartPage> logger)
    {
        InitializeComponent();

        this.serviceProvider = serviceProvider;
        this.speech = speech;
        this.logger = logger;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            lstCart.ItemsSource = DataStore.selectedProducts;

            txtPlaceCup.Visibility = DataStore.categoryFilter == "juice" ? Visibility.Visible : Visibility.Collapsed;
            UpdateCart();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    
    private void BtnPlus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string productId = ((Button)sender).Tag.ToString() ?? "";
            var product = DataStore.selectedProducts.FirstOrDefault(x => x.Id == productId);
            if (product == null)
                return;

            if (product.Stock > product.qty)
            {
                product.qty++;
                speech.SpeakAsync($"{product.Name} {product.qty} quantity");
            }
            else
            {
                DisplayMsg($"Only {product.Stock} number(s) left");
                speech.SpeakAsync($"Only {product.Stock} quantity left");
            }
            UpdateCart();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void BtnMinus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string productId = ((Button)sender).Tag.ToString() ?? "";
            var product = DataStore.selectedProducts.FirstOrDefault(x => x.Id == productId);
            if (product == null)
                return;

            if (product.qty > 0)
                product.qty--;

            speech.SpeakAsync($"{product.Name} {product.qty} quantity");
            UpdateCart();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void btnNext_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (qty < 1)
            {
                DisplayMsg("No item in the cart, Please add one or more items into cart");
                return;
            }
            DataStore.selectedProducts = DataStore.selectedProducts.Where(x => x.qty > 0).ToList();
            DataStore.orderNumber = string.Empty;
            LogOrder();
            NavigationService.Navigate(serviceProvider.GetRequiredService<PaymentOptionPage>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(serviceProvider.GetRequiredService<OrderPage>());
    }

    private void UpdateCart()
    {
        qty = DataStore.selectedProducts.Sum(x => x.qty);
        amount = DataStore.selectedProducts.Sum(x => x.amount);

        lblTotal.Text = string.Format(new CultureInfo("en-IN"), "{0:C}", amount);
    }

    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgCartHost"))
                DialogHost.Close("pgCartHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgCartHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    private void LogOrder()
    {
        try
        {
            logger.LogInformation("\n\n");
            logger.LogInformation("----------------------------------------------------------------------------");
            foreach (var item in DataStore.selectedProducts)
            {
                logger.LogInformation($"{item.Name.PadRight(30)}  {item.Price.ToString("0.00").PadLeft(6)}  {item.qty.ToString().PadLeft(3)}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}
