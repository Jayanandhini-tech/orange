using CMS.Dto;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Http.Json;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VM.Components;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;

public partial class OrderPage : Page
{
    private readonly AppDbContext dbContext;
    private readonly IServerClient httpClient;
    private readonly IServiceProvider serviceProvider;
    private readonly ISensorService sensorService;
    private readonly IModbus modbus;
    private readonly IHostEnvironment environment;
    private readonly SpeechSynthesizer speech;
    private readonly ILogger<OrderPage> logger;



    List<CategoryProductDto> categoryProducts = [];
    int qty = 0;
    double amount = 0;

    bool hardwareReady = false;

    public OrderPage(AppDbContext dbContext, IServerClient httpClient,
        IServiceProvider serviceProvider, ISensorService sensorService,
        IModbus modbus, IHostEnvironment environment, SpeechSynthesizer speech,
        ILogger<OrderPage> logger)
    {
        InitializeComponent();
        this.dbContext = dbContext;
        this.httpClient = httpClient;
        this.serviceProvider = serviceProvider;
        this.sensorService = sensorService;
        this.modbus = modbus;
        this.environment = environment;
        this.speech = speech;
        this.logger = logger;

    }


    private async void DialogHost_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (categoryProducts.Count == 0)
            {
                categoryProducts = DataStore.AppType == StrDir.AppType.VM ? await GetVMProducts() : await GetKioskProducts();
                var categories = categoryProducts.Select((x, index) => new { index, x.CategoryId, x.CategoryName, x.ImgPath }).ToList();

                lstCategory.ItemsSource = categories;
                lstProducts.ItemsSource = categoryProducts;
            }

            UpdateCart();

            if (DataStore.AppType == StrDir.AppType.VM)
            {
                hardwareReady = false;
                bool isOpen = modbus.Open();
                if (isOpen)
                {
                    _ = Task.Run(() => CheckHardware());
                }
            }
            else
            {
                hardwareReady = true;
            }

        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    public async Task<List<CategoryProductDto>> GetVMProducts()
    {
        try
        {
            var productStock = await dbContext.MotorSettings
                                                  .Where(x => x.IsActive == true && x.SoldOut == false && x.Stock > 0)
                                                  .GroupBy(x => x.ProductId)
                                                  .Select(g => new
                                                  {
                                                      ProductId = g.Key,
                                                      Stock = g.Sum(x => x.Stock)
                                                  }).ToListAsync();


            var productIds = productStock.Select(x => x.ProductId).ToList();
            var products = await dbContext.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();

            var cateIds = products.Select(x => x.CategoryId).Distinct().ToList();
            var dbCates = await dbContext.Categories.Where(x => cateIds.Contains(x.Id)).ToListAsync();

            var result = dbCates.Select(x =>
                                    new CategoryProductDto()
                                    {
                                        CategoryId = x.Id,
                                        CategoryName = x.Name,
                                        ImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, x.ImgPath),
                                        DisplayOrder = x.DisplayOrder,
                                        Products = products.Where(p => p.CategoryId == x.Id)
                                                    .Select(p => new DisplayProductDto()
                                                    {
                                                        Id = p.Id,
                                                        Name = p.Name,
                                                        ImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p.ImgPath),
                                                        Price = p.Price,
                                                        Rate = p.BaseRate,
                                                        Gst = p.Gst,
                                                        qty = 0,
                                                        Stock = productStock.Where(s => s.ProductId == p.Id).Sum(s => s.Stock)
                                                    }).ToList(),
                                    }).ToList();


