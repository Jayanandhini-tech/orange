using CMS.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;

public partial class FaceLoginPage : Page
{
    private readonly IServiceProvider serviceProvider;
    private readonly IFaceDeviceService faceDeviceService;
    private readonly IUserService userService;
    private readonly IEventService eventService;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<FaceLoginPage> logger;

    DispatcherTimer tmr = new DispatcherTimer();

    int tmrCount = 60;

    public FaceLoginPage(IServiceProvider serviceProvider, IFaceDeviceService faceDeviceService, IUserService userService,
        IEventService eventService, SpeechSynthesizer speech, ILogger<FaceLoginPage> logger)
    {
        InitializeComponent();
        this.serviceProvider = serviceProvider;
        this.faceDeviceService = faceDeviceService;
        this.userService = userService;
        this.eventService = eventService;
        this.speech = speech;
        this.logger = logger;

        tmr.Interval = TimeSpan.FromSeconds(1);
        tmr.Tick += Tmr_Tick;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            tmrCount = 60;
            spSeconds.Visibility = Visibility.Hidden;
            lblNotification.Text = "Please wait";
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
            bool deviceReady = await faceDeviceService.ConnectDevice();
            if (deviceReady)
            {
                lblNotification.Text = "Show Your Face";
                spSeconds.Visibility = Visibility.Visible;
                lblSecondsleft.Text = $"{tmrCount} Seconds";
                tmr.Start();
                speech.SpeakAsync("Show your face");
            }
            else
            {
                speech.SpeakAsync("The device is not ready at the movement");
                lblNotification.Text = "The device is not ready at the movement. Sorry for the inconvenience. Please try other payments";
                btnBack.IsEnabled = false;
                await Task.Delay(3000);
                NavigationService.GoBack();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void Tmr_Tick(object? sender, EventArgs e)
    {
        try
        {
            var record = faceDeviceService.GetLastRecord();
            if (record is not null)
            {
                tmr.Stop();
                logger.LogInformation($"Autheticated User Id : {record.Id}, Name : {record.Name}");

                var user = await userService.GetUserAsync(record.Id);
                if (user is null)
                {
                    string imageData = faceDeviceService.GetUserImage(record.Id);
                    string imagePath = "";

                    if (!string.IsNullOrEmpty(imageData))
                        imagePath = StoreUserImage(record.Id, imageData);

                    user = await userService.CreateUserAsync(record.Id, record.Name, imagePath);
                    eventService.RaiseUserCreatedEvent(user.Id);
                }

                var balance = await userService.GetBalanceAsync(user.Id);

                var userHome = serviceProvider.GetRequiredService<UserHomePage>();
                userHome.user = new AppUserDto(record.Id, record.Name, "", balance, user.ImagePath);


                if (string.IsNullOrEmpty(record.Name) || record.Name.ToLower() == "name")
                    speech.SpeakAsync($"Hello {record.Id}");
                else
                    speech.SpeakAsync($"Hello {record.Name}");


                NavigationService.Navigate(userHome);
            }

            tmrCount--;
            if (tmrCount <= 0)
            {
                tmr.Stop();
                NavigationService.Navigate(serviceProvider.GetRequiredService<HomePage>());
            }
            lblSecondsleft.Text = $"{tmrCount} Seconds";
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        tmr.Stop();
        faceDeviceService.DisconnectDevice();
        btnBack.IsEnabled = true;
    }

    private string StoreUserImage(string Id, string base64img)
    {
        try
        {
            string userDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Users");
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);

            string imagePath = $"Images/Users/{Id}.jpg";
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
            byte[] bytes = Convert.FromBase64String(base64img);
            File.WriteAllBytes(filePath, bytes);
            return imagePath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return string.Empty;
        }
    }

}
