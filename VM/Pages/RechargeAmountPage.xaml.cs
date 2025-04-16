using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VM.Components;

namespace VM.Pages;

public partial class RechargeAmountPage : Page
{
    private readonly IServiceProvider serviceProvider;
    TextBox txtInput;
    public string UserId = string.Empty;

    public RechargeAmountPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        this.serviceProvider = serviceProvider;
        txtInput = txtAmount;
    }


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        txtAmount.Text = "";
    }



    private void btnUpi_Click(object sender, RoutedEventArgs e)
    {
        int.TryParse(txtInput.Text, out int amount);
        if (amount > 0)
        {
            RechargeUPIpage next = serviceProvider.GetRequiredService<RechargeUPIpage>();
            next.UserId = UserId;
            next.Amount = amount;
            NavigationService.Navigate(next);
        }
        else
        {
            DisplayMsg("Please enter valid amount");
        }

    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(serviceProvider.GetRequiredService<UserHomePage>());
    }


    private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        TextBox textBox = (sender as TextBox)!;
        txtInput = textBox;
    }

    private void btnNumber_Click(object sender, RoutedEventArgs e)
    {
        Button btn = (Button)sender;

        txtInput.Text = btn.Tag.ToString() switch
        {
            "DEL" => txtInput.Text.Length > 0 ? txtInput.Text.Substring(0, txtInput.Text.Length - 1) : "",
            "CE" => "",
            _ => txtInput.Text += GetUpdateString(btn.Tag.ToString() ?? "", txtInput.MaxLength),
        };
        txtInput.Focus();
        txtInput.CaretIndex = txtInput.Text.Length;       
    }

    private string GetUpdateString(string input, int maxLength)
    {
        return (txtInput.MaxLength == 0 || (txtInput.MaxLength > 0 && txtInput.Text.Length < txtInput.MaxLength)) ? input : "";
    }

    public async void DisplayMsg(string msg)
    {
        if (DialogHost.IsDialogOpen("pgRechargeAmountPageHost"))
            DialogHost.Close("pgRechargeAmountPageHost");

        var sampleMessageDialog = new Dialog { Message = { Text = msg } };
        await DialogHost.Show(sampleMessageDialog, "pgRechargeAmountPageHost");
    }

}
