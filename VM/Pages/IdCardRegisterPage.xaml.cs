using CMS.Dto;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using VM.Components;
using VM.Services.Interfaces;

namespace VM.Pages;

public partial class IdCardRegisterPage : Page
{
    private readonly IServerClient httpClient;
    private readonly ILogger<IdCardRegisterPage> logger;

    TextBox txtInput;
    public string IdCardNumber = "0123456789021";

    public IdCardRegisterPage(IServerClient httpClient, ILogger<IdCardRegisterPage> logger)
    {
        InitializeComponent();
        txtInput = txtRollNo;

        this.httpClient = httpClient;
        this.logger = logger;
    }


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        txtIdCardNo.Text = IdCardNumber;
    }


    private async void btnNext_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (txtRollNo.Text.Trim().Length == 0)
            {
                DisplayMsg("Please enter rollnumber");
                return;
            }

            if (txtName.Text.Trim().Length == 0)
            {
                DisplayMsg("Please enter name");
                return;
            }

            AppUserIdCardDto appUser = new AppUserIdCardDto(txtRollNo.Text.Trim(), txtName.Text.Trim(), IdCardNumber);

            btnNext.IsEnabled = false;

            var response = await httpClient.PostAsync("api/appuser/idcard", appUser);

            if (response.IsSuccessStatusCode)
            {
                var registerResponse = await response.Content.ReadFromJsonAsync<AppUserRegisterResponse>();

                if (registerResponse is null)
                {
                    DisplayMsg("Registration failed");

                }
                else
                {
                    DisplayMsg(registerResponse.Message);

                    if (registerResponse.Success)
                        NavigationService.GoBack();
                }
            }
            else
            {
                var responseText = await response.Content.ReadAsStringAsync();
                logger.LogInformation(responseText);
                DisplayMsg(responseText);
            }
            btnNext.IsEnabled = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            btnNext.IsEnabled = true;
        }
    }


    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.GoBack();
    }


    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgIdCardRegisterHost"))
                DialogHost.Close("pgIdCardRegisterHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgIdCardRegisterHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void TextBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

}
