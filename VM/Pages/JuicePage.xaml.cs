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
using System.Speech.Recognition;
using System.Windows.Input;
using System.Windows.Documents;
using ESC_POS_USB_NET.EpsonCommands;
using NPOI.SS.Formula.Functions;
using System.Drawing.Imaging;
using static System.Formats.Asn1.AsnWriter;

namespace VM.Pages;

public partial class JuicePage : Page
{
    DispatcherTimer tmrDateTime = new DispatcherTimer();
    DispatcherTimer tmrUpi = new DispatcherTimer();
    DispatcherTimer tmrAdmin = new DispatcherTimer();
    DispatcherTimer inactivityTimer = new DispatcherTimer();

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly AppDbContext dbContext;
    private readonly IServiceProvider serviceProvider;
    private readonly IServerClient httpClient;
    private readonly ISignalRClientService signalRClient;
    private readonly IModbus modbus;
    private readonly ILogger<JuicePage> logger;
    private readonly IOrderService orderService;
    private readonly IHostEnvironment environment;
    private readonly ISyncService syncService;
    private readonly IPineLabsService pineLabsService;
    private readonly ISensorService sensorService;
    //private DispatcherTimer inactivityTimer;

    private int CabinId = 1;

    int adminTmrCount = 0;
    List<VendingProductDto> vendingItems = new List<VendingProductDto>();

   
    string upiNumber = string.Empty;
    int OrangePerCup = 5;
    Product product;
    int timeout = 20;
    bool hardwareReady = false;

    public JuicePage(AppDbContext dbContext, IServerClient httpClient, ISignalRClientService signalRClient,
        IModbus modbus,
        ILogger<JuicePage> logger,
        IServiceProvider serviceProvider,
        IOrderService orderService,

        IHostEnvironment environment,
        IServiceScopeFactory serviceScopeFactory,
        ISyncService syncService, IPineLabsService pineLabsService)
       
    {
        InitializeComponent();
        tmrDateTime.Interval = TimeSpan.FromSeconds(1);
        tmrDateTime.Tick += TmrDateTime_Tick;
        tmrUpi.Interval = TimeSpan.FromSeconds(1);
        tmrUpi.Tick += TmrUpi_Tick;

        // ✅ Setup 2-minute inactivity timer
        inactivityTimer.Interval = TimeSpan.FromMinutes(2);
        inactivityTimer.Tick += InactivityTimer_Tick;
        inactivityTimer.Start();

        // ✅ Reset inactivity timer on user interaction
        this.PreviewMouseMove += (s, e) => ResetInactivityTimer();
        this.PreviewKeyDown += (s, e) => ResetInactivityTimer();
        this.Unloaded += JuicePage_Unloaded;

        this.syncService = syncService;
        this.serviceScopeFactory = serviceScopeFactory;
        //this.dbContext = dbContext;
        this.httpClient = httpClient;
        this.serviceProvider = serviceProvider;
        this.signalRClient = signalRClient;
        this.modbus = modbus;
        this.logger = logger;
        this.orderService = orderService;
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        //product = dbContext.Products
        //    .Where(x => EF.Functions.Like(x.Name, "%juice%"))
        //    .OrderByDescending(x => x.UpdatedOn)
        //    .FirstOrDefault();
        //product = dbContext.Products.OrderByDescending(x => x.UpdatedOn).First();
        product = (from s in dbContext.MotorSettings
                   join p in dbContext.Products on s.ProductId equals p.Id
                   where s.IsActive == true && s.CabinId == CabinId
                   orderby s.MotorNumber
                   select p) 
  .FirstOrDefault();
    }

    // ✅ Resets inactivity timer
    private void ResetInactivityTimer()
    {
        inactivityTimer.Stop();
        inactivityTimer.Start();
    }

