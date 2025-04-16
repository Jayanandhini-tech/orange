using CMS.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;


public partial class OperatorHomePage : Page
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<OperatorHomePage> logger;

    public OperatorHomePage(IServiceScopeFactory serviceScopeFactory, ILogger<OperatorHomePage> logger)
    {
        InitializeComponent();
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        btnRefill.Visibility = Visibility.Collapsed;
        btnRefill2.Visibility = Visibility.Collapsed;
        btnSprialSettings.Visibility = Visibility.Collapsed;
        btnSprialSettings2.Visibility = Visibility.Collapsed;
        btnMotorTesting.Visibility = Visibility.Collapsed;

        if (DataStore.AppType == StrDir.AppType.VM)
        {
            btnRefill.Visibility = Visibility.Visible;
            //btnSprialSettings.Visibility = Visibility.Visible;
            btnMotorTesting.Visibility = Visibility.Visible;
            if (DataStore.CabinCount == 2)
            {
                btnRefill2.Visibility = Visibility.Visible;
                //btnSprialSettings2.Visibility = Visibility.Visible;
            }
        }
    }

    private void btnRefill_Click(object sender, RoutedEventArgs e)
    {
        int.TryParse(((Button)sender).Tag.ToString(), out int cabinId);
        cabinId = cabinId > 0 ? cabinId : 1;

        var scope = serviceScopeFactory.CreateScope();
        var page = scope.ServiceProvider.GetRequiredService<RefillPage>();
        page.CabinId = cabinId;
        NavigationService.Navigate(page);
    }

    private void btnSprialSettings_Click(object sender, RoutedEventArgs e)
    {
        int.TryParse(((Button)sender).Tag.ToString(), out int cabinId);
        cabinId = cabinId > 0 ? cabinId : 1;

        var scope = serviceScopeFactory.CreateScope();
        var page = scope.ServiceProvider.GetRequiredService<SpiralSettingPage>();
        page.cabinId = cabinId;
        NavigationService.Navigate(page);
    }

    private void btnReport_Click(object sender, RoutedEventArgs e)
    {
        var scope = serviceScopeFactory.CreateScope();
        NavigationService.Navigate(scope.ServiceProvider.GetRequiredService<ReportsPage>());
    }

    private void btnMotorTesting_Click(object sender, RoutedEventArgs e)
    {
        var scope = serviceScopeFactory.CreateScope();
        NavigationService.Navigate(scope.ServiceProvider.GetRequiredService<MotorTestingPage>());
    }

    private void btnShutdown_Click(object sender, RoutedEventArgs e)
    {
        logger.LogInformation("Application Shutdown by User");
        Process.Start("shutdown", "/s /t 2");
        Application.Current.Shutdown();
    }

    private void btnRestart_Click(object sender, RoutedEventArgs e)
    {
        logger.LogInformation("Application Restarted by User");
        Process.Start("shutdown", "/r /t 2");
        Application.Current.Shutdown();
    }

    private void btnExit_Click(object sender, RoutedEventArgs e)
    {
        logger.LogInformation("Application Closed by User");
        Process p = Process.Start("Explorer.exe");
        Application.Current.Shutdown();
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        var scope = serviceScopeFactory.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        eventService.RaiseOrderCompleteEvent(string.Empty);

        NavigationService.GoBack();
    }


}
