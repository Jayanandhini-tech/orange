using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CMS.Dto.Payments;
using CMS.Dto.Payments.QR;
using Microsoft.Extensions.DependencyInjection;
using CMS.Dto;
using VM.Services;
using System;
using Microsoft.Extensions.Hosting;

namespace VM.Pages;
public partial class OrangeHomePage : Page
{

   
    DispatcherTimer tmrDateTime = new DispatcherTimer();
    DispatcherTimer tmrUpi = new DispatcherTimer();

  
    private readonly AppDbContext dbContext;
    private readonly IServiceProvider serviceProvider;
    private readonly IServerClient httpClient;
    private readonly ISignalRClientService signalRClient;
    private readonly IModbus modbus;
    private readonly ILogger<OrangeHomePage> logger;
    private readonly IJuiceService juiceService;
    private readonly IHostEnvironment environment;

    List<VendingProductDto> vendingItems = new List<VendingProductDto>();
    string upiNumber = string.Empty;
    
    int OrangePerCup = 5;

    Product product;
    int timeout = 20;

    bool hardwareReady = false;

    public OrangeHomePage(AppDbContext dbContext, IServerClient httpClient, ISignalRClientService signalRClient, IModbus modbus, ILogger<OrangeHomePage> logger, IServiceProvider serviceProvider, IJuiceService juiceService, IHostEnvironment environment)
    {
        InitializeComponent();
        tmrDateTime.Interval = TimeSpan.FromSeconds(1);
        tmrDateTime.Tick += TmrDateTime_Tick;

        tmrUpi.Interval = TimeSpan.FromSeconds(1);
        tmrUpi.Tick += TmrUpi_Tick;

        this.dbContext = dbContext;
        this.httpClient = httpClient;
        this.serviceProvider = serviceProvider;
        this.signalRClient = signalRClient;
        this.modbus = modbus;
        this.logger = logger;
        this.juiceService = juiceService;


        product = dbContext.Products
        .Where(x => EF.Functions.Like(x.Name, "%juice%"))
        .OrderByDescending(x => x.UpdatedOn)
        .FirstOrDefault();

    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        signalRClient.OnPaymentStatusReceived += OnPaymentStatusReceived;
        tmrDateTime.Start();
        spUpi.Visibility = Visibility.Hidden;
        processCard.Visibility = Visibility.Hidden;
        imgUPI.Visibility = Visibility.Hidden;
        imgOrnageProcess.Visibility = Visibility.Hidden;

        lblPrice.Text = $"₹ {20}";

        PlayNextVideo();

        modbus.Open();

        if (!modbus.IsOpen())
        {
            logger.LogInformation("Port is not opened");
            return;
        }


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
    private async Task CheckHardware()
    {
        try
        {
            
            int status = modbus.Status();
            hardwareReady = status > 0;
            logger.LogInformation($"PLC status code : {status}. Is Hardware ready : {hardwareReady}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void btnOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            
            var motor3Stock = await dbContext.MotorSettings
                                             .Where(ms => ms.MotorNumber == 3)
                                             .Select(ms => ms.Stock)
                                             .FirstOrDefaultAsync();

            var motor4Stock = await dbContext.MotorSettings
                                             .Where(ms => ms.MotorNumber == 4)
                                             .Select(ms => ms.Stock)
                                             .FirstOrDefaultAsync();



            if (motor3Stock < OrangePerCup && motor4Stock < 1 && environment.IsProduction() && hardwareReady == false)
            {
                await MachineFailDialog();
                return;
            }



            lblQrMessage.Text = "Please wait, Generating QR code";
            lblSecondsRemains.Text = "";
            lblSecondsPart2.Text = "";
            btnCancel.Visibility = Visibility.Visible;

            imgUPI.Visibility = Visibility.Hidden;
            imgOrnageProcess.Visibility = Visibility.Hidden;
            processCard.Visibility = Visibility.Visible;


            MoveOrderCenterToLeft();
           
            upiNumber = await juiceService.GetNextOrderNumberforUPI();

            QrImageDto qr = await GetQr(upiNumber, product.Price);

            
            if (qr.IsSuccess)
            {
                logger.LogInformation("QR creation is successful");

               
                processCard.Visibility = Visibility.Hidden;
                imgUPI.Visibility = Visibility.Visible;

             
                byte[] binaryData = Convert.FromBase64String(qr.QRbase64png);

               
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();
                imgUPI.Source = bi;

               
                lblQrMessage.Text = "Scan QR code to make the payment";
                lblSecondsRemains.Text = "25 Seconds";
                lblSecondsPart2.Text = "Remaining to complete the payment";
                lblSecondsRemains.TextAlignment = TextAlignment.Center;

                timeout = 25;
                tmrUpi.Start();
                //signalRClient.OnPaymentStatusReceived += OnPaymentStatusReceived;
            }
            else
            {
               
                lblQrMessage.Text = qr.Message;
                await Task.Delay(3000);
                CancelOrder();
            }
        }
        catch (Exception ex)
        {
           
            logger.LogError(ex.Message, ex);
            btnOrder.IsEnabled = true;
        }
    }

    private async Task MachineFailDialog()
    {
        try
        {

            pgJuicePageHost.IsOpen = true;

            var tcs = new TaskCompletionSource<bool>();

            RoutedEventHandler handler = null;
            handler = (sender, args) =>
            {
                tcs.SetResult(true);
                pgJuicePageHost.IsOpen = false; 
                btnOk.Click -= handler; 
            };

            btnOk.Click += handler;

           
            await tcs.Task;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error in MsgDialog: {ex.Message}");
        }
    }

    private void OnDialogOkClick(object sender, RoutedEventArgs e)
    {

        pgJuicePageHost.IsOpen = false;
    }
    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        CancelOrder();
    }

    private void TmrUpi_Tick(object? sender, EventArgs e)
    {
        timeout--;

        if (timeout <= 0)
        {
            tmrUpi.Stop();
            lblSecondsRemains.Text = "Time Expired";

            CheckPaymentStatusAsync(DataStore.orderNumber);
            CancelOrder();
         
        }
        else
        {
            lblSecondsRemains.Text = $"{timeout} Seconds";
        }
    }

    private async Task<QrImageDto> GetQr(string orderNumber, double amount)
    {
        try
        {
            QrCreateDto qrCreate = new QrCreateDto(orderNumber, amount);
            var response = await httpClient.PostAsync($"api/payments/upi/create", qrCreate);
            string respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation(respText);

            if (response.StatusCode == HttpStatusCode.OK)
                return JsonConvert.DeserializeObject<QrImageDto>(respText)!;

            return new QrImageDto(orderNumber, false, Message: "Unable to generate QR code");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return new QrImageDto(orderNumber, false, Message: "Unable to generate QR code");
        }
    }

    private async Task CheckPaymentStatusAsync(string orderNumber)
    {
        QrStatusDto qrStatusDto = new QrStatusDto(orderNumber, PaymentStatusCode.FAILED, "");

        try
        {
            var response = await httpClient.GetAsync($"api/payments/upi/status/gateway?orderNumber={DataStore.orderNumber}");
            string respText = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
                logger.LogInformation(respText);

            if (response.StatusCode == HttpStatusCode.OK)
                qrStatusDto = JsonConvert.DeserializeObject<QrStatusDto>(respText)!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }

        ProcessPaymentStatus(qrStatusDto);
    }

    private void OnPaymentStatusReceived(QrStatusDto status)
    {
        logger.LogInformation(JsonConvert.SerializeObject(status));

        if (status is not null)
        {
            if (status.OrderNumber == upiNumber)
            {
                tmrUpi.Stop();
                Dispatcher.Invoke((Action)(() =>
                {
                    ProcessPaymentStatus(status);
                }));
            }
        }
    }

    private async void ProcessPaymentStatus(QrStatusDto qrStatus)
    {
        try
        {
            if (qrStatus.OrderNumber == upiNumber)
            {
                switch (qrStatus.Status)
                {
                    case PaymentStatusCode.PENDING:
                       
                        break;
                    case PaymentStatusCode.SUCCESS:
                       
                        DataStore.PaymentDto = new OrderPaymentDto()
                        {
                            PaymentType = StrDir.PaymentType.UPI,
                            Amount = product.Price,
                            IsPaid = true,
                            Reference = upiNumber,
                            PaymentId = qrStatus.PaymentId
                        };
                        logger.LogInformation($"Payment successful: IsPaid={DataStore.PaymentDto.IsPaid}");
                        await CreateOrderAsync();
                        await StartProcessing();
                        break;
                    case PaymentStatusCode.FAILED:
                    default:
                        
                        lblQrMessage.Text = "Transaction failed";
                        btnCancel.IsEnabled = false;
                        Task.Delay(3000).ContinueWith(t => CancelOrder()); 
                        break;
                }

                if (timeout <= 0 && qrStatus.Status == PaymentStatusCode.PENDING)
                    CancelOrder();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
    public async Task CreateOrderAsync()
    {
        try
        {
            // Fetch the product related to "juice" and order them by the most recently updated
            var product = dbContext.Products
                .Where(x => EF.Functions.Like(x.Name, "%juice%"))
                .OrderByDescending(x => x.UpdatedOn)
                .FirstOrDefault();

            if (product == null)
            {
                logger.LogWarning("No products found with 'juice' in the name.");
                return;  // Exit early if no product is found
            }

            // Get sorted product IDs based on motor settings with available stock
            var productIdSortedByMotorNumber = dbContext.MotorSettings
                .Where(x => x.ProductId == product.Id &&
                            x.Stock > 0 &&
                            x.IsActive == true &&
                            !x.SoldOut)
                .OrderBy(x => x.CabinId)
                .ThenBy(x => x.MotorNumber)
                .Select(x => x.ProductId)
                .ToList();

            if (productIdSortedByMotorNumber.Count == 0)
            {
                logger.LogWarning("No available motor settings for the product.");
                return;
            }

            // Create the list of vending items
            vendingItems = new List<VendingProductDto>
        {
            new VendingProductDto()
            {
                Id = product.Id,
                Name = product.Name,
                ImgPath = product.ImgPath,
                Price = product.Price,
                Qty = 1,
                Vend = 0,
                Status = "",
                VendQtyStatus = Enumerable.Range(1, 1).Select(i => new VendQtyStatusDto()).ToList()  // Single item for quantity 1
            }
        };

            logger.LogInformation($"Got vending items: {vendingItems.Count}");
            foreach (var item in vendingItems)
            {
                logger.LogInformation($"Item: {item.Name}, Qty: {item.Qty}");
            }

            logger.LogInformation("Juice service starts...");

            if (string.IsNullOrEmpty(DataStore.orderNumber))
            {
                // Create the order and store the order number
                string orderNumber = await juiceService.CreateOrderAsync(
                    DataStore.selectedProducts,
                    DataStore.PaymentDto,
                    DataStore.deliveryType);
                DataStore.orderNumber = orderNumber;
            }

            logger.LogInformation("Juice service completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating the order.");
        }
    }

    public async Task StartProcessing()
    {
        try
        {
            // Check if the payment is received
            if (DataStore.PaymentDto.IsPaid)
            {
                logger.LogInformation("Payment received, starting vending process...");
                await StartVending();
                await CheckRefund();
            }
            else
            {
                lblQrMessage.Text = "Sorry, order payment not received.";
                logger.LogInformation("Order payment not received.");
                await Task.Delay(5000);  // Wait for 5 seconds
            }

            // Update the order status
            await UpdateOrder();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while processing the order.");
        }
        finally
        {
            logger.LogInformation("Vending process ended.");
            logger.LogInformation("Navigating to bill page...");
            NavigationService?.Navigate(serviceProvider.GetRequiredService<PrintBillPage>());
        }
    }

    //private async Task StartVending()
    //{
    //    logger.LogInformation("Starting vending process...");
    //    lblQrMessage.Text = "Please wait, preparing your juice...";
    //    lblSecondsRemains.Text = string.Empty;
    //    lblSecondsPart2.Text = string.Empty;
    //    lblSecondsRemains.TextAlignment = TextAlignment.Left;

    //    btnCancel.Visibility = Visibility.Hidden;
    //    processCard.Visibility = Visibility.Hidden;
    //    imgUPI.Visibility = Visibility.Hidden;
    //    imgOrnageProcess.Visibility = Visibility.Visible;

    //    bool isVendSuccess = false;

    //    if (vendingItems == null || vendingItems.Count == 0)
    //    {
    //        logger.LogWarning("No items to vend.");
    //        return;  // Exit early if there are no items.
    //    }

    //    foreach (var vendingItem in vendingItems)
    //    {
    //        logger.LogInformation($"Processing item: {vendingItem.Name}, Qty: {vendingItem.Qty}");

    //        if (vendingItem.Qty <= 0)
    //        {
    //            logger.LogWarning($"Invalid quantity for product {vendingItem.Name}. Skipping item.");
    //            continue;  // Skip this item if the quantity is invalid.
    //        }

    //        for (int i = 0; i < vendingItem.Qty; i++)
    //        {
    //            try
    //            {
    //                // Find the motor setting for the vending item
    //                var setting = await dbContext.MotorSettings
    //                    .Where(x => x.ProductId == vendingItem.Id && x.Stock > 0 && x.IsActive && !x.SoldOut)
    //                    .OrderBy(x => x.CabinId)
    //                    .ThenBy(x => x.MotorNumber)
    //                    .FirstOrDefaultAsync();

    //                if (setting == null)
    //                {
    //                    logger.LogInformation($"Out of stock for product {vendingItem.Name}");
    //                    continue;  // Skip this item and continue with the next
    //                }

    //                logger.LogInformation($"Vending for product {vendingItem.Name}, Qty {i + 1}");

    //                isVendSuccess = true;
    //                var index = vendingItems.IndexOf(vendingItem);
    //                vendingItem.VendQtyStatus[index].ProcessValue = 0;
    //                vendingItem.Vend++;

    //                // Add your vending logic (e.g., interaction with hardware)
    //                await ReduceStockAsync();

    //            }
    //            catch (Exception ex)
    //            {
    //                logger.LogError(ex, $"Error occurred while vending item {vendingItem.Name}.");
    //            }
    //        }
    //    }

    //    if (isVendSuccess)
    //    {
    //        var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

    //        if (order != null)
    //        {
    //            foreach (var item in order.Items!)
    //            {
    //                var vendItem = vendingItems.FirstOrDefault(x => x.Id == item.ProductId);
    //                if (vendItem != null)
    //                {
    //                    item.VendQty = vendItem.Vend;
    //                    item.UpdatedOn = DateTime.Now;
    //                    item.IsViewed = false;
    //                }
    //            }

    //            await dbContext.SaveChangesAsync();
    //        }

    //        logger.LogInformation("Vending process completed.");
    //    }
    //}


    //private async Task StartVending()
    //{
    //    logger.LogInformation("Starting vending process...");
    //    lblQrMessage.Text = "Please wait, preparing your juice...";
    //    lblSecondsRemains.Text = "";
    //    lblSecondsPart2.Text = "";
    //    lblSecondsRemains.TextAlignment = TextAlignment.Left;

    //    btnCancel.Visibility = Visibility.Hidden;
    //    processCard.Visibility = Visibility.Hidden;
    //    imgUPI.Visibility = Visibility.Hidden;
    //    imgOrnageProcess.Visibility = Visibility.Visible;

    //    bool IsVendSuccess = false;
    //    try
    //    {
    //        if (vendingItems == null || vendingItems.Count == 0)
    //        {
    //            logger.LogWarning("No items to vend.");
    //            return;
    //        }

    //        modbus.Open();

    //        if (!modbus.IsOpen())
    //        {
    //            logger.LogInformation("Vending Start Failed due to port closed");
    //            return;
    //        }

    //        if (modbus.Open())
    //        {
    //            logger.LogInformation("Port is open and ahead to vending");
    //        }

    //        foreach (var vendingItem in vendingItems)
    //        {
    //            for (int i = 0; i < vendingItem.Qty; i++)
    //            {
    //                try
    //                {
    //                    // Find the motor setting for the vending item
    //                    var setting = await dbContext.MotorSettings
    //                        .Where(x => x.ProductId == vendingItem.Id && x.Stock > 0 && x.IsActive && !x.SoldOut)
    //                        .OrderBy(x => x.CabinId)
    //                        .ThenBy(x => x.MotorNumber)
    //                        .FirstOrDefaultAsync();

    //                    if (setting == null)
    //                    {
    //                        logger.LogInformation($"Out of stock for product {vendingItem.Name}");
    //                        continue;
    //                    }


    //                    vendingItem.VendQtyStatus[i].Processing = true;
    //                    vendingItem.VendQtyStatus[i].ProcessValue = 4;
    //                    vendingItem.Status = "Starting.";

    //                    logger.LogInformation($"Start Vending for product {vendingItem.Name}, Qty {i + 1}");

    //                    int cupStation = 1;

    //                    bool write = modbus.RunMotor(cupStation);
    //                    logger.LogInformation($"Motor write success: {write}");

    //                    if (!write)
    //                    {
    //                        logger.LogInformation("Modbus write failed");
    //                        vendingItem.VendQtyStatus[i].Processing = false;
    //                        vendingItem.VendQtyStatus[i].ProcessValue = 0;
    //                        vendingItem.Status = "";
    //                        continue;
    //                    }

    //                    bool IsContinue = true;
    //                    int wait_count = 0;
    //                    int processStatus = 0;

    //                    while (IsContinue)
    //                    {
    //                        await Task.Delay(1000);
    //                        wait_count++;
    //                        processStatus = modbus.Status();
    //                        logger.LogInformation($"Process status: {processStatus}");

    //                        // Switch-case for process status
    //                        switch (processStatus)
    //                        {
    //                            case 0:
    //                            case 1:
    //                                lblSecondsRemains.Text = $"Starting{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 2:
    //                                logger.LogInformation("Cup one dispensed");
    //                                lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 3:
    //                                logger.LogInformation("Cup two dispensed");
    //                                lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 4:
    //                                logger.LogInformation("Cup delivery point reached");
    //                                lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 5:
    //                                logger.LogInformation("Fruit loaded");
    //                                lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 6:
    //                                IsVendSuccess = true;
    //                                logger.LogInformation("Juicer motor started");
    //                                lblSecondsRemains.Text = $"Crushing Your Juice, Please wait{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 7:
    //                                IsVendSuccess = true;
    //                                await Task.Delay(5000);
    //                                logger.LogInformation("Juice Level reached");
    //                                lblSecondsRemains.Text = $"Open the door and pickup your juice{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 8:
    //                                IsVendSuccess = true;
    //                                IsContinue = false;
    //                                logger.LogInformation("Cup taken");
    //                                lblSecondsRemains.Text = $"Enjoy your Juice{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;

    //                            case 21:
    //                            case 22:
    //                            case 23:
    //                                logger.LogInformation("Home position error");
    //                                break;
    //                            case 24:
    //                                IsContinue = false;
    //                                logger.LogInformation("Cup1 empty");
    //                                modbus.ResetJuicer();
    //                                break;
    //                            case 25:
    //                                IsContinue = false;
    //                                logger.LogInformation("Cup2 empty");
    //                                modbus.ResetJuicer();
    //                                break;
    //                            case 26:
    //                                IsContinue = false;
    //                                logger.LogInformation("Cup station 2 sensor detection failure");
    //                                modbus.ResetJuicer();
    //                                break;
    //                            case 27:
    //                                IsContinue = false;
    //                                logger.LogInformation("Cup station delivery point sensor detection failure");
    //                                modbus.ResetJuicer();
    //                                break;
    //                            case 28:
    //                                IsContinue = false;
    //                                logger.LogInformation("Fruit count empty");
    //                                modbus.ResetJuicer();
    //                                break;
    //                            case 29:
    //                                logger.LogInformation("Juice door open");
    //                                lblSecondsRemains.Text = $"Please close the door{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 30:
    //                                logger.LogInformation("Juice door open and cup taken");
    //                                lblSecondsRemains.Text = $"Please take the cup and close the door{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
    //                                break;
    //                            case 31:
    //                                IsContinue = false;
    //                                logger.LogInformation("Process abort");
    //                                modbus.ResetJuicer();
    //                                break;
    //                            case 32:
    //                                IsContinue = false;
    //                                logger.LogInformation("Cup station home not reached.");
    //                                modbus.ResetJuicer();
    //                                break;

    //                            default:
    //                                logger.LogInformation("Unknown status code received");
    //                                logger.LogInformation($"Modbus status: {modbus.modbusStatus}");
    //                                break;
    //                        }

    //                        if (wait_count >= 180)
    //                        {
    //                            IsContinue = false;
    //                            logger.LogInformation("Timeout");
    //                        }
    //                    }

    //                    // After vending completion, update stock
    //                    await dbContext.SaveChangesAsync();
    //                }
    //                catch (Exception ex)
    //                {
    //                    logger.LogError(ex, $"Error occurred while vending item {vendingItem.Name}.");
    //                    logger.LogInformation($"Vending Start Failed due to port closed. Reason {modbus.modbusStatus}");
    //                }
    //            }



    //            if (IsVendSuccess)
    //            {
    //                var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

    //                if (order != null)
    //                {
    //                    foreach (var item in order.Items!)
    //                    {
    //                        var vendItem = vendingItems.FirstOrDefault(x => x.Id == item.ProductId);
    //                        if (vendItem != null)
    //                        {
    //                            item.VendQty = vendItem.Vend;
    //                            item.UpdatedOn = DateTime.Now;
    //                            item.IsViewed = false;
    //                        }
    //                    }
    //                    await ReduceStockAsync();
    //                    await dbContext.SaveChangesAsync();
    //                    logger.LogInformation("Vending process completed.");
    //                }
    //            } 
    //    catch (Exception ex)
    //    {
    //        logger.LogError($"Vending Start Failed due to port closed. Reason {modbus.modbusStatus}");
    //    }
    //}
    private async Task StartVending()
    {
        logger.LogInformation("Starting vending process...");
        lblQrMessage.Text = "Please wait, preparing your juice...";
        lblSecondsRemains.Text = "";
        lblSecondsPart2.Text = "";
        lblSecondsRemains.TextAlignment = TextAlignment.Left;

        btnCancel.Visibility = Visibility.Hidden;
        processCard.Visibility = Visibility.Hidden;
        imgUPI.Visibility = Visibility.Hidden;
        imgOrnageProcess.Visibility = Visibility.Visible;

        bool IsVendSuccess = false;

        try
        {
            // Validate vending items
            if (vendingItems == null || vendingItems.Count == 0)
            {
                logger.LogWarning("No items to vend.");
                return;
            }

            // Open Modbus connection
            modbus.Open();

            if (!modbus.IsOpen())
            {
                logger.LogInformation("Vending Start Failed due to port closed.");
                return;
            }

            logger.LogInformation("Port is open. Proceeding with vending...");

            // Iterate through vending items
            foreach (var vendingItem in vendingItems)
            {
                for (int i = 0; i < vendingItem.Qty; i++)
                {
                    try
                    {
                        // Retrieve motor settings for vending item
                        var setting = await dbContext.MotorSettings
                            .Where(x => x.ProductId == vendingItem.Id && x.Stock > 0 && x.IsActive && !x.SoldOut)
                            .OrderBy(x => x.CabinId)
                            .ThenBy(x => x.MotorNumber)
                            .FirstOrDefaultAsync();

                        if (setting == null)
                        {
                            logger.LogInformation($"Out of stock for product {vendingItem.Name}.");
                            continue;
                        }

                        vendingItem.VendQtyStatus[i].Processing = true;
                        vendingItem.VendQtyStatus[i].ProcessValue = 4;
                        vendingItem.Status = "Starting.";

                        logger.LogInformation($"Start vending for product {vendingItem.Name}, Qty {i + 1}");

                        int cupStation = 1;

                        // Activate motor for vending
                        bool write = modbus.RunMotor(cupStation);
                        logger.LogInformation($"Motor write success: {write}");

                        if (!write)
                        {
                            logger.LogInformation("Modbus write failed.");
                            vendingItem.VendQtyStatus[i].Processing = false;
                            vendingItem.VendQtyStatus[i].ProcessValue = 0;
                            vendingItem.Status = "";
                            continue;
                        }

                        // Process monitoring
                        bool IsContinue = true;
                        int wait_count = 0;
                        int processStatus = 0;

                        while (IsContinue)
                        {
                            await Task.Delay(1000);
                            wait_count++;
                            processStatus = modbus.Status();
                            logger.LogInformation($"Process status: {processStatus}");

                            // Handle different process statuses
                            switch (processStatus)
                            {
                                case 0:
                                    lblSecondsRemains.Text = $"Starting{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 1:
                                    lblSecondsRemains.Text = $"Starting{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 2:
                                    logger.LogInformation("Cup one dispensed");
                                    lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 3:
                                    logger.LogInformation("Cup two dispensed");
                                    lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 4:
                                    logger.LogInformation("Cup delivery ponit reached");
                                    lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 5:
                                    logger.LogInformation("Fruit loaded");
                                    lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 6:
                                    IsVendSuccess = true;
                                    logger.LogInformation("Juicer motor started");
                                    lblSecondsRemains.Text = $"Crushing Your Juice, Please wait{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 7:
                                    IsVendSuccess = true;
                                    await Task.Delay(5000);
                                    logger.LogInformation("Juice Level reached");
                                    lblSecondsRemains.Text = $"Open the door and pickup your juice{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 8:
                                    IsVendSuccess = true;
                                    IsContinue = false;
                                    logger.LogInformation("Cup taken");
                                    lblSecondsRemains.Text = $"Enjoy your Juice{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;

                                case 21:
                                case 22:
                                case 23:
                                    logger.LogInformation("Home position error");
                                    break;
                                case 24:
                                    IsContinue = false;
                                    logger.LogInformation("Cup1 empty");
                                    modbus.ResetJuicer();
                                    break;
                                case 25:
                                    IsContinue = false;
                                    logger.LogInformation("Cup2 empty");
                                    modbus.ResetJuicer();
                                    break;
                                case 26:
                                    IsContinue = false;
                                    logger.LogInformation("Cup station 2 sensor detection failure");
                                    modbus.ResetJuicer();
                                    break;
                                case 27:
                                    IsContinue = false;
                                    logger.LogInformation("Cup station delivery point sensor detection failure");
                                    modbus.ResetJuicer();
                                    break;
                                case 28:
                                    IsContinue = false;
                                    logger.LogInformation("Fruit count empty");
                                    modbus.ResetJuicer();
                                    break;
                                case 29:
                                    logger.LogInformation("Juice door open");
                                    lblSecondsRemains.Text = $"Please close the door{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 30:
                                    logger.LogInformation("Juice door open and cup taken");
                                    lblSecondsRemains.Text = $"Please put the cup and close the door{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                    break;
                                case 31:
                                    IsContinue = false;
                                    logger.LogInformation("Process abort");
                                    modbus.ResetJuicer();
                                    break;
                                case 32:
                                    IsContinue = false;
                                    logger.LogInformation("Cup station home not reached.");
                                    break;

                                default:
                                    logger.LogInformation("Unknown status code received");
                                    logger.LogInformation($"Modbus status : {modbus.modbusStatus}");
                                    break;
                            }

                            // Timeout
                            if (wait_count >= 220)
                            {
                                logger.LogWarning("Process timeout.");
                                IsContinue = false;
                            }
                        }
                        await ReduceStockAsync();
                        // Update stock after vending
                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error while vending item {vendingItem.Name}.");
                    }
                }
            }

            // Finalize order if vending was successful
            if (IsVendSuccess)
            {
                var order = await dbContext.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

                if (order != null)
                {
                    foreach (var item in order.Items!)
                    {
                        var vendItem = vendingItems.FirstOrDefault(x => x.Id == item.ProductId);
                        if (vendItem != null)
                        {
                            item.VendQty = vendItem.Vend;
                            item.UpdatedOn = DateTime.Now;
                            item.IsViewed = false;
                        }
                    }

                   
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Vending process completed.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the vending process.");
        }
    }

    public async Task UpdateOrder()
    {
        try
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

            if (order is null)
                return;

            double VendTotal = vendingItems.Sum(x => x.VendAmount);
            order.Total = VendTotal;
            order.Status = StrDir.OrderStatus.SUCCESS;
            order.IsViewed = false;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task ReduceStockAsync()
    {
        try
        {
            // Retrieve motor settings from the database
            var motor1 = await dbContext.MotorSettings
                                        .Where(ms => ms.MotorNumber == 1)
                                        .FirstOrDefaultAsync();

            var motor2 = await dbContext.MotorSettings
                                        .Where(ms => ms.MotorNumber == 2)
                                        .FirstOrDefaultAsync();

            var motor3 = await dbContext.MotorSettings
                                        .Where(ms => ms.MotorNumber == 3)
                                        .FirstOrDefaultAsync();

            var motor4 = await dbContext.MotorSettings
                                        .Where(ms => ms.MotorNumber == 4)
                                        .FirstOrDefaultAsync();

            // Ensure all motors are retrieved
            if (motor1 != null && motor2 != null && motor3 != null && motor4 != null)
            {
                // Define the oranges per cup
                const int OrangePerCup = 5;


                motor3.Stock -= OrangePerCup;  // Reduce oranges
                motor4.Stock -= 1;             // Reduce cups


                float motor1StockInKg = motor3.Stock / motor2.Stock;  // Calculate stock in float
                motor1.Stock = (int)motor1StockInKg;                  // Store floored value

                // Log the float value of motor1 stock for auditing
                logger.LogInformation($"Motor 1 Stock (logged as float): {motor1StockInKg:F2} kg");


                dbContext.MotorSettings.Update(motor1);

                dbContext.MotorSettings.Update(motor3);
                dbContext.MotorSettings.Update(motor4);


                // Save changes to the database
                await dbContext.SaveChangesAsync();

                // Log success message
                logger.LogInformation("Stock updated successfully.");
            }
            else
            {
                // Log error if any motor is missing
                logger.LogError("Failed to retrieve all motor settings from the database.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors during the stock update process
            logger.LogError($"Error reducing stock: {ex.Message}", ex);
        }
    }

    public async Task CheckRefund()
    {
        try
        {

            var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

            if (order is null)
                return;

            double VendTotal = vendingItems.Sum(x => x.VendAmount);

            logger.LogInformation($"Total Vended Amount : {VendTotal}");

            double balance = order.PaidAmount - VendTotal;
            string txtMsg = string.Empty;

            if (balance > 0)
            {
                string msg = string.Join(" | ", vendingItems.Where(x => x.Qty != x.Vend)
                                                            .Select(x => $"{x.Name}-{x.Price}x{x.Qty - x.Vend}={(x.Qty - x.Vend) * x.Price}")
                                                            .ToList());
                logger.LogInformation($"Refund : {balance}, Msg :{msg}");

                switch (order.PaymentType)
                {
                    case "UPI":
                        {
                            txtMsg = $"Sorry for the Inconvenience, Your balance of Rs.{balance} has been refunded. It will reflect your account within 48hrs.";
                            bool isRefund = await RefundUPI(order.OrderNumber, balance, msg);
                            if (isRefund)
                            {
                                order.IsRefunded = true;
                                order.RefundedAmount = balance;
                                order.IsViewed = false;
                                logger.LogInformation($"UPI refund success");
                            }
                        }
                        break;
                 
                    default:
                        break;
                }
            }

            await dbContext.SaveChangesAsync();

            if (!string.IsNullOrEmpty(txtMsg))
            {
                lblQrMessage.Text = txtMsg;
                await Task.Delay(5000);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async Task<bool> RefundUPI(string orderNumber, double refundAmount, string msg = "")
    {
        logger.LogInformation($"Refund for order : {orderNumber}, Amount :{refundAmount}");
        try
        {
            QrRefundDto refundDto = new QrRefundDto(orderNumber, refundAmount, msg);
            var response = await httpClient.PostAsync($"api/payments/upi/refund", refundDto);
            string respText = await response.Content.ReadAsStringAsync();
            logger.LogInformation(respText);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return false;
        }
    }

    private void CancelOrder()
    {
        try
        {
            CancelOrderAnimation();
            DataStore.orderNumber = string.Empty;
            tmrUpi.Stop();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
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
    #region UI-Code

    private void meVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        PlayNextVideo();
    }

    private void meVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        PlayNextVideo();
    }

    private void TmrDateTime_Tick(object? sender, EventArgs e)
    {
        lblCurrentTime.Text = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
    }

    private void PlayNextVideo()
    {
        try
        {
            if (DataStore.videos.Count > 0)
            {
                DataStore.playIndex++;

                DataStore.playIndex = DataStore.playIndex >= DataStore.videos.Count ? 0 : DataStore.playIndex;

                meVideo.Source = new Uri(DataStore.videos[DataStore.playIndex]);
            }
        }
        catch
        {

        }
    }

    private void MoveOrderCenterToLeft()
    {
        try
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 340;
            animation.To = 40;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, spOrder);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.LeftProperty));
            storyboard.Completed += OrderStoryboard_Completed;
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void OrderStoryboard_Completed(object? sender, EventArgs e)
    {
        try
        {
            spUpi.Visibility = Visibility.Visible;

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0;
            animation.To = 1;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, spUpi);
            Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private void CancelOrderAnimation()
    {
        DoubleAnimation animation = new DoubleAnimation();
        animation.From = 1;
        animation.To = 0;
        animation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));

        Storyboard storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        Storyboard.SetTarget(animation, spUpi);
        Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
        storyboard.Completed += HideUpi_Completed;
        storyboard.Begin();
    }

    private void HideUpi_Completed(object? sender, EventArgs e)
    {
        try
        {
            spUpi.Visibility = Visibility.Hidden;
            imgOrnageProcess.Visibility = Visibility.Hidden;
            imgUPI.Visibility = Visibility.Hidden;
            processCard.Visibility = Visibility.Hidden;

            btnOrder.IsEnabled = true;

            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 40;
            animation.To = 340;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(1000));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, spOrder);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.LeftProperty));
            storyboard.Begin();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }



    #endregion
}