            result = result.Where(x => x.Products.Count > 0).ToList();
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    public async Task<List<CategoryProductDto>> GetKioskProducts()
    {

        try
        {
            var response = await httpClient.GetAsync("/api/kiosk/productwithstock");

            if (!response.IsSuccessStatusCode)
                return [];


            var categoryProds = await response.Content.ReadFromJsonAsync<List<CategoryProductDto>>();

            if (categoryProds is null)
                return [];

            // Cache Images
            foreach (var category in categoryProds)
            {
                await httpClient.DownloadImage(category.ImgPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category.ImgPath));
                category.ImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, category.ImgPath);
                foreach (var product in category.Products)
                {
                    await httpClient.DownloadImage(product.ImgPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, product.ImgPath));
                    product.ImgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, product.ImgPath);
                }
            }


            return categoryProds;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return [];
        }
    }

    private void btnCategory_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var parent = btn!.Parent as Grid;
        parent!.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = Mouse.MouseDownEvent,
            Source = this,
        });

    }

    private void GrdCategory_MouseDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            int index = Convert.ToInt32(((Grid)sender).Tag);
            var categoryLable = lstProducts.ItemContainerGenerator.ContainerFromItem(lstProducts.Items[index]) as FrameworkElement;
            if (categoryLable != null)
            {
                Vector vector = VisualTreeHelper.GetOffset(categoryLable);
                svItems.ScrollToVerticalOffset(vector.Y);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void ProductImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        string productId = ((FrameworkElement)sender).Tag.ToString() ?? "";
        AddItem(productId, sender);
    }
    private void Add_MouseDown(object sender, MouseButtonEventArgs e)
    {
        string productId = ((FrameworkElement)sender).Tag.ToString() ?? "";
        AddItem(productId, sender);
    }

    private void BtnPlus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string productId = ((FrameworkElement)sender).Tag.ToString() ?? "";
            AddItem(productId);
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
            var product = categoryProducts.FirstOrDefault(x => x.Products.Any(p => p.Id == productId))!.Products.FirstOrDefault(P => P.Id == productId);
            if (product!.qty > 0)
                product!.qty--;

            speech.SpeakAsync($"{product.Name} {product.qty} quantity");
            UpdateCart();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private void AddItem(string productId, object? sender = null)
    {
        var product = categoryProducts.SelectMany(x => x.Products).Where(x => x.Id == productId).FirstOrDefault();

        if (product == null)
            return;

        if (product.qty == 0)
            AddControl(sender, product);


        if (product.Stock > product.qty)
        {
            product.qty++;
            speech.SpeakAsync($"{product.Name} {product.qty} quantity");
        }
        else
        {
            speech.SpeakAsync($"Only {product.Stock} quantity left");
            DisplayMsg($"Only {product.Stock} number(s) left");
        }
        UpdateCart();
    }

    private void AddControl(object? sender, Dtos.DisplayProductDto product)
    {
        if (sender is null)
            return;

        var border = (Border)sender;

        StackPanel parent;

        var rootParent = (StackPanel)border.Parent;

        if (string.IsNullOrEmpty(rootParent.Name))
            parent = (StackPanel)rootParent.FindName("spBtnPlaceHolder");
        else
            parent = rootParent;


        Brush myGreen = (SolidColorBrush)new BrushConverter().ConvertFrom("#3BA600")!;

        Border outer = new Border() { Width = 120, Height = 30 };
        outer.CornerRadius = new CornerRadius(5);
        outer.Background = myGreen;
        outer.BorderBrush = myGreen;

        var stackPanel = new StackPanel() { Height = 28, Orientation = Orientation.Horizontal };

        var btnAdd = new Button()
        {
            Tag = product.Id,
            Width = 40,
            Height = 28,
            VerticalAlignment = VerticalAlignment.Center,
            Background = myGreen,
            BorderBrush = myGreen,
            Foreground = Brushes.White,
            Padding = new Thickness(0)
        };

        btnAdd.Content = new PackIcon() { Kind = PackIconKind.Plus, Height = 25, Width = 25 };
        btnAdd.Click += BtnPlus_Click;

        var btnMinus = new Button()
        {
            Tag = product.Id,
            Width = 40,
            Height = 28,
            VerticalAlignment = VerticalAlignment.Center,
            Background = myGreen,
            BorderBrush = myGreen,
            Foreground = Brushes.White,
            Padding = new Thickness(0)
        };
        btnMinus.Content = new PackIcon() { Kind = PackIconKind.Minus, Height = 25, Width = 25 };
        btnMinus.Click += BtnMinus_Click;

        var textBlock = new TextBlock()
        {
            Width = 40,
            Height = 27,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brushes.White,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        };


        Binding myBinding = new Binding(nameof(product.qty));
        myBinding.Source = product;
        textBlock.SetBinding(TextBlock.TextProperty, myBinding);

        var plusIcon = new PackIcon() { Kind = PackIconKind.Plus, Width = 40, Height = 25, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White };

        stackPanel.Children.Add(btnMinus);
        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(btnAdd);
        outer.Child = stackPanel;

        parent.Children.Add(outer);
    }

    private void btnNext_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataStore.AppType == StrDir.AppType.VM && environment.IsProduction() && hardwareReady == false)
            {
                DisplayMsg("Machine out of Order. Sorry for the inconvenience");
                speech.SpeakAsync($"Machine out of order");
                return;
            }

            if (qty < 1)
            {
                speech.SpeakAsync($"No item in the cart, Please add one or more items into cart");
                DisplayMsg("No item in the cart, Please add one or more items into cart");
                return;
            }

            DataStore.selectedProducts = categoryProducts.SelectMany(x => x.Products).Where(x => x.IsSelected).ToList();
            //NavigationService.Navigate(serviceProvider.GetRequiredService<CartPage>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Navigate(serviceProvider.GetRequiredService<HomePage>());
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        modbus.Close();
    }

    private async Task CheckHardware()
    {
        try
        {
            sensorService.CalibrateAllSensorPermanantly();
            await Task.Delay(250);
            bool di0 = modbus.GetInstantValue(0);
            bool di1 = modbus.GetInstantValue(1);
            int status = modbus.Status();
            hardwareReady = status > 0 && di0 == false && di1 == false;
            logger.LogInformation($"Hardware Status DI0 : {di0}, DI1 :{di1}, PLC status code : {status}. Is Hardware ready : {hardwareReady}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void UpdateCart()
    {
        var selectedProduct = categoryProducts.SelectMany(x => x.Products).Where(x => x.IsSelected).ToList();
        qty = selectedProduct.Sum(x => x.qty);
        amount = selectedProduct.Sum(x => x.amount);

        lblTotal.Text = $"Total : ₹{amount}";
    }

    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgOrderHost"))
                DialogHost.Close("pgOrderHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgOrderHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    public async void ShowProgressbar()
    {
        CloseDialog();
        await DialogHost.Show(new ProcessingDialog(), "pgOrderHost");
    }

    public void CloseDialog()
    {
        if (DialogHost.IsDialogOpen("pgOrderHost"))
            DialogHost.Close("pgOrderHost");

    }


}
