using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using VM.Domains;
using VM.Dtos;

namespace VM.Pages;


public partial class SpiralSettingPage : Page
{
    private readonly AppDbContext dbContext;
    private readonly ILogger<SpiralSettingPage> logger;

    public int cabinId = 1;
    List<SpiralSettingDto> spiralSettings = [];

    public SpiralSettingPage(AppDbContext dbContext, ILogger<SpiralSettingPage> logger)
    {
        InitializeComponent();
        this.dbContext = dbContext;
        this.logger = logger;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            spiralSettings = await dbContext.MotorSettings
                               .Where(x => x.CabinId == cabinId)
                               .Select(x =>
                               new SpiralSettingDto()
                               {
                                   MotorNumber = x.MotorNumber,
                                   IsActive = x.IsActive
                               }).ToListAsync();

            lstMotors.ItemsSource = spiralSettings;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void ToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        var btn = (ToggleButton)sender;

        var setting = await dbContext.MotorSettings.FirstOrDefaultAsync(x => x.CabinId == cabinId && x.MotorNumber == (int)btn.Tag);

        if (setting is not null)
        {
            setting.IsActive = true;
            setting.ProductId = string.Empty;
            setting.Capacity = 0;
            setting.Stock = 0;
            setting.SoldOut = false;
            setting.IsViewed = false;
            setting.UpdatedOn = DateTime.Now;
            await dbContext.SaveChangesAsync();
        }

    }

    private async void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        var btn = (ToggleButton)sender;

        var setting = await dbContext.MotorSettings.FirstOrDefaultAsync(x => x.CabinId == cabinId && x.MotorNumber == (int)btn.Tag);

        if (setting is not null)
        {

            if (setting.Stock > 0)
            {
                var product = await dbContext.Products.FindAsync(setting.ProductId);

                await dbContext.StockCleared.AddAsync(
                 new StockCleared()
                 {
                     Id = $"sc_{Guid.NewGuid().ToString("N").ToUpper()}",
                     ClearedOn = DateTime.Now,
                     MotorNumber = setting.MotorNumber,
                     ProductId = setting.ProductId,
                     ProductName = product?.Name ?? string.Empty,
                     Quantity = setting.Stock,
                     IsViewed = false
                 });
            }

            setting.IsActive = false;
            setting.ProductId = string.Empty;
            setting.Capacity = 0;
            setting.Stock = 0;
            setting.SoldOut = false;
            setting.IsViewed = false;
            setting.UpdatedOn = DateTime.Now;
            await dbContext.SaveChangesAsync();
        }
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.GoBack();
    }


}