    // ✅ Triggered after 5 mins of inactivity
    private void InactivityTimer_Tick(object sender, EventArgs e)
    {
        inactivityTimer.Stop(); // Optional: prevent retrigger

        logger.LogInformation("User inactive for 5 minutes. Navigating back.");

        if (NavigationService != null && NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
    }

    private void JuicePage_Unloaded(object sender, RoutedEventArgs e)
    {
        inactivityTimer?.Stop();
        inactivityTimer.Tick -= InactivityTimer_Tick;
    }

   
    private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Tab))
            {
                NavigationService?.Navigate(serviceProvider.GetRequiredService<OperatorHomePage>());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (!modbus.IsOpen())
        {
            logger.LogInformation($"Modbus Connection Failed!");
            modbusAlertPopup.IsOpen = true;  // Open the popup
        }
        if (!string.IsNullOrEmpty(DataStore.orderNumber))
        {
            using var scope = serviceScopeFactory.CreateScope();
            var scopedOrderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            await scopedOrderService.UpdateOrderStatus(DataStore.orderNumber);
            eventService.RaiseOrderCompleteEvent(DataStore.orderNumber);
            await syncService.StockUpdateAsync();
            await syncService.PushStockClearedAsync();
            await syncService.PushStockRefillAsync();
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

        signalRClient.OnPaymentStatusReceived += OnPaymentStatusReceived;
        tmrDateTime.Start();
        spUpi.Visibility = Visibility.Hidden;
        processCard.Visibility = Visibility.Hidden;

        imgUPI.Visibility = Visibility.Hidden;
        imgOrnageProcess.Visibility = Visibility.Hidden;
        lblPrice.Text = $"₹ {product.Price}";

        PlayNextVideo();
        modbus.Open();
        if (!modbus.IsOpen())
        {
            logger.LogInformation($"Modbus Connection Failed!");
            modbusAlertPopup.IsOpen = true;  // Open the popup
        }
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

    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        // Handle navigation logic or go back to the previous page
        if (NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
    }

    private async void btnOrder_Click(object sender, RoutedEventArgs e)
    {
        if (!modbus.IsOpen())
        {
            logger.LogInformation($"Modbus Connection Failed!");
            modbusAlertPopup.IsOpen = true; 
        }
        if (modbus.IsOpen())
        {  try
            {
                var productsWithStock = await (from s in dbContext.MotorSettings
                                               join p in dbContext.Products on s.ProductId equals p.Id
                                               where s.IsActive == true && s.CabinId == CabinId
                                                     && (s.MotorNumber == 1 || s.MotorNumber == 2) // Check only motors 1 and 2
                                               orderby s.MotorNumber
                                               select new { Product = p, s.Stock }).ToListAsync();

                if (productsWithStock == null || !productsWithStock.Any())
                {
                    logger.LogInformation("No products found for motors 1 and 2.");
                    MainDialogHost.IsOpen = true;
                    return;
                }

                // Check if any motor has Stock = 0 or Price = 0
                bool anyMotorOutOfStockOrFree = productsWithStock.Any(p => p.Product.Price <= 0 || p.Stock <= 0);

                if (anyMotorOutOfStockOrFree)
                {
                    logger.LogInformation("At least one motor (1 or 2) has no stock or price is 0.");
                    MainDialogHost.IsOpen = true;
                }
                else
                {
                    OrderDialogHost.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while checking stock for motors 1 & 2.");
            }
        }
    }


    private void btnPineApps_Click(object sender, RoutedEventArgs e)
    {
        var scope = serviceScopeFactory.CreateScope();
        NavigationService.Navigate(scope.ServiceProvider.GetRequiredService<CardPaymentPage>());
    }

    private async void btnQr_Click(object sender, RoutedEventArgs e)
    { 
        OrderDialogHost.IsOpen = false;
        try
        {
            //var motor3Stock = await dbContext.MotorSettings
            //                                 .Where(ms => ms.MotorNumber == 3)
            //                                 .Select(ms => ms.Stock)
            //                                 .FirstOrDefaultAsync();
            //var motor4Stock = await dbContext.MotorSettings
            //                                 .Where(ms => ms.MotorNumber == 4)
            //                                 .Select(ms => ms.Stock)
            //                                 .FirstOrDefaultAsync();

            //if (motor3Stock < OrangePerCup && motor4Stock < 1)
            //{
            //    //await MachineFailDialog();
            //    return;
            //}

            //if (!modbus.IsOpen())
            //{
            //    logger.LogInformation("Port is not opened");
            //    //await MachineFailDialog();
            //    return;
            //}

            lblQrMessage.Text = "Please wait, Generating QR code";
            lblSecondsRemains.Text = "";
            lblSecondsPart2.Text = "";
            btnCancel.Visibility = Visibility.Visible;

            imgUPI.Visibility = Visibility.Hidden;
            imgOrnageProcess.Visibility = Visibility.Hidden;
            processCard.Visibility = Visibility.Hidden;

            MoveOrderCenterToLeft();
            btnOrder.IsEnabled = false;
            btnBack.Visibility = Visibility.Collapsed;

            DataStore.orderNumber = await CreateOrderAsync();
            QrImageDto qr = await GetQr(DataStore.orderNumber, product.Price);

            if (qr.IsSuccess)
            {

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
    private void CloseDialog(object sender, RoutedEventArgs e)
    {
        OrderDialogHost.IsOpen = false; // Close the modal
    }
    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        CancelOrder();
        btnBack.Visibility = Visibility.Visible;

    }


    //private async Task MachineFailDialog()
    //{
    //    try
    //    {
    //        pgJuicePageHost.IsOpen = true;
    //        var tcs = new TaskCompletionSource<bool>();
    //        RoutedEventHandler handler = null;
    //        handler = (sender, args) =>
    //        {
    //            tcs.SetResult(true);
    //            pgJuicePageHost.IsOpen = false;
    //            btnOk.Click -= handler;
    //        };
    //        btnOk.Click += handler;
    //        await tcs.Task;
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError($"Error in MsgDialog: {ex.Message}");
    //    }
    //}


    private void OnDialogOkClick(object sender, RoutedEventArgs e)
    {
        pgJuicePageHost.IsOpen = false;
    }

    private void TmrUpi_Tick(object? sender, EventArgs e)
    {
        timeout--;

        if (timeout <= 0)
        {
            tmrUpi.Stop();
            btnBack.Visibility = Visibility.Visible;
            CheckPaymentStatus(DataStore.orderNumber);
        }
        lblSecondsRemains.Text = $"{timeout} Seconds";
        


    }
    public string GenerateQrCode(string orderNumber, double amount)
    {
        // Example method for generating QR code using a QR code library (e.g., QRCoder).
        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode($"orderNumber:{orderNumber},amount:{amount}", QRCodeGenerator.ECCLevel);
        var qrCode = new QRCode(qrData);
        using (var ms = new MemoryStream())
        {
            //qrCode.GetGraphic(20).Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
    }


    private async Task<string> CreateOrderAsync()
    {
        try
        {
            string orderId = $"or_{Guid.NewGuid().ToString("N").ToUpper()}";
            int orderCount = await dbContext.Orders.CountAsync();
            orderCount++;
            string orderNumber = $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}{DataStore.AppType[0]}{orderCount.ToString("000000")}";


            Order order = new Order()
            {
                Id = orderId,
                OrderNumber = orderNumber,
                OrderDate = DateTime.Now,
                PaymentType = "UPI",
                DeliveryType = "VEND",
                Total = product.Price,
                IsPaid = false,
                PaidAmount = 0,
                IsRefunded = false,
                RefundedAmount = 0,
                IsViewed = false,
                Items = [new OrderItem() {
                   Id = $"oi_{Guid.NewGuid().ToString("N").ToUpper()}",
                            OrderId = orderId,
                            ProductId = product.Id,
                            ProductName = product.Name,
                            Rate = product.BaseRate,
                            Gst = product.Gst,
                            Price = product.Price,
                            Qty = 1,
                            VendQty = 0,
                            IsViewed = false,
                            UpdatedOn = DateTime.Now
            }],
            };

            await dbContext.Orders.AddAsync(order);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("\n\n");
            logger.LogInformation("----------------------------------------------------------------------------");
            logger.LogInformation($"Order Number : {order.OrderNumber} , Amount : {order.Total},  Type : {order.PaymentType}");

            return orderNumber;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return $"{DataStore.MachineInfo.VendorShortName}{DataStore.MachineInfo.MachineNumber.ToString("00")}{DataStore.AppType[0]}{Guid.NewGuid().ToString("N").ToUpper().Substring(0, 6)}";
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
            btnBack.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            return new QrImageDto(orderNumber, false, Message: "Unable to generate QR code");
            btnBack.Visibility = Visibility.Visible;
        }
    }
    private async Task CheckPaymentStatus(string orderNumber)
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
            if (status.OrderNumber == DataStore.orderNumber)
            {
                tmrUpi.Stop();
                //btnBack.Visibility = Visibility.Visible;
                Dispatcher.Invoke((Action)(() =>
                {
                    ProcessPaymentStatus(status);
                }));
            }
        }
    }

    private async Task UpdatePaymentStatus(string orderNumber, bool isPaid)
    {
        try
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
            if (order != null)
            {
                order.IsPaid = isPaid;
                order.PaidAmount = isPaid ? order.Total : 0;
                dbContext.Orders.Update(order);
                await dbContext.SaveChangesAsync();
                logger.LogInformation($"Payment status updated for Order Number : {orderNumber}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
    private async void ProcessPaymentStatus(QrStatusDto qrStatus)
    {
        try
        {
            if (qrStatus.OrderNumber == DataStore.orderNumber)
            {
                switch (qrStatus.Status)
                {
                    case PaymentStatusCode.PENDING:
                        break;
                    case PaymentStatusCode.SUCCESS:
                        if (string.IsNullOrEmpty(DataStore.orderNumber))
                        {
                            DataStore.orderNumber = await CreateOrderAsync();
                        }
                        DataStore.PaymentDto = new OrderPaymentDto()
                        {
                            PaymentType = StrDir.PaymentType.UPI,
                            Amount = product.Price,
                            IsPaid = true,
                            Reference = DataStore.orderNumber,
                            PaymentId = qrStatus.PaymentId
                        };
                        logger.LogInformation($"Payment successful: IsPaid={DataStore.PaymentDto.IsPaid}");
                        //await CreateOrderAsync();
                        // Update the Order record with the PaymentId from the payment status
                        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == DataStore.orderNumber);
                        if (order != null)
                        {
                            order.PaymentId = qrStatus.PaymentId;

                            dbContext.Orders.Update(order);
                            await dbContext.SaveChangesAsync();
                            logger.LogInformation($"Order {order.OrderNumber} updated with PaymentId: {order.PaymentId}");
                        }
                        else
                        {
                            logger.LogWarning($"Order {DataStore.orderNumber} not found during payment update.");
                        }
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
                {
                    CancelOrder();
                    btnBack.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }
    //private async void ProcessPaymentStatus(QrStatusDto qrStatus)
    //{
    //    try
    //    {
    //        if (qrStatus.OrderNumber == DataStore.orderNumber && !DataStore.IsVendingStarted)
    //        {
    //            switch (qrStatus.Status)
    //            {
    //                case PaymentStatusCode.PENDING:
    //                    break;

    //                case PaymentStatusCode.SUCCESS:
    //                    if (string.IsNullOrEmpty(DataStore.orderNumber))
    //                    {
    //                        DataStore.orderNumber = await CreateOrderAsync();
    //                    }

    //                    DataStore.PaymentDto = new OrderPaymentDto()
    //                    {
    //                        PaymentType = StrDir.PaymentType.UPI,
    //                        Amount = product.Price,
    //                        IsPaid = true,
    //                        Reference = DataStore.orderNumber,
    //                        PaymentId = qrStatus.PaymentId
    //                    };

    //                    logger.LogInformation($"Payment successful: IsPaid={DataStore.PaymentDto.IsPaid}");

    //                    var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == DataStore.orderNumber);
    //                    if (order != null)
    //                    {
    //                        order.PaymentId = qrStatus.PaymentId;
    //                        dbContext.Orders.Update(order);
    //                        await dbContext.SaveChangesAsync();
    //                        logger.LogInformation($"Order {order.OrderNumber} updated with PaymentId: {order.PaymentId}");
    //                    }
    //                    else
    //                    {
    //                        logger.LogWarning($"Order {DataStore.orderNumber} not found during payment update.");
    //                    }

    //                    await StartProcessing();
    //                    break;

    //                case PaymentStatusCode.FAILED:
    //                default:
    //                    lblQrMessage.Text = "Transaction failed";
    //                    btnCancel.IsEnabled = false;
    //                    Task.Delay(3000).ContinueWith(t => CancelOrder());
    //                    break;
    //            }

    //            if (timeout <= 0 && qrStatus.Status == PaymentStatusCode.PENDING)
    //            {
    //                CancelOrder();
    //                btnBack.Visibility = Visibility.Visible;
    //            }
    //        }
    //        else
    //        {
    //            logger.LogWarning("Duplicate or irrelevant QR status received, ignoring.");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex.Message, ex);
    //    }
    //}



    public async Task StartProcessing()
    {
        try
        {

            if (DataStore.PaymentDto.IsPaid)
            {
                if (DataStore.orderNumber == string.Empty)
                {
                    string orderNumber = await orderService.CreateOrderAsync(
                                                DataStore.selectedProducts,
                                                DataStore.PaymentDto,
                                                DataStore.deliveryType);
                    DataStore.orderNumber = orderNumber;
                }




                modbus.Open();
                var order = await dbContext.Orders.Include(x => x.Items)
                  .FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);
                logger.LogInformation("Start Vending Process");
                await StartVending();
                await CheckRefund();
            }
            else
            {
                lblQrMessage.Text = "Sorry, order payment not received.";
                logger.LogInformation("Order payment not received.");
                await Task.Delay(5000);
            }

            await UpdateOrder();

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while processing the order.");
        }
        finally
        {
            modbus.Close();
            CancelOrder();
            btnBack.Visibility = Visibility.Visible;
            var eventService = serviceProvider.GetRequiredService<IEventService>();
            eventService.RaiseOrderCompleteEvent(DataStore.orderNumber);


            await syncService.StockUpdateAsync();
        }
    }
    //public async Task StartProcessing()
    //{
    //    if (DataStore.IsVendingStarted)
    //    {
    //        logger.LogWarning("Vending already started. Skipping duplicate call.");
    //        return;
    //    }

    //    DataStore.IsVendingStarted = true;

    //    try
    //    {
    //        if (DataStore.PaymentDto?.IsPaid == true)
    //        {
    //            if (string.IsNullOrEmpty(DataStore.orderNumber))
    //            {
    //                string orderNumber = await orderService.CreateOrderAsync(
    //                                            DataStore.selectedProducts,
    //                                            DataStore.PaymentDto,
    //                                            DataStore.deliveryType);
    //                DataStore.orderNumber = orderNumber;
    //            }

    //            modbus.Open();

    //            var order = await dbContext.Orders.Include(x => x.Items)
    //              .FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);

    //            logger.LogInformation("Start Vending Process");

    //            await StartVending();
    //            await CheckRefund();
    //        }
    //        else
    //        {
    //            lblQrMessage.Text = "Sorry, order payment not received.";
    //            logger.LogInformation("Order payment not received.");
    //            await Task.Delay(5000);
    //        }

    //        await UpdateOrder();
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex, "Error occurred while processing the order.");
    //    }
    //    finally
    //    {
    //        modbus.Close();
    //        CancelOrder();
    //        btnBack.Visibility = Visibility.Visible;

    //        var eventService = serviceProvider.GetRequiredService<IEventService>();
    //        eventService.RaiseOrderCompleteEvent(DataStore.orderNumber);

    //        await syncService.StockUpdateAsync();
    //        //DataStore.IsVendingStarted = false;
    //    }
    //}




    public async Task StartVending()
    {
        try
        {
            lblQrMessage.Text = "Please wait, preparing your juice";
            lblSecondsRemains.Text = "";
            lblSecondsPart2.Text = "";
            lblSecondsRemains.TextAlignment = TextAlignment.Left;

            btnCancel.Visibility = Visibility.Hidden;
            processCard.Visibility = Visibility.Hidden;
            imgUPI.Visibility = Visibility.Hidden;
            imgOrnageProcess.Visibility = Visibility.Visible;
            bool IsVendSuccess = false;

            modbus.Open();

            if (!modbus.IsOpen())
            {
                logger.LogInformation($"Modbus Connection Failed!");
                modbusAlertPopup.IsOpen = true;  // Open the popup
            }

            if (modbus.IsOpen())
            {

                int cupStation = 1;

               bool write = modbus.RunMotor(cupStation);
               //bool write = true;
             
                logger.LogInformation($"Vend write Status : {write}");

                if (write)
                {

                    bool IsContinue = true;
                    int wait_count = 0;
                    int processStatus = 0;

                    while (IsContinue)
                    {
                        await Task.Delay(1000);
                        wait_count++;
                        processStatus = 0; 
                        //processStatus = 1;

                        logger.LogInformation($"Process status : {processStatus}");

                        switch (processStatus)
                        {
                            case 0:
                                lblSecondsRemains.Text = $"Cup dispencer{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                break;
                            //case 1:
                            //    IsVendSuccess = true;
                            //    IsContinue = false;
                            //    logger.LogInformation("Cup dispensed");
                            //    //lblSecondsRemains.Text = $"Processing{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                            //    ReduceStockAsync();
                            //    break;
                            case 1:
                                IsVendSuccess = true;
                              
                                logger.LogInformation("Cup dispensed");

                                //for (int i = 50; i > 0; i--)
                                //{
                                //    //lblSecondsRemains.Text = $"Processing... {i} sec remaining";
                                //    await Task.Delay(1000); // wait 1 second
                                //}

                               // Reduce stock after countdown
                                break;
                            case 2:
                                IsVendSuccess = true;
                                logger.LogInformation("Fruit loaded");
                                lblSecondsRemains.Text = $"Fruit loaded{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                break;
                            case 3:
                                IsVendSuccess = true;
                                IsContinue = false;
                                await ReduceStockAsync();
                                logger.LogInformation("Enjoy your juice");
                                lblSecondsRemains.Text = $"Enjoy your juice{string.Concat(Enumerable.Repeat(".", wait_count % 4))}";
                                break;

                            case 21:
                                IsContinue = false;
                                logger.LogInformation("Cup not taken - error");
                                break;
                            case 22:
                                IsContinue = false;
                                logger.LogInformation("Cup1 sensor failed - error");
                                modbus.ResetJuicer();
                                break;
                            case 23:
                                IsContinue = false;
                                logger.LogInformation("Seesaw dowm failed - error");
                                modbus.ResetJuicer();
                                break;
                            case 24:
                                IsContinue = false;
                                logger.LogInformation("Fruit count sensor failed - error");
                                modbus.ResetJuicer();
                                break;
                            case 25:
                                IsContinue = false;
                                logger.LogInformation("Fruit sensor limit failed - error");
                                modbus.ResetJuicer();
                                break;
                            case 26:
                                IsContinue = false;
                                logger.LogInformation("Juicer cam limit failed - error");
                                modbus.ResetJuicer();
                                break;
                            case 27:
                                IsContinue = false;
                                logger.LogInformation("Seesaw up failed - error");
                     
                                break;
                            case 28:
                                IsContinue = false;
                                logger.LogInformation("Sealing back limit failed - error");
                                
                                break;
                            case 29:
                                IsContinue = false;
                                logger.LogInformation("Sealing cam down failed - error");
                                modbus.ResetJuicer();
                                break;
                            case 30:
                                IsContinue = false;
                                logger.LogInformation("Sealing cam down failed - error");
                                break;
                            case 31:
                                IsContinue = false;
                                logger.LogInformation("Sealing front limit failed - error");
                                break;
                            case 32:
                                IsContinue = false;
                                logger.LogInformation("Delivery down failed - error");
                                break;
                            case 33:
                                IsContinue = false;
                                logger.LogInformation("Delivery up failed - error");
                                break;
                            default:
                                logger.LogInformation("Unknown status code received");
                                logger.LogInformation($"Modbus status : {modbus.modbusStatus}");
                                break;
                        }

                        if (wait_count >= 15)
                        {
                            IsContinue = false;
                            logger.LogInformation($"Time out");
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"Modbus write failed");
                logger.LogInformation($"Vending Start Failed due to port closed. Reason {modbus.modbusStatus}");
            }
            }
            else
            {
                logger.LogInformation($"Vending Start Failed due to port closed. Reason {modbus.modbusStatus}");
            }

            var order = await dbContext.Orders.Include(x => x.Items).FirstAsync(x => x.OrderNumber == DataStore.orderNumber);
           

            if (IsVendSuccess)
            {
               
                order.Items.ForEach(x => { x.VendQty = 1; x.IsViewed = false; });
                order.IsRefunded = false;
                order.RefundedAmount = 0;
                order.IsViewed = false;
                order.IsPaid = true;
                order.Status = "SUCCESS";
                order.PaidAmount = product.Price;
                order.Total = product.Price;
                // Save the updated refund details to the MySQL orders table
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Payment success vending completed");
            }
            else
            {
                lblQrMessage.Text = $"Sorry, We can't deliver your order. Your balance of Rs.{product.Price} has been refunded. It will reflect your account within 48hrs.";
                bool refunded = await Refund(DataStore.orderNumber, product.Price);
                if (refunded)
                {
                    order.IsRefunded = true;
                    order.RefundedAmount = product.Price;
                    order.IsViewed = false;
                    order.IsPaid = true;
                    order.Status = "SUCCESS";
                    order.PaidAmount = product.Price;
                    order.Total = 0;
                    // Save the updated refund details to the MySQL orders table
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("UPI refund success");
                }
                await Task.Delay(5000);
            }
            logger.LogInformation($"process end");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
        finally
        {
            modbus.Close();
            btnBack.Visibility = Visibility.Visible;
            btnCancel.Visibility = Visibility.Visible;
            CancelOrder();
        }
    }

    private void ClosePopupButton_Click(object sender, RoutedEventArgs e)
    {
        // Close the popup when the button is clicked
        modbusAlertPopup.IsOpen = false;
    }
    public async Task UpdateOrder()
    {
        try
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);
            if (order is null)
                return;
            double VendTotal = order.Total;
            logger.LogInformation($"Total Vended Amount in update order: {VendTotal}");
            order.Status = StrDir.OrderStatus.SUCCESS;
            order.IsViewed = false;
            await CheckRefund();
            await dbContext.SaveChangesAsync();
            logger.LogInformation("update order process completed");
           

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
            //var motor2 = await dbContext.MotorSettings
            //                            .Where(ms => ms.MotorNumber == 2)
            //                            .FirstOrDefaultAsync();
            //var motor3 = await dbContext.MotorSettings
            //                            .Where(ms => ms.MotorNumber == 3)
            //                            .FirstOrDefaultAsync();
            //var motor4 = await dbContext.MotorSettings
            //                            .Where(ms => ms.MotorNumber == 4)
            //                            .FirstOrDefaultAsync();
            // Ensure all motors are retrieved
            //if (motor1 != null && motor2 != null && motor3 != null && motor4 != null)
            if (motor1 != null)
            {
                // Define the oranges per cup
                int orangeCount = DataStore.OrangeCount;
                int OrangePerCup = orangeCount;
                const int cupPerProcess = 1;
               
                motor1.Stock -= OrangePerCup;  // Reduce oranges
                                               //motor3.IsViewed = false;
                                               //motor4.Stock -= 1
                                               //motor4.IsViewed = false;// Reduce cups
                motor2.Stock -= cupPerProcess;
                float motor1StockInKg = motor1.Stock;
                float motor2Stock = motor2.Stock;
                motor1.Stock = (int)motor1StockInKg;// Store floored value
                motor2.Stock = (int)motor2Stock;
                  motor1.IsViewed = false;
                  motor2.IsViewed = false;
                // Log the float value of motor1 stock for auditing
                logger.LogInformation($"Motor 1 Stock : {(motor1StockInKg)} count");
                logger.LogInformation($"Motor 2 Stock : {(motor2Stock)} count");
                dbContext.MotorSettings.Update(motor1);
                //dbContext.MotorSettings.Update(motor3);
                //dbContext.MotorSettings.Update(motor4);
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
    //public async Task CheckRefund()
    //{
    //    try
    //    {
    //        var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.OrderNumber == DataStore.orderNumber);
    //        if (order is null)
    //            return;
    //        double VendTotal = order.Total;
    //        logger.LogInformation($"Total Vended Amount : {VendTotal}");
    //        double balance = order.PaidAmount - VendTotal;
    //        logger.LogInformation($"Balance : {balance}");
    //        string txtMsg = string.Empty;
    //        if (balance > 0)
    //        {
    //            string msg = string.Join(" | ", vendingItems.Where(x => x.Qty != x.Vend)
    //                                                        .Select(x => $"{x.Name}-{x.Price}x{x.Qty - x.Vend}={(x.Qty - x.Vend) * x.Price}")
    //                                                        .ToList());
    //            logger.LogInformation($"Refund : {balance}, Msg :{msg}");
    //            switch (order.PaymentType)
    //            {
    //                case "UPI":
    //                    {
    //                        txtMsg = $"Sorry for the Inconvenience, Your balance of Rs.{balance} has been refunded. It will reflect your account within 48hrs.";
    //                        bool isRefund = await RefundUPI(order.OrderNumber, balance, msg);
    //                        if (isRefund)
    //                        {
    //                            order.IsRefunded = true;
    //                            order.RefundedAmount = balance;
    //                            order.IsViewed = false;
    //                            logger.LogInformation($"UPI refund success");
    //                        }
    //                    }
    //                    break;
    //                default:
    //                    break;
    //            }
    //        }
    //        await dbContext.SaveChangesAsync();
    //        if (!string.IsNullOrEmpty(txtMsg))
    //        {
    //            lblQrMessage.Text = txtMsg;
    //            await Task.Delay(5000);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.LogError(ex.Message, ex);
    //    }
    //}

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

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        tmrAdmin.Stop();
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
                //meVideo.Source = new Uri(DataStore.videos[DataStore.playIndex]);
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
    private async Task<bool> Refund(string orderNumber, double refundAmount)
    {
        logger.LogInformation($"Refund for order : {orderNumber}, Amount :{refundAmount}");
        try
        {
            QrRefundDto refundDto = new QrRefundDto(orderNumber, refundAmount, "");
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

internal class QRCode
{
    private object qrData;

    public QRCode(object qrData)
    {
        this.qrData = qrData;
    }

    internal object GetGraphic(int v)
    {
        throw new NotImplementedException();
    }
}