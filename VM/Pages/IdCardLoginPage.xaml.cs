using CMS.Dto;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using VM.Components;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;


namespace VM.Pages;

public partial class IdCardLoginPage : Page
{
    private readonly IServerClient httpClient;
    private readonly IServiceProvider serviceProvider;
    private readonly AppDbContext dbContext;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<IdCardLoginPage> logger;

    string cardId = "";
    bool optRegister = false;

    public IdCardLoginPage(IServerClient httpClient, IServiceProvider serviceProvider, AppDbContext dbContext, SpeechSynthesizer speech, ILogger<IdCardLoginPage> logger)
    {
        InitializeComponent();
        this.httpClient = httpClient;
        this.serviceProvider = serviceProvider;
        this.dbContext = dbContext;
        this.speech = speech;
        this.logger = logger;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        txtIdCardNumber.AcceptsReturn = true;
        lblMessage.Text = "";
        txtIdCardNumber.Focus();
        txtIdCardNumber.KeyUp += txtIdCardNumber_KeyUp;
        speech.SpeakAsync("Show your id card");
    }



    private async void txtIdCardNumber_KeyUp(object sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.Enter)
            {
                lblMessage.Text = "";
                cardId = txtIdCardNumber.Text.Trim();
                logger.LogInformation($"Tapped Id Card : {cardId}");

                if (cardId.Length > 0)
                {
                    txtIdCardNumber.KeyUp -= txtIdCardNumber_KeyUp;

                    lblMessage.Text = "Please wait, retriving your information";

                    var appUser = await GetCardInfo(cardId);

                    if (appUser is not null)
                    {
                        bool userExist = await dbContext.AppUsers.AnyAsync(x => x.Id == appUser.Id);
                        if (!userExist)
                        {
                            await dbContext.AppUsers.AddAsync(new AppUser
                            {
                                Id = appUser.Id,
                                Name = appUser.Name,
                                CreditLimit = 1000,
                                ImagePath = string.Empty,
                                CreatedOn = DateTime.Now,
                                IsViewed = true,
                                IsActive = true
                            });
                            await dbContext.SaveChangesAsync();
                        }

                        speech.SpeakAsync($"Hello {appUser.Name}");

                        var userHome = serviceProvider.GetRequiredService<UserHomePage>();
                        userHome.user = appUser;
                        NavigationService.Navigate(userHome);
                    }
                    else
                    {
                        lblMessage.Text = "";

                        if (!optRegister)
                            optRegister = await ConfirmRegister("The card is not registered. Do you need to register it?");

                        if (optRegister)
                        {
                            var idCardRegister = serviceProvider.GetRequiredService<IdCardRegisterPage>();
                            idCardRegister.IdCardNumber = cardId;
                            NavigationService.Navigate(idCardRegister);
                        }
                        else
                        {
                            txtIdCardNumber.KeyUp += txtIdCardNumber_KeyUp;
                        }
                    }
                }

                txtIdCardNumber.Text = "";
                txtIdCardNumber.Focus();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void btnRegister_Click(object sender, RoutedEventArgs e)
    {
        optRegister = true;
        DisplayMsg("Please tape your Id Card");
    }



    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        txtIdCardNumber.KeyUp -= txtIdCardNumber_KeyUp;
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

    private async Task<bool> ConfirmRegister(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgIdCardLoginHost"))
                DialogHost.Close("pgIdCardLoginHost");

            var sampleMessageDialog = new ConfirmDialog { Message = { Text = msg } };
            var result = await DialogHost.Show(sampleMessageDialog, "pgIdCardLoginHost");
            return Convert.ToBoolean(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgIdCardLoginHost"))
                DialogHost.Close("pgIdCardLoginHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgIdCardLoginHost", null, null, (object sender, DialogClosedEventArgs e) =>
            {

                var ss = txtIdCardNumber.Focus();
                // logger.LogInformation(ss.ToString());
            });


        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    private async Task<AppUserDto?> GetCardInfo(string idCardNumber)
    {
        var response = await httpClient.GetAsync($"api/appuser/idcard/{idCardNumber}");

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<AppUserDto>();

        string respText = await response.Content.ReadAsStringAsync();
        logger.LogInformation($"{respText}");
        return null;
    }

    private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!txtIdCardNumber.IsKeyboardFocused)
            txtIdCardNumber.Focus();
    }

}
