using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM.Components;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;

public partial class RefillPage : Page
{
    private readonly AppDbContext dbContext;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<RefillPage> logger;
    private TextBox activeTextBox;
    private IModbus modbus;

    public int CabinId = 1;
    int selectedMotorNumber = 0;

    List<RefillItemDto> settings = new List<RefillItemDto>();

    public RefillPage(AppDbContext dbContext, IServiceScopeFactory serviceScopeFactory,  IModbus modbus, SpeechSynthesizer speech, ILogger<RefillPage> logger)
    {
        InitializeComponent();
        this.dbContext = dbContext;
        this.serviceScopeFactory = serviceScopeFactory;
        this.speech = speech;
        this.logger = logger;
        this.modbus = modbus;
        //txtCurrent = modbusOrangeCount;

    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadRefillItems();
    }

    private void item_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var itemRefill = sender as ItemRefill;
            if (itemRefill is null)
                return;

            var item = settings.FirstOrDefault(x => x.CabinId == CabinId && x.MotorNumber == itemRefill.MotorNumber);

            if (item is null)
                return;

            lblDisplayNo.Text = item.MotorNumber.ToString();
            lblProductName.Text = item.ProductName;
            lblCurrentStock.Text = item.Stock.ToString();
            brImage.Background = new ImageBrush(new BitmapImage(new Uri(item.ImgPath, UriKind.Absolute)));
            lblProductName.Background = item.SoldOut ? Brushes.Red : Brushes.Transparent;

            selectedMotorNumber = item.MotorNumber;
            btnClearStock.IsEnabled = item.MotorNumber != 3;
            btnRefill.IsEnabled=item.MotorNumber != 3;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void btnRefill_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (selectedMotorNumber < 1)
                return;

            int.TryParse(txtTotalStock.Text, out int totalStock);
            if (totalStock < 1) return;

            var refillItem = settings.First(x => x.CabinId == CabinId && x.MotorNumber == selectedMotorNumber);

            var setting = dbContext.MotorSettings.Where(x => x.CabinId == CabinId && x.MotorNumber == selectedMotorNumber).FirstOrDefault();
            if (setting is null) return;

            int refilled = totalStock - setting.Stock;
            if (refilled < 1) return;

            int capacity = setting.Capacity > totalStock ? setting.Capacity : totalStock;

            setting.Stock = totalStock;
            setting.Capacity = capacity;
            setting.SoldOut = false;
            setting.IsViewed = false;
            setting.UpdatedOn = DateTime.Now;

            await dbContext.Refills.AddAsync(
                new Refill()
                {
                    Id = $"sr_{Guid.NewGuid().ToString("N").ToUpper()}",
                    RefilledOn = DateTime.Now,
                    MotorNumber = selectedMotorNumber,
                    ProductId = setting.ProductId,
                    ProductName = refillItem.ProductName,
                    Quantity = refilled,
                    IsViewed = false
                });

            await dbContext.SaveChangesAsync();

            refillItem.SoldOut = false;
            refillItem.Capacity = setting.Capacity;
            refillItem.Stock = setting.Stock;


            PerformStockCalculations();
            speech.SpeakAsync($"{refillItem.ProductName} is refilled successfully");

            ResetRefillCard();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
    //private async void btnRefill_Click(object sender, RoutedEventArgs e)
    //{
    //    try
    //    {
    //        if (selectedMotorNumber < 1)
    //            return;

    //        if (!int.TryParse(txtTotalStock.Text, out int totalStock) || totalStock < 1)
    //            return;

    //        // Fetch the refill item and motor setting for the selected motor
    //        var refillItem = settings.First(x => x.CabinId == CabinId && x.MotorNumber == selectedMotorNumber);
    //        if (refillItem == null) return;

    //        var setting = dbContext.MotorSettings
    //            .FirstOrDefault(x => x.CabinId == CabinId && x.MotorNumber == selectedMotorNumber);
    //        if (setting == null) return;

    //        // Calculate the refilled quantity
    //        int refilled = totalStock - setting.Stock;
    //        if (refilled < 1) return;

    //        int capacity = setting.Capacity > totalStock ? setting.Capacity : totalStock;

    //        setting.Capacity = capacity;
    //        setting.Stock = totalStock;
    //        setting.SoldOut = false;
    //        setting.IsViewed = false;
    //        setting.UpdatedOn = DateTime.Now;

    //        // Add a refill record
    //        await dbContext.Refills.AddAsync(new Refill()
    //        {
    //            Id = $"sr_{Guid.NewGuid():N}".ToUpper(),
    //            RefilledOn = DateTime.Now,
    //            MotorNumber = selectedMotorNumber,
    //            ProductId = setting.ProductId,
    //            ProductName = refillItem.ProductName,
    //            Quantity = refilled,
    //            IsViewed = false
    //        });

    //        // Save changes to the database
    //        await dbContext.SaveChangesAsync();

    //        // Update UI
    //        refillItem.SoldOut = false;
    //        refillItem.Capacity = setting.Capacity;
    //        refillItem.Stock = setting.Stock;

    //        // Perform stock calculations only after successful refill
    //        PerformStockCalculations();

    //        // Announce success
    //        speech.SpeakAsync($"{refillItem.ProductName} is refilled successfully");

    //        // Reset refill card
    //        ResetRefillCard();
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex.Message, ex);
    //    }
    //}

    private async void PerformStockCalculations()
    {
        try
        {
            var motor1Setting = dbContext.MotorSettings
                   .FirstOrDefault(x => x.CabinId == CabinId && x.MotorNumber == 1);
            var motor2Setting = dbContext.MotorSettings
                .FirstOrDefault(x => x.CabinId == CabinId && x.MotorNumber == 2);
            var motor3Setting = dbContext.MotorSettings
               .FirstOrDefault(x => x.CabinId == CabinId && x.MotorNumber == 3);

          
            if (motor1Setting == null || motor2Setting == null || motor3Setting == null) return;

            // Adjust Motor 1 stock based on Motor 2's stock (if it exists)
            if (motor2Setting.Stock > 0)
            {
                motor3Setting.Stock = motor1Setting.Stock * motor2Setting.Stock; // Example logic for updating Motor 1

             
                int capacity = Math.Max(motor3Setting.Capacity, motor3Setting.Stock); 
                motor3Setting.Capacity = capacity;
                motor3Setting.UpdatedOn = DateTime.Now;
                motor3Setting.IsViewed = false;

                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Error during stock calculations", ex);
        }
    }
    //private async void PerformStockCalculations()
    //{
    //    try
    //    {
    //        // Retrieve Motor 1 and Motor 2 settings for the current CabinId
    //        var motor1Setting = dbContext.MotorSettings
    //            .FirstOrDefault(x => x.CabinId == CabinId && x.MotorNumber == 1);
    //        var motor2Setting = dbContext.MotorSettings
    //            .FirstOrDefault(x => x.CabinId == CabinId && x.MotorNumber == 2);

    //        // Retrieve Motor 1 setting specifically for CabinId = 3
    //        var motor1SettingForCabin3 = dbContext.MotorSettings
    //            .FirstOrDefault(x => x.CabinId == 3 && x.MotorNumber == 1);

    //        // Log retrieved values
    //        if (motor1Setting == null || motor2Setting == null || motor1SettingForCabin3 == null)
    //        {
    //            logger.LogWarning($"Required MotorSettings not found. CabinId: {CabinId}, Motor1: {motor1Setting?.MotorNumber}, Motor2: {motor2Setting?.MotorNumber}, Motor1 (Cabin 3): {motor1SettingForCabin3?.MotorNumber}");
    //            return;
    //        }

    //        // Perform stock calculation only if Motor 2's stock is greater than 0
    //        if (motor2Setting.Stock > 0)
    //        {
    //            logger.LogInformation($"Motor1 Stock: {motor1Setting.Stock}, Motor2 Stock: {motor2Setting.Stock}");

    //            // Store the calculation result in Motor 1's stock for CabinId = 3
    //            motor1SettingForCabin3.Stock = motor1Setting.Stock * motor2Setting.Stock;

    //            logger.LogInformation($"Calculated Stock for Motor1 (CabinId 3): {motor1SettingForCabin3.Stock}");

    //            // Update the capacity for Motor 1 in CabinId = 3 if needed
    //            int capacity = Math.Max(motor1SettingForCabin3.Capacity, motor1SettingForCabin3.Stock); // Ensure capacity >= stock
    //            motor1SettingForCabin3.Capacity = capacity;

    //            logger.LogInformation($"Updated Capacity for Motor1 (CabinId 3): {motor1SettingForCabin3.Capacity}");

    //            // Save the changes to the database
    //            await dbContext.SaveChangesAsync();
    //            logger.LogInformation("Stock calculation and database update completed successfully.");
    //        }
    //        else
    //        {
    //            logger.LogWarning($"Motor2 Stock is 0 or less. Skipping calculation. Motor2 Stock: {motor2Setting.Stock}");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError("Error during stock calculations", ex);
    //    }
    //}


    private async void btnClearStock_Click(object sender, RoutedEventArgs e)
    {
        
        await ClearStock();
    }

    private async void btnChangeItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (selectedMotorNumber < 1)
                return;

            var scope = serviceScopeFactory.CreateScope();

            var sampleMessageDialog = scope.ServiceProvider.GetRequiredService<ProductDialog>();
            sampleMessageDialog.Height = this.ActualHeight > 1000 ? this.ActualHeight - 100 : this.ActualHeight;
            var result = await DialogHost.Show(sampleMessageDialog, "pgRefillPageHost");
            string selectedProduct = result as string ?? string.Empty;

            if (string.IsNullOrEmpty(selectedProduct)) return;

            int motorNo = selectedMotorNumber;

            await ClearStock();

            var setting = await dbContext.MotorSettings.FirstOrDefaultAsync(x => x.CabinId == CabinId && x.MotorNumber == motorNo);

            if (setting is null) return;

            setting.ProductId = selectedProduct;
            setting.Capacity = 3;
            setting.IsViewed = false;
            setting.UpdatedOn = DateTime.Now;

            await dbContext.SaveChangesAsync();
            await LoadRefillItems();

            speech.SpeakAsync($"Item changed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void btnStockRequired_Click(object sender, RoutedEventArgs e)
    {
        var scope = serviceScopeFactory.CreateScope();
        NavigationService.Navigate(scope.ServiceProvider.GetRequiredService<StockRequiredPage>());
    }

    private async Task ClearStock()
    {
        try
        {
            if (selectedMotorNumber < 1)
                return;

            var refillItem = settings.First(x => x.CabinId == CabinId && x.MotorNumber == selectedMotorNumber);
            if (refillItem.Stock < 1) return;

            var setting = dbContext.MotorSettings.Where(x => x.CabinId == CabinId && x.MotorNumber == selectedMotorNumber).FirstOrDefault();
            if (setting is null) return;

            await dbContext.StockCleared.AddAsync(
               new StockCleared()
               {
                   Id = $"sc_{Guid.NewGuid().ToString("N").ToUpper()}",
                   ClearedOn = DateTime.Now,
                   MotorNumber = selectedMotorNumber,
                   ProductId = setting.ProductId,
                   ProductName = refillItem.ProductName,
                   Quantity = setting.Stock,
                   IsViewed = false
               });

            setting.Stock = 0;
            setting.Capacity = 0;
            setting.SoldOut = false;
            setting.IsViewed = false;
            setting.UpdatedOn = DateTime.Now;

            await dbContext.SaveChangesAsync();

            refillItem.SoldOut = false;
            refillItem.Stock = 0;

            ResetRefillCard();
            speech.SpeakAsync($"Item cleared successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task LoadRefillItems()
    {
        try
        {
            settings = await (from s in dbContext.MotorSettings
                              join p in dbContext.Products on s.ProductId equals p.Id into motorProduct
                              from mp in motorProduct.DefaultIfEmpty()
                              where s.IsActive == true && s.CabinId == CabinId
                              select new RefillItemDto()
                              {
                                  CabinId = CabinId,
                                  MotorNumber = s.MotorNumber,
                                  Id = s.ProductId,
                                  ProductName = mp == null ? string.Empty : mp.Name,
                                  ImgPath = mp == null ?
                                                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images/Products/default.jpg") :
                                                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mp.ImgPath),
                                  Price = mp == null ? 0.0 : mp.Price,
                                  SoldOut = s.SoldOut,
                                  Stock = s.Stock,
                                  Capacity = s.Capacity
                              }).ToListAsync();

            lstRow1.ItemsSource = settings.Where(x => x.MotorNumber >= 1 && x.MotorNumber <= 2).ToList();

            
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _); // Allow only digits
    }


    private void btnEnterOrangeCount_Click(object sender, RoutedEventArgs e)
    {
        int orangeCount = 0;
        if (int.TryParse(modbusOrangeCount.Text, out orangeCount))
        {
            if (orangeCount >= 0 && orangeCount <= ushort.MaxValue)
            {
                ushort count = (ushort)orangeCount;
                MessageBox.Show($"Entered Orange Count: {count}");
                DataStore.OrangeCount = (int)orangeCount;
                modbusOrangeCount.Text = "";
                modbus.OrangeCount(1, count);
            }
            else
            {
                MessageBox.Show("Please enter a number between 0 and 65535 (ushort range).");
            }
        }
        else
        {
            MessageBox.Show("Please enter a valid number.");
        }
    }

    private void ResetRefillCard()
    {
        selectedMotorNumber = 0;
        lblDisplayNo.Text = "";
        lblCurrentStock.Text = "";
        txtTotalStock.Text = "";
        lblProductName.Text = "Select Tray";
        lblProductName.Background = Brushes.Transparent;
        brImage.Background = new ImageBrush(new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images/Products/default.jpg"), UriKind.Absolute)));
    }

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.GoBack();
    }

    #region Touch Input
    TextBox txtCurrent;


    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        activeTextBox = sender as TextBox;
    }
    //private void btnNumber_Click(object sender, RoutedEventArgs e)
    //{
    //    try
    //    {
    //        Button btn = (Button)sender;
    //        if (btn is null)
    //            return;

    //        if (txtTotalStock.Text.Trim().Length < txtTotalStock.MaxLength)
    //        {
    //            var tag = btn.Tag.ToString() ?? "0";
    //            char c = tag[0];

    //            if (char.IsDigit(c))
    //                txtTotalStock.Text = txtTotalStock.Text + c;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex.Message, ex);
    //    }
    //}
    private void btnNumber_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Button btn = sender as Button;
            if (btn == null || activeTextBox == null)
                return;

            if (activeTextBox.Text.Trim().Length < activeTextBox.MaxLength)
            {
                var tag = btn.Tag?.ToString() ?? "0";
                char c = tag[0];

                if (char.IsDigit(c))
                    activeTextBox.Text += c;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (txtCurrent.Text.Trim().Length > 0)
            {
                txtCurrent.Text = txtCurrent.Text.Substring(0, txtCurrent.Text.Length - 1);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public void clear()
    {
        try
        {
            txtCurrent.Text = "";
            txtCurrent.Focus();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            clear();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }



    #endregion

    private void txtTotalStock_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

    private void modbusOrangeCount_TextChanged(object sender, TextChangedEventArgs e)
    {

    }
}
